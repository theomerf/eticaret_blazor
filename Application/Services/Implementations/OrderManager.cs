using Application.Common.Exceptions;
using Application.Common.Models;
using Application.DTOs;
using Application.Repositories.Interfaces;
using Application.Services.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Application.Queries.RequestParameters;
using System.Data;

namespace Application.Services.Implementations
{
    public class OrderManager : IOrderService
    {
        private readonly IRepositoryManager _manager;
        private readonly IMapper _mapper;
        private readonly ILogger<OrderManager> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISecurityLogService _securityLogService;
        private readonly IPaymentProvider _paymentProvider;
        private readonly ICouponService _couponService;
        private readonly ICampaignService _campaignService;
        private readonly ICartService _cartService;
        private readonly IActivityService _activityService;
        private readonly IEmailQueueService _emailQueueService;
        private readonly ICacheService _cache;

        public OrderManager(
            IRepositoryManager manager,
            IMapper mapper,
            ILogger<OrderManager> logger,
            IHttpContextAccessor httpContextAccessor,
            ISecurityLogService securityLogService,
            IPaymentProvider paymentProvider,
            ICouponService couponService,
            ICampaignService campaignService,
            ICartService cartService,
            IActivityService activityService,
            IEmailQueueService emailQueueService,
            ICacheService cache)
        {
            _manager = manager;
            _mapper = mapper;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _securityLogService = securityLogService;
            _paymentProvider = paymentProvider;
            _couponService = couponService;
            _campaignService = campaignService;
            _cartService = cartService;
            _activityService = activityService;
            _emailQueueService = emailQueueService;
            _cache = cache;
        }

        private string GetCurrentUserId() =>
            _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

        private string GetCurrentUserName() =>
            _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

        private void ValidateUserAccess(string? requestedUserId, string currentUserId)
        {
            if (_httpContextAccessor.HttpContext?.User?.IsInRole("Admin") ?? false) return;
            if (requestedUserId != currentUserId)
            {
                _securityLogService.LogUnauthorizedAccessAsync(
                    userId: currentUserId,
                    requestPath: _httpContextAccessor.HttpContext?.Request?.Path.ToString() ?? ""
                );
                throw new UnauthorizedAccessException("Bu siparişe erişim yetkiniz yok.");
            }
        }

        private string GenerateOrderNumber()
        {
            return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        }

        public async Task<OperationResult<int>> CreateAsync(OrderDtoForCreation orderDto)
        {
            var userId = GetCurrentUserId();
            try
            {
                var createResult = await _manager.ExecuteInTransactionAsync(async cancellationToken =>
                {
                    _manager.ClearTracker();

                    var order = new Order
                    {
                        UserId = userId,
                        OrderNumber = GenerateOrderNumber(),

                        // Customer Info
                        FirstName = orderDto.FirstName,
                        LastName = orderDto.LastName,
                        Phone = orderDto.Phone,

                        // Address
                        City = orderDto.City,
                        District = orderDto.District,
                        AddressLine = orderDto.AddressLine,
                        PostalCode = orderDto.PostalCode,

                        // Shipping & Payment
                        ShippingMethod = orderDto.ShippingMethod,
                        PaymentMethod = orderDto.PaymentMethod,
                        GiftWrap = orderDto.GiftWrap,
                        CustomerNotes = orderDto.CustomerNotes,

                        // Status
                        OrderStatus = OrderStatus.Pending,
                        PaymentStatus = PaymentStatus.Pending,
                        OrderedAt = DateTime.UtcNow,
                        Currency = "TRY"
                    };

                    foreach (var cartLine in orderDto.CartLines)
                    {
                        var product = await _manager.Product.GetByIdAsync(cartLine.ProductId, false, false);
                        if (product == null)
                        {
                            throw new OrderValidationException($"Ürün bulunamadı: {cartLine.ProductName}");
                        }

                        var variant = await _manager.ProductVariant.GetByIdAsync(cartLine.ProductVariantId, false, true);
                        if (variant == null)
                        {
                            throw new OrderValidationException($"Ürün varyantı bulunamadı: {cartLine.ProductName}");
                        }

                        if (variant.Stock < cartLine.Quantity)
                        {
                            throw new OrderValidationException($"Yetersiz stok: {product.ProductName} ({variant.Color} - {variant.Size}). Mevcut: {variant.Stock}");
                        }

                        var category = await _manager.Category.GetByIdAsync(product.CategoryId, false);

                        if (category == null || category.CategoryName == null)
                        {
                            throw new OrderValidationException($"Kategori bulunamadı: ID {product.CategoryId}");
                        }

                        var orderLine = new OrderLine
                        {
                            ProductId = cartLine.ProductId,
                            ProductName = cartLine.ProductName,
                            CategoryName = category.CategoryName,
                            SubCategoryName = category.ParentCategory != null ? category.ParentCategory.CategoryName : null,
                            Quantity = cartLine.Quantity,
                            Price = cartLine.Price,
                            DiscountPrice = cartLine.DiscountPrice,
                            ImageUrl = cartLine.ImageUrl,
                            ProductVariantId = cartLine.ProductVariantId,
                            VariantColor = cartLine.SelectedColor,
                            VariantSize = cartLine.SelectedSize,
                            SpecificationsJson = System.Text.Json.JsonSerializer.Serialize(cartLine.VariantSpecifications.Select(x => new ProductSpecificationDto { Key = x.Key, Value = x.Value }))
                        };

                        orderLine.ValidateForCreation();
                        order.Lines.Add(orderLine);

                        variant.Stock -= cartLine.Quantity;
                        _manager.ProductVariant.Update(variant);
                    }

                    order.SubTotal = order.Lines.Sum(l => (l.DiscountPrice ?? l.Price) * l.Quantity);

                    decimal remainingAmount = order.SubTotal;

                    if (!string.IsNullOrWhiteSpace(orderDto.CouponCode))
                    {
                        var couponResult = await _couponService.ValidateForOrderAsync(orderDto.CouponCode, order.SubTotal, userId);
                        if (couponResult.IsSuccess && couponResult.Data != null)
                        {
                            var coupon = couponResult.Data;
                            order.CouponCode = coupon.Code;
                            order.CouponDiscountAmount = coupon.CalculateDiscount(order.SubTotal);

                            remainingAmount -= order.CouponDiscountAmount.Value;

                            _logger.LogInformation(
                                "Coupon applied. Code: {CouponCode}, Discount: {Discount}, Remaining: {Remaining}",
                                coupon.Code, order.CouponDiscountAmount, remainingAmount);
                        }
                    }

                    var applicableCampaigns = await _campaignService.GetApplicableAsync(order.SubTotal);

                    var sortedCampaigns = applicableCampaigns.OrderByDescending(c => c.Priority).ToList();

                    decimal campaignDiscountTotal = 0;
                    bool nonStackableApplied = false;

                    foreach (var campaign in sortedCampaigns)
                    {
                        if (nonStackableApplied)
                            break;

                        var discount = campaign.CalculateDiscount(remainingAmount);

                        if (discount > 0)
                        {
                            var orderCampaign = new OrderCampaign
                            {
                                CampaignId = campaign.CampaignId,
                                CampaignName = campaign.Name,
                                CampaignType = campaign.Type,
                                CampaignScope = campaign.Scope,
                                CampaignValue = campaign.Value,
                                DiscountAmount = discount,
                                Priority = campaign.Priority
                            };

                            orderCampaign.ValidateForCreation();
                            order.AppliedCampaigns.Add(orderCampaign);
                            campaignDiscountTotal += discount;

                            remainingAmount -= discount;

                            _logger.LogInformation(
                                "Campaign applied. Name: {CampaignName}, Priority: {Priority}, Discount: {Discount}, Remaining: {Remaining}, Stackable: {Stackable}",
                                campaign.Name, campaign.Priority, discount, remainingAmount, campaign.IsStackable);

                            if (!campaign.IsStackable)
                            {
                                nonStackableApplied = true;
                            }
                        }
                    }

                    order.CampaignDiscountTotal = campaignDiscountTotal;
                    order.TotalDiscountAmount = (order.CouponDiscountAmount ?? 0) + (order.CampaignDiscountTotal ?? 0);

                    if (order.TotalDiscountAmount > order.SubTotal)
                    {
                        _logger.LogWarning(
                            "Total discount ({TotalDiscount}) exceeds subtotal ({SubTotal}). Capping discount.",
                            order.TotalDiscountAmount, order.SubTotal);
                        order.TotalDiscountAmount = order.SubTotal;
                    }

                    decimal baseShippingCost = order.ShippingMethod switch
                    {
                        ShippingMethod.Standard => 29.99m,
                        ShippingMethod.Express => 49.99m,
                        ShippingMethod.HandlingOnly => 0m,
                        _ => 0
                    };

                    const decimal freeShippingThreshold = 500m;
                    decimal netAmountAfterDiscounts = order.SubTotal - (order.TotalDiscountAmount ?? 0);

                    if (netAmountAfterDiscounts >= freeShippingThreshold && order.ShippingMethod == ShippingMethod.Standard)
                    {
                        order.ShippingCost = 0;
                        _logger.LogInformation("Free shipping applied. Net amount: {NetAmount}", netAmountAfterDiscounts);
                    }
                    else
                    {
                        order.ShippingCost = baseShippingCost;
                    }

                    // Calculate tax (18% KDV) on net amount including shipping
                    decimal taxableAmount = netAmountAfterDiscounts + order.ShippingCost;
                    order.TaxAmount = taxableAmount * 0.18m;

                    order.CalculateTotals();

                    order.ValidateForCreation();

                    _manager.Order.Create(order);

                    var currentUserId = GetCurrentUserId();
                    var historyEntry = OrderHistory.CreateEvent(
                        orderId: 0,
                        eventType: OrderEventType.OrderCreated,
                        description: "Sipariş oluşturuldu",
                        userId: currentUserId
                    );
                    order.History.Add(historyEntry);

                    if (!string.IsNullOrWhiteSpace(order.CouponCode))
                    {
                        var coupon = await _manager.Coupon.GetByCodeAsync(order.CouponCode, true);
                        if (coupon != null)
                        {
                            coupon.IncrementUsage();

                            var couponUsage = new CouponUsage
                            {
                                CouponId = coupon.CouponId,
                                UserId = userId,
                                Order = order,
                                UsedAt = DateTime.UtcNow
                            };
                            _manager.CouponUsage.Create(couponUsage);
                        }
                    }
                    return order;
                }, IsolationLevel.ReadCommitted, CancellationToken.None);

                await _activityService.LogAsync(
                    "Yeni Sipariş",
                    $"#{createResult.OrderNumber} numaralı sipariş alındı. Tutar: {createResult.TotalAmount:C2}",
                    "fa-shopping-cart",
                    "text-emerald-500 bg-emerald-100",
                    $"/admin/orders/detail/{createResult.OrderId}"
                );

                var user = await _manager.User.GetByIdAsync(createResult.UserId, false);
                if (user?.Email is not null)
                {
                    _emailQueueService.EnqueueOrderCreatedEmail(
                        user.Email,
                        user.FirstName,
                        createResult.OrderNumber,
                        createResult.TotalAmount,
                        createResult.Currency);
                }

                await _cartService.ClearAsync(userId);

                _logger.LogInformation(
                    "Order created successfully. OrderId: {OrderId}, OrderNumber: {OrderNumber}, User: {UserId}, Total: {Total}",
                    createResult.OrderId, createResult.OrderNumber, userId, createResult.TotalAmount);

                return OperationResult<int>.Success(createResult.OrderId, "Sipariş başarıyla oluşturuldu.");
            }
            catch (OrderValidationException ex)
            {
                _logger.LogWarning(ex, "Order validation failed. User: {UserId}", userId);
                return OperationResult<int>.Failure(ex.Message, ResultType.ValidationError);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized order creation attempt. User: {UserId}", userId);
                return OperationResult<int>.Failure(ex.Message, ResultType.Unauthorized);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Order creation failed due to concurrency. User: {UserId}", userId);
                return OperationResult<int>.Failure("Sipariş oluşturulurken eşzamanlı güncelleme tespit edildi. Lütfen tekrar deneyin.", ResultType.Error);
            }
        }

        public async Task<OrderWithDetailsDto> GetByIdAsync(int orderId)
        {
            var currentUserId = GetCurrentUserId();

            var order = await _manager.Order.GetWithDetailsAsync(orderId, false);
            if (order == null)
            {
                throw new OrderNotFoundException(orderId);
            }

            ValidateUserAccess(order.UserId, currentUserId);

            var orderDto = _mapper.Map<OrderWithDetailsDto>(order);

            return orderDto;
        }

        public async Task<OrderWithDetailsDto> GetByNumberAsync(string orderNumber)
        {
            var currentUserId = GetCurrentUserId();

            var order = await _manager.Order.GetByNumberAsync(orderNumber, false);
            if (order == null)
            {
                throw new OrderNotFoundException(orderNumber);
            }

            ValidateUserAccess(order.UserId, currentUserId);

            var orderDto = _mapper.Map<OrderWithDetailsDto>(order);

            return orderDto;
        }

        public async Task<IEnumerable<OrderDto>> GetByUserIdAsync(string userId)
        {
            var currentUserId = GetCurrentUserId();
            ValidateUserAccess(userId, currentUserId);

            var orders = await _manager.Order.GetByUserIdAsync(userId, false);
            var ordersDto = _mapper.Map<IEnumerable<OrderDto>>(orders);

            return ordersDto;
        }

        public async Task<(IEnumerable<OrderDto> orders, int count, int processingCount)> GetAllAdminAsync(OrderRequestParametersAdmin p, CancellationToken ct = default)
        {
            var result = await _manager.Order.GetAllAdminAsync(p, false, ct);
            var ordersDto = _mapper.Map<IEnumerable<OrderDto>>(result.orders);

            return (ordersDto, result.count, result.processingCount);
        }

        public async Task<OperationResult<OrderWithDetailsDto>> UpdateStatusAsync(OrderDtoForUpdate orderDto)
        {
            _manager.ClearTracker();
            var order = await _manager.Order.GetByIdAsync(orderDto.OrderId, true);
            if (order == null)
            {
                return OperationResult<OrderWithDetailsDto>.Failure("Sipariş bulunamadı.", ResultType.NotFound);
            }

            var userId = GetCurrentUserId();
            var hasChanges = false;
            if (orderDto.OrderStatus.HasValue && order.OrderStatus != orderDto.OrderStatus.Value)
            {
                if (orderDto.OrderStatus.Value is OrderStatus.Cancelled or OrderStatus.Returned or OrderStatus.Shipped or OrderStatus.Delivered)
                {
                    return OperationResult<OrderWithDetailsDto>.Failure(
                        "Bu durum geçişi için ilgili özel aksiyonu kullanın (iptal/kargo/teslim/iade).",
                        ResultType.ValidationError);
                }

                order.OrderStatus = orderDto.OrderStatus.Value;
                hasChanges = true;

                OrderEventType eventType = orderDto.OrderStatus.Value switch
                {
                    OrderStatus.Processing => OrderEventType.OrderProcessing,
                    OrderStatus.Shipped => OrderEventType.Shipped,
                    OrderStatus.Delivered => OrderEventType.Delivered,
                    OrderStatus.Cancelled => OrderEventType.Cancelled,
                    OrderStatus.Returned => OrderEventType.Returned,
                    _ => OrderEventType.OrderProcessing
                };

                var historyEntry = OrderHistory.CreateEvent(
                    orderId: order.OrderId,
                    eventType: eventType,
                    description: $"Sipariş durumu değiştirildi: {orderDto.OrderStatus.Value}",
                    userId: userId
                );
                _manager.OrderHistory.Create(historyEntry);
            }

            if (orderDto.PaymentStatus.HasValue && order.PaymentStatus != orderDto.PaymentStatus.Value)
            {
                order.PaymentStatus = orderDto.PaymentStatus.Value;
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(orderDto.TrackingNumber))
            {
                order.TrackingNumber = orderDto.TrackingNumber;
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(orderDto.ShippingCompanyName))
            {
                order.ShippingCompanyName = orderDto.ShippingCompanyName;
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(orderDto.ShippingServiceName))
            {
                order.ShippingServiceName = orderDto.ShippingServiceName;
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(orderDto.AdminNotes))
            {
                order.AdminNotes = orderDto.AdminNotes;
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(orderDto.PaymentProvider))
            {
                order.PaymentProvider = orderDto.PaymentProvider;
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(orderDto.PaymentTransactionId))
            {
                order.PaymentTransactionId = orderDto.PaymentTransactionId;
                hasChanges = true;
            }

            if (hasChanges)
            {
                await _manager.SaveAsync();

                _logger.LogInformation(
                    "Order updated. OrderId: {OrderId}, User: {UserId}",
                    order.OrderId, userId);
            }

            var orderDtoForReturn = _mapper.Map<OrderWithDetailsDto>(order);

            return OperationResult<OrderWithDetailsDto>.Success(orderDtoForReturn, "Sipariş güncellendi.");
        }

        public async Task<OperationResult<OrderWithDetailsDto>> CancelAsync(int orderId, string reason)
        {
            _manager.ClearTracker();
            var userId = GetCurrentUserId();
            var order = await _manager.Order.GetWithDetailsAsync(orderId, true);

            if (order == null)
            {
                return OperationResult<OrderWithDetailsDto>.Failure("Sipariş bulunamadı.", ResultType.NotFound);
            }

            ValidateUserAccess(order.UserId, userId);

            if (!order.CanBeCancelled())
            {
                return OperationResult<OrderWithDetailsDto>.Failure(
                    "Bu sipariş iptal edilemez. Sadece beklemede veya işleniyor durumundaki siparişler iptal edilebilir.",
                    ResultType.ValidationError);
            }

            if (order.PaymentStatus == PaymentStatus.Completed)
            {
                _logger.LogInformation("Order has completed payment, initiating refund. OrderId: {OrderId}", orderId);

                var paymentTransactions = await _manager.OrderLinePaymentTransaction
                    .GetByOrderIdAsync(orderId, true);

                if (paymentTransactions != null && paymentTransactions.Any())
                {
                    var userIp = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                    var refundErrors = new List<string>();

                    foreach (var transaction in paymentTransactions)
                    {
                        if (transaction!.IsRefunded)
                        {
                            _logger.LogInformation("Transaction already refunded. PaymentTransactionId: {PaymentTransactionId}",
                                transaction.PaymentTransactionId);
                            continue;
                        }

                        var refundRequest = new IyzicoRefundRequest
                        {
                            PaymentTransactionId = transaction.PaymentTransactionId,
                            Price = transaction.PaidPrice,
                            Currency = order.Currency,
                            Ip = userIp,
                            Reason = "BUYER_REQUEST",
                            Description = $"Sipariş iptali - {order.OrderNumber} - {reason}"
                        };

                        var refundResult = await _paymentProvider.RefundAsync(refundRequest);

                        if (refundResult.IsSuccess && refundResult.Data != null)
                        {
                            transaction.IsRefunded = true;
                            transaction.RefundTransactionId = refundResult.Data.PaymentTransactionId;
                            transaction.RefundedAt = DateTime.UtcNow;
                            _manager.OrderLinePaymentTransaction.Update(transaction);

                            _logger.LogInformation(
                                "Payment refunded for cancelled order. PaymentTransactionId: {PaymentTransactionId}",
                                transaction.PaymentTransactionId);
                        }
                        else
                        {
                            var errorMsg = $"Item {transaction.ItemId}: {refundResult.Message}";
                            refundErrors.Add(errorMsg);
                            _logger.LogError("Refund failed for cancelled order item. Error: {Error}", errorMsg);
                        }
                    }

                    if (refundErrors.Any())
                    {
                        await _manager.SaveAsync();
                        return OperationResult<OrderWithDetailsDto>.Failure(
                            $"Sipariş iptal edildi ancak ödeme iadesi kısmen başarısız: {string.Join(", ", refundErrors)}. Lütfen müşteri hizmetleri ile iletişime geçin.",
                            ResultType.Error);
                    }

                    order.PaymentStatus = PaymentStatus.Refunded;
                    _logger.LogInformation("Payment fully refunded for cancelled order. OrderId: {OrderId}", orderId);
                }
                else
                {
                    _logger.LogWarning("No payment transactions found for paid order cancellation. OrderId: {OrderId}", orderId);
                }
            }

            order.Cancel(reason);

            var historyEntry = OrderHistory.CreateEvent(
                orderId: order.OrderId,
                eventType: OrderEventType.Cancelled,
                description: $"Sipariş müşteri tarafından iptal edildi. Sebep: {reason}",
                userId: userId
            );
            _manager.OrderHistory.Create(historyEntry);

            foreach (var line in order.Lines)
            {
                var product = await _manager.Product.GetByIdAsync(line.ProductId, true, true);
                if (product != null)
                {
                    var variant = await _manager.ProductVariant.GetByIdAsync(line.ProductVariantId, false, true);
                    if (variant != null)
                    {
                        variant.Stock += line.Quantity;
                        _manager.ProductVariant.Update(variant);

                        _logger.LogInformation("Stock restored for variant {VariantId}: +{Quantity}", variant.ProductVariantId, line.Quantity);
                    }

                    _manager.Product.Update(product);
                }
            }

            await _manager.SaveAsync();

            var successMessage = order.PaymentStatus == PaymentStatus.Refunded
                ? "Sipariş başarıyla iptal edildi ve ödeme iadesi yapıldı."
                : "Sipariş başarıyla iptal edildi.";

            _logger.LogInformation("Order cancelled successfully. OrderId: {OrderId}, User: {UserId}, PaymentRefunded: {PaymentRefunded}",
                orderId, userId, order.PaymentStatus == PaymentStatus.Refunded);

            var orderDto = _mapper.Map<OrderWithDetailsDto>(order);
            return OperationResult<OrderWithDetailsDto>.Success(orderDto, successMessage);
        }

        public async Task<OperationResult<OrderWithDetailsDto>> MarkAsShippedAsync(int orderId, string trackingNumber, string? companyName, string? serviceName)
        {
            _manager.ClearTracker();
            var order = await _manager.Order.GetByIdAsync(orderId, true);
            if (order == null)
            {
                return OperationResult<OrderWithDetailsDto>.Failure("Sipariş bulunamadı.", ResultType.NotFound);
            }

            var userId = GetCurrentUserId();
            await _manager.ExecuteInTransactionAsync(async ct =>
            {
                order.MarkAsShipped(trackingNumber, companyName, serviceName);

                var historyEntry = OrderHistory.CreateEvent(
                    orderId: order.OrderId,
                    eventType: OrderEventType.Shipped,
                    description: $"Sipariş kargoya verildi. Takip No: {trackingNumber}",
                    userId: userId
                );
                _manager.OrderHistory.Create(historyEntry);
            }, IsolationLevel.ReadCommitted);

            var user = await _manager.User.GetByIdAsync(order.UserId, false);
            if (user?.Email is not null)
            {
                _emailQueueService.EnqueueOrderShippedEmail(
                    user.Email,
                    user.FirstName,
                    order.OrderNumber,
                    trackingNumber,
                    companyName);
            }

            _logger.LogInformation(
                "Order marked as shipped. OrderId: {OrderId}, TrackingNumber: {TrackingNumber}",
                orderId, trackingNumber);

            var orderDto = _mapper.Map<OrderWithDetailsDto>(order);
            return OperationResult<OrderWithDetailsDto>.Success(orderDto, "Sipariş kargoya verildi.");
        }

        public async Task<OperationResult<OrderWithDetailsDto>> MarkAsDeliveredAsync(int orderId)
        {
            _manager.ClearTracker();
            var order = await _manager.Order.GetByIdAsync(orderId, true);
            if (order == null)
            {
                return OperationResult<OrderWithDetailsDto>.Failure("Sipariş bulunamadı.", ResultType.NotFound);
            }

            var userId = GetCurrentUserId();
            await _manager.ExecuteInTransactionAsync(async ct =>
            {
                order.MarkAsDelivered();

                var historyEntry = OrderHistory.CreateEvent(
                    orderId: order.OrderId,
                    eventType: OrderEventType.Delivered,
                    description: "Sipariş teslim edildi",
                    userId: userId
                );
                _manager.OrderHistory.Create(historyEntry);
            }, IsolationLevel.ReadCommitted);

            var user = await _manager.User.GetByIdAsync(order.UserId, false);
            if (user?.Email is not null)
            {
                _emailQueueService.EnqueueOrderDeliveredEmail(
                    user.Email,
                    user.FirstName,
                    order.OrderNumber);
            }

            _logger.LogInformation(
                "Order marked as delivered. OrderId: {OrderId}",
                orderId);

            var orderDto = _mapper.Map<OrderWithDetailsDto>(order);
            return OperationResult<OrderWithDetailsDto>.Success(orderDto, "Sipariş teslim edildi.");
        }

        public async Task<OperationResult<OrderWithDetailsDto>> HandlePaymentCallbackAsync(PaymentCallbackDto callback)
        {
            if (string.IsNullOrWhiteSpace(callback.Token))
            {
                if (!string.IsNullOrWhiteSpace(callback.OrderNumber))
                {
                    var existing = await _manager.Order.GetByNumberAsync(callback.OrderNumber, true);
                    if (existing != null && existing.PaymentStatus == PaymentStatus.Completed)
                    {
                        var existingDto = _mapper.Map<OrderWithDetailsDto>(existing);
                        return OperationResult<OrderWithDetailsDto>.Success(existingDto);
                    }

                    if (existing != null && !string.IsNullOrWhiteSpace(callback.TransactionId))
                    {
                        await _manager.ExecuteInTransactionAsync(async ct =>
                        {
                            if (callback.IsSuccess)
                            {
                                existing.MarkAsPaid(callback.TransactionId, callback.Provider ?? "Iyzico");
                                var paidHistory = OrderHistory.CreateEvent(
                                    orderId: existing.OrderId,
                                    eventType: OrderEventType.PaymentCompleted,
                                    description: $"Webhook ödeme onayı alındı. İşlem ID: {callback.TransactionId}",
                                    userId: null,
                                    isSystemEvent: true
                                );
                                _manager.OrderHistory.Create(paidHistory);

                                // TODO: Enqueue e-arşiv fatura oluşturma background job'u.
                            }
                            else
                            {
                                existing.MarkAsFailed(callback.FailureReason ?? "Webhook ödeme başarısız.");
                                foreach (var line in existing.Lines)
                                {
                                    var variant = await _manager.ProductVariant.GetByIdAsync(line.ProductVariantId, false, true);
                                    if (variant == null)
                                        continue;

                                    variant.Stock += line.Quantity;
                                    _manager.ProductVariant.Update(variant);
                                }
                            }
                        }, IsolationLevel.ReadCommitted);

                        var existingDto = _mapper.Map<OrderWithDetailsDto>(existing);
                        return OperationResult<OrderWithDetailsDto>.Success(existingDto);
                    }
                }

                return OperationResult<OrderWithDetailsDto>.Failure("Ödeme doğrulama token bilgisi eksik.", ResultType.ValidationError);
            }

            var retrieveResult = await _paymentProvider.VerifyAsync(callback.Token);
            if (!retrieveResult.IsSuccess || retrieveResult.Data == null)
            {
                _logger.LogWarning("Payment callback retrieval failed. Token: {Token}", callback.Token);
                return OperationResult<OrderWithDetailsDto>.Failure(
                    "Ödeme sonucu alınamadı.",
                    ResultType.Error);
            }

            var paymentResult = retrieveResult.Data;

            var order = await _manager.ExecuteInTransactionAsync(async token =>
            {
                _manager.ClearTracker();
                var loadedOrder = await _manager.Order.GetByNumberAsync(paymentResult.BasketId, true);
                if (loadedOrder == null)
                {
                    return (Order?)null;
                }

                if (loadedOrder.PaymentStatus == PaymentStatus.Completed)
                {
                    if (loadedOrder.PaymentTransactionId == paymentResult.PaymentId)
                    {
                        return loadedOrder;
                    }

                    _logger.LogWarning(
                        "Order already paid with different transaction. OrderId: {OrderId}, ExistingTx: {ExistingTx}, IncomingTx: {IncomingTx}",
                        loadedOrder.OrderId, loadedOrder.PaymentTransactionId, paymentResult.PaymentId);
                    return loadedOrder;
                }

                if (paymentResult.PaymentStatus == "SUCCESS" && paymentResult.FraudStatus == 1)
                {
                    string? bankName = null;
                    if (!string.IsNullOrEmpty(paymentResult.BinNumber))
                    {
                        try
                        {
                            var binResult = await _paymentProvider.GetBinDetailsAsync(paymentResult.BinNumber);
                            if (binResult.IsSuccess && binResult.Data != null)
                            {
                                bankName = binResult.Data.BankName;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to get BIN details. BinNumber: {BinNumber}", paymentResult.BinNumber);
                        }
                    }

                    loadedOrder.MarkAsPaid(
                        paymentResult.PaymentId,
                        "Iyzico",
                        cardType: paymentResult.CardType,
                        cardAssociation: paymentResult.CardAssociation,
                        cardFamily: paymentResult.CardFamily,
                        bankName: bankName,
                        installmentCount: paymentResult.Installment,
                        lastFourDigits: paymentResult.LastFourDigits
                    );

                    if (paymentResult.ItemTransactions != null && paymentResult.ItemTransactions.Any())
                    {
                        foreach (var item in paymentResult.ItemTransactions)
                        {
                            var orderLine = loadedOrder.Lines.FirstOrDefault(l => item.ItemId == l.OrderLineId.ToString());
                            if (orderLine == null)
                            {
                                _logger.LogWarning(
                                    "Could not match itemTransaction to order line. ItemId: {ItemId}, OrderId: {OrderId}",
                                    item.ItemId, loadedOrder.OrderId);
                                continue;
                            }

                            var existingTransaction = await _manager.OrderLinePaymentTransaction
                                .GetByOrderLineIdAsync(orderLine.OrderLineId, false);

                            if (existingTransaction == null)
                            {
                                var paymentTransaction = new OrderLinePaymentTransaction
                                {
                                    OrderLineId = orderLine.OrderLineId,
                                    ItemId = item.ItemId,
                                    PaymentTransactionId = item.PaymentTransactionId,
                                    TransactionStatus = item.TransactionStatus,
                                    Price = item.Price,
                                    PaidPrice = item.PaidPrice,
                                    IsRefunded = false,
                                    CreatedAt = DateTime.UtcNow
                                };

                                _manager.OrderLinePaymentTransaction.Create(paymentTransaction);
                            }
                        }
                    }

                    var successHistory = OrderHistory.CreateEvent(
                        orderId: loadedOrder.OrderId,
                        eventType: OrderEventType.PaymentCompleted,
                        description: $"Ödeme alındı. İşlem ID: {paymentResult.PaymentId}, Kart: {paymentResult.CardAssociation} {paymentResult.LastFourDigits}",
                        userId: null,
                        isSystemEvent: true
                    );
                    _manager.OrderHistory.Create(successHistory);

                    // TODO: Enqueue e-arşiv fatura oluşturma background job'u.
                }
                else
                {
                    var failureReason = paymentResult.PaymentStatus != "SUCCESS"
                        ? $"Ödeme başarısız: {paymentResult.ErrorMessage}"
                        : $"Fraud kontrolü başarısız. FraudStatus: {paymentResult.FraudStatus}";

                    loadedOrder.MarkAsFailed(failureReason);

                    foreach (var line in loadedOrder.Lines)
                    {
                        var variant = await _manager.ProductVariant.GetByIdAsync(line.ProductVariantId, false, true);
                        if (variant == null)
                            continue;

                        variant.Stock += line.Quantity;
                        _manager.ProductVariant.Update(variant);
                    }

                    var failedHistory = OrderHistory.CreateEvent(
                        orderId: loadedOrder.OrderId,
                        eventType: OrderEventType.PaymentFailed,
                        description: failureReason,
                        userId: null,
                        isSystemEvent: true
                    );
                    _manager.OrderHistory.Create(failedHistory);
                }

                return loadedOrder;
            }, IsolationLevel.ReadCommitted, CancellationToken.None);

            if (order == null)
            {
                _logger.LogError("Order not found for payment callback. BasketId: {BasketId}", paymentResult.BasketId);
                return OperationResult<OrderWithDetailsDto>.Failure("Sipariş bulunamadı.", ResultType.NotFound);
            }

            var orderDto = _mapper.Map<OrderWithDetailsDto>(order);
            return OperationResult<OrderWithDetailsDto>.Success(orderDto);
        }

        public async Task<OperationResult<OrderWithDetailsDto>> RefundAsync(int orderId)
        {
            _manager.ClearTracker();
            var order = await _manager.Order.GetWithDetailsAsync(orderId, true);
            if (order == null)
            {
                return OperationResult<OrderWithDetailsDto>.Failure("Sipariş bulunamadı.", ResultType.NotFound);
            }
            if (!order.CanBeRefunded())
            {
                return OperationResult<OrderWithDetailsDto>.Failure("Bu sipariş iade edilemez.", ResultType.ValidationError);
            }
            var paymentTransactions = await _manager.OrderLinePaymentTransaction
                .GetByOrderIdAsync(orderId, true);
            if (!paymentTransactions.Any() || paymentTransactions == null)
            {
                _logger.LogWarning("No payment transactions found for refund. OrderId: {OrderId}", orderId);
                return OperationResult<OrderWithDetailsDto>.Failure(
                    "İade için ödeme bilgisi bulunamadı. Manuel iade gerekiyor.",
                    ResultType.ValidationError);
            }
            var userId = GetCurrentUserId();
            var userIp = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var refundErrors = new List<string>();
            foreach (var transaction in paymentTransactions)
            {
                if (transaction!.IsRefunded)
                {
                    _logger.LogInformation("Transaction already refunded. PaymentTransactionId: {PaymentTransactionId}",
                        transaction.PaymentTransactionId);
                    continue;
                }
                var refundRequest = new IyzicoRefundRequest
                {
                    PaymentTransactionId = transaction.PaymentTransactionId,
                    Price = transaction.PaidPrice,
                    Currency = order.Currency,
                    Ip = userIp,
                    Reason = "BUYER_REQUEST",
                    Description = $"Sipariş iadesi - {order.OrderNumber}"
                };
                var refundResult = await _paymentProvider.RefundAsync(refundRequest);
                if (refundResult.IsSuccess && refundResult.Data != null)
                {
                    transaction.IsRefunded = true;
                    transaction.RefundTransactionId = refundResult.Data.PaymentTransactionId;
                    transaction.RefundedAt = DateTime.UtcNow;
                    _manager.OrderLinePaymentTransaction.Update(transaction);
                    _logger.LogInformation(
                        "Item refunded successfully. PaymentTransactionId: {PaymentTransactionId}",
                        transaction.PaymentTransactionId);
                }
                else
                {
                    var errorMsg = $"Item {transaction.ItemId}: {refundResult.Message}";
                    refundErrors.Add(errorMsg);
                    _logger.LogError("Refund failed for item. Error: {Error}", errorMsg);
                }
            }
            if (refundErrors.Any())
            {
                await _manager.SaveAsync();
                return OperationResult<OrderWithDetailsDto>.Failure(
                    $"İade işlemi kısmen başarısız: {string.Join(", ", refundErrors)}",
                    ResultType.Error);
            }
            order.MarkAsRefunded();
            var historyEntry = OrderHistory.CreateEvent(
                orderId: order.OrderId,
                eventType: OrderEventType.Refunded,
                description: "Sipariş iade edildi. İyzico üzerinden otomatik iade yapıldı.",
                userId: userId
            );
            _manager.OrderHistory.Create(historyEntry);
            foreach (var line in order.Lines)
            {
                var product = await _manager.Product.GetByIdAsync(line.ProductId, true, true);
                if (product != null)
                {
                    var variant = await _manager.ProductVariant.GetByIdAsync(line.ProductVariantId, false, true);
                    if (variant != null)
                    {
                        variant.Stock += line.Quantity;
                        _manager.ProductVariant.Update(variant);
                    }

                    _manager.Product.Update(product);
                    _logger.LogInformation("Stock restored for refunded order. ProductId: {ProductId}, Quantity: +{Quantity}", line.ProductId, line.Quantity);
                }
            }
            await _manager.SaveAsync();
            _logger.LogInformation(
                "Order refunded successfully. OrderId: {OrderId}, User: {UserId}",
                orderId, userId);
            var orderDto = _mapper.Map<OrderWithDetailsDto>(order);
            return OperationResult<OrderWithDetailsDto>.Success(orderDto, "Sipariş başarıyla iade edildi.");
        }

        public async Task<IEnumerable<ProductSalesDto>> GetTopSellingProductsAsync(int topN, CancellationToken ct = default)
        {
            return await _cache.GetOrCreateAsync(
                "orders:topSelling",
                async token =>
                {
                    return await _manager.Order.GetTopSellingProductsAsync(topN, token);
                },
                absoluteExpiration: TimeSpan.FromMinutes(5),
                slidingExpiration: TimeSpan.FromMinutes(2),
                ct: ct
            );
        }

        public async Task<int> CountByUserIdAsync(string userId)
        {
            return await _manager.Order.CountByUserIdAsync(userId);
        }

        public async Task<decimal> GetUserTotalSpentAsync(string userId)
        {
            return await _manager.Order.GetUserTotalSpentAsync(userId);
        }

        public async Task<int> CountOfInProcessAsync(CancellationToken ct = default)
        {
            return await _cache.GetOrCreateAsync("orders:topSelling",
                async token =>
                {
                    return await _manager.Order.CountOfInProcessAsync(token);
                },
                absoluteExpiration: TimeSpan.FromMinutes(5),
                slidingExpiration: TimeSpan.FromMinutes(2),
                ct: ct
            );

        }

        public async Task<OperationResult<OrderWithDetailsDto>> AddAdminNoteAsync(int orderId, string note)
        {
            if (string.IsNullOrWhiteSpace(note))
            {
                return OperationResult<OrderWithDetailsDto>.Failure("Not boş olamaz.", ResultType.ValidationError);
            }

            _manager.ClearTracker();
            var order = await _manager.Order.GetByIdAsync(orderId, true);
            if (order == null)
            {
                return OperationResult<OrderWithDetailsDto>.Failure("Sipariş bulunamadı.", ResultType.NotFound);
            }

            var userId = GetCurrentUserId();
            var userName = GetCurrentUserName();

            if (string.IsNullOrWhiteSpace(order.AdminNotes))
            {
                order.AdminNotes = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] {userName}: {note}";
            }
            else
            {
                order.AdminNotes += $"\n[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] {userName}: {note}";
            }

            var historyEntry = OrderHistory.CreateEvent(
                orderId: order.OrderId,
                eventType: OrderEventType.OrderProcessing,
                description: $"Admin notu eklendi: {note}",
                userId: userId
            );
            _manager.OrderHistory.Create(historyEntry);

            await _manager.SaveAsync();

            _logger.LogInformation(
                "Admin note added to order. OrderId: {OrderId}, User: {UserId}",
                orderId, userId);

            var orderDto = _mapper.Map<OrderWithDetailsDto>(order);
            return OperationResult<OrderWithDetailsDto>.Success(orderDto, "Admin notu eklendi.");
        }

        public async Task<OperationResult<OrderWithDetailsDto>> InitiateReturnAsync(int orderId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return OperationResult<OrderWithDetailsDto>.Failure("İade nedeni belirtilmelidir.", ResultType.ValidationError);
            }

            _manager.ClearTracker();
            var order = await _manager.Order.GetWithDetailsAsync(orderId, true);
            if (order == null)
            {
                return OperationResult<OrderWithDetailsDto>.Failure("Sipariş bulunamadı.", ResultType.NotFound);
            }

            if (order.OrderStatus != OrderStatus.Delivered)
            {
                return OperationResult<OrderWithDetailsDto>.Failure(
                    "Sadece teslim edilmiş siparişler iade edilebilir.",
                    ResultType.ValidationError);
            }

            var userId = GetCurrentUserId();

            if (order.PaymentStatus == PaymentStatus.Completed)
            {
                _logger.LogInformation("Processing refund for admin-initiated return. OrderId: {OrderId}", orderId);

                var paymentTransactions = await _manager.OrderLinePaymentTransaction
                    .GetByOrderIdAsync(orderId, true);

                if (paymentTransactions != null && paymentTransactions.Any())
                {
                    var userIp = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                    var refundErrors = new List<string>();

                    foreach (var transaction in paymentTransactions)
                    {
                        if (transaction!.IsRefunded)
                        {
                            _logger.LogInformation("Transaction already refunded. PaymentTransactionId: {PaymentTransactionId}",
                                transaction.PaymentTransactionId);
                            continue;
                        }

                        var refundRequest = new IyzicoRefundRequest
                        {
                            PaymentTransactionId = transaction.PaymentTransactionId,
                            Price = transaction.PaidPrice,
                            Currency = order.Currency,
                            Ip = userIp,
                            Reason = "SELLER_REQUEST",
                            Description = $"Admin tarafından iade başlatıldı - {order.OrderNumber} - {reason}"
                        };

                        var refundResult = await _paymentProvider.RefundAsync(refundRequest);

                        if (refundResult.IsSuccess && refundResult.Data != null)
                        {
                            transaction.IsRefunded = true;
                            transaction.RefundTransactionId = refundResult.Data.PaymentTransactionId;
                            transaction.RefundedAt = DateTime.UtcNow;
                            _manager.OrderLinePaymentTransaction.Update(transaction);

                            _logger.LogInformation(
                                "Payment refunded for admin-initiated return. PaymentTransactionId: {PaymentTransactionId}",
                                transaction.PaymentTransactionId);
                        }
                        else
                        {
                            var errorMsg = $"Item {transaction.ItemId}: {refundResult.Message}";
                            refundErrors.Add(errorMsg);
                            _logger.LogError("Refund failed for admin-initiated return. Error: {Error}", errorMsg);
                        }
                    }

                    if (refundErrors.Any())
                    {
                        await _manager.SaveAsync();
                        return OperationResult<OrderWithDetailsDto>.Failure(
                            $"İade başlatıldı ancak ödeme iadesi kısmen başarısız: {string.Join(", ", refundErrors)}. Lütfen manuel işlem yapın.",
                            ResultType.Error);
                    }

                    order.PaymentStatus = PaymentStatus.Refunded;
                    _logger.LogInformation("Payment fully refunded for admin-initiated return. OrderId: {OrderId}", orderId);
                }
                else
                {
                    _logger.LogWarning("No payment transactions found for admin-initiated return. OrderId: {OrderId}", orderId);
                }
            }

            order.OrderStatus = OrderStatus.Returned;

            var historyEntry = OrderHistory.CreateEvent(
                orderId: order.OrderId,
                eventType: OrderEventType.Returned,
                description: $"Sipariş admin tarafından iade edildi. Sebep: {reason}",
                userId: userId
            );
            _manager.OrderHistory.Create(historyEntry);

            foreach (var line in order.Lines)
            {
                var product = await _manager.Product.GetByIdAsync(line.ProductId, true, true);
                if (product != null)
                {
                    var variant = await _manager.ProductVariant.GetByIdAsync(line.ProductVariantId, false, true);
                    if (variant != null)
                    {
                        variant.Stock += line.Quantity;
                        _manager.ProductVariant.Update(variant);

                        _logger.LogInformation("Stock restored for admin-initiated return. VariantId: {VariantId}, Quantity: +{Quantity}",
                            variant.ProductVariantId, line.Quantity);
                    }

                    _manager.Product.Update(product);
                }
            }

            await _manager.SaveAsync();

            var successMessage = order.PaymentStatus == PaymentStatus.Refunded
                ? "Sipariş başarıyla iade edildi ve ödeme iadesi yapıldı."
                : "Sipariş başarıyla iade edildi.";

            _logger.LogInformation("Admin-initiated return completed. OrderId: {OrderId}, User: {UserId}, PaymentRefunded: {PaymentRefunded}",
                orderId, userId, order.PaymentStatus == PaymentStatus.Refunded);

            var orderDto = _mapper.Map<OrderWithDetailsDto>(order);
            return OperationResult<OrderWithDetailsDto>.Success(orderDto, successMessage);
        }
    }
}
