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
using Polly;
using Polly.Retry;
using System.Security.Claims;

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
        private readonly ResiliencePipeline _retryPipeline;

        public OrderManager(
            IRepositoryManager manager,
            IMapper mapper,
            ILogger<OrderManager> logger,
            IHttpContextAccessor httpContextAccessor,
            ISecurityLogService securityLogService,
            IPaymentProvider paymentProvider,
            ICouponService couponService,
            ICampaignService campaignService,
            ICartService cartService)
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

            // Resilience pipeline for database operations
            _retryPipeline = new ResiliencePipelineBuilder()
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromMilliseconds(100),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    ShouldHandle = new PredicateBuilder()
                        .Handle<DbUpdateConcurrencyException>()
                        .Handle<DbUpdateException>()
                        .Handle<TimeoutException>(),
                    OnRetry = args =>
                    {
                        _logger.LogWarning(
                            "{Exception} hatası nedeniyle işlem {Duration}ms sonra {RetryCount}. kez tekrar ediliyor.",
                            args.Outcome.Exception?.Message, args.RetryDelay.TotalMilliseconds, args.AttemptNumber);
                        return ValueTask.CompletedTask;
                    }
                })
                .Build();
        }

        private string GetCurrentUserId() =>
            _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

        private string GetCurrentUserName() =>
            _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

        private void ValidateUserAccess(string? requestedUserId, string currentUserId)
        {
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

        public async Task<OperationResult<int>> CreateOrderAsync(OrderDtoForCreation orderDto)
        {
            var userId = GetCurrentUserId();
            try
            {
                return await _retryPipeline.ExecuteAsync(async cancellationToken =>
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
                        var product = await _manager.Product.GetOneProductAsync(cartLine.ProductId, false);
                        if (product == null)
                        {
                            return OperationResult<int>.Failure($"Ürün bulunamadı: {cartLine.ProductName}", ResultType.NotFound);
                        }

                        if (product.Stock < cartLine.Quantity)
                        {
                            return OperationResult<int>.Failure($"Yetersiz stok: {product.ProductName}. Mevcut: {product.Stock}", ResultType.ValidationError);
                        }
                        var category = await _manager.Category.GetOneCategoryAsync(product.CategoryId, false);

                        if (category == null || category.CategoryName == null)
                        {
                            return OperationResult<int>.Failure($"Kategori bulunamadı: ID {product.CategoryId}", ResultType.NotFound);
                        }

                        var orderLine = new OrderLine
                        {
                            ProductId = cartLine.ProductId,
                            ProductName = cartLine.ProductName,
                            CategoryName = category.CategoryName,
                            SubCategoryName = category.ParentCategory != null ? category.ParentCategory.CategoryName : null,
                            Quantity = cartLine.Quantity,
                            ActualPrice = product.ActualPrice,
                            DiscountPrice = product.DiscountPrice,
                            ImageUrl = cartLine.ImageUrl
                        };

                        orderLine.ValidateForCreation();
                        order.Lines.Add(orderLine);

                        product.DecreaseStock(cartLine.Quantity);
                    }

                    order.SubTotal = order.Lines.Sum(l => (l.DiscountPrice ?? l.ActualPrice) * l.Quantity);

                    decimal remainingAmount = order.SubTotal;

                    if (!string.IsNullOrWhiteSpace(orderDto.CouponCode))
                    {
                        var couponResult = await _couponService.ValidateCouponForOrderAsync(orderDto.CouponCode, order.SubTotal, userId);
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

                    var applicableCampaigns = await _campaignService.GetApplicableCampaignsAsync(order.SubTotal);
                    
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

                    _manager.Order.CreateOrder(order);

                    var currentUserId = GetCurrentUserId();
                    var historyEntry = OrderHistory.CreateEvent(
                        orderId: 0, // Will be set after save
                        eventType: OrderEventType.OrderCreated,
                        description: "Sipariş oluşturuldu",
                        userId: currentUserId
                    );
                    order.History.Add(historyEntry);

                    await _manager.SaveAsync();

                    if (!string.IsNullOrWhiteSpace(order.CouponCode))
                    {
                        var coupon = await _manager.Coupon.GetCouponByCodeAsync(order.CouponCode, true);
                        if (coupon != null)
                        {
                            coupon.IncrementUsage();
                            
                            var couponUsage = new CouponUsage
                            {
                                CouponId = coupon.CouponId,
                                UserId = userId,
                                OrderId = order.OrderId,
                                UsedAt = DateTime.UtcNow
                            };
                            _manager.CouponUsage.CreateCouponUsage(couponUsage);
                            await _manager.SaveAsync();
                        }
                    }

                    _logger.LogInformation(
                        "Order created successfully. OrderId: {OrderId}, OrderNumber: {OrderNumber}, User: {UserId}, Total: {Total}",
                        order.OrderId, order.OrderNumber, userId, order.TotalAmount);

                    await _cartService.ClearCartAsync(userId);

                    return OperationResult<int>.Success(order.OrderId, "Sipariş başarıyla oluşturuldu.");
                }, CancellationToken.None);
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
        }

        public async Task<OrderWithDetailsDto> GetOrderByIdAsync(int orderId)
        {
            var currentUserId = GetCurrentUserId();
            
            var order = await _manager.Order.GetOrderWithDetailsAsync(orderId, false);
            if (order == null)
            {
                throw new OrderNotFoundException(orderId);
            }

            ValidateUserAccess(order.UserId, currentUserId);

            var orderDto = _mapper.Map<OrderWithDetailsDto>(order);

            return orderDto;
        }

        public async Task<OrderWithDetailsDto> GetOrderByNumberAsync(string orderNumber)
        {
            var currentUserId = GetCurrentUserId();
            
            var order = await _manager.Order.GetOrderByNumberAsync(orderNumber, false);
            if (order == null)
            {
                throw new OrderNotFoundException(orderNumber);
            }

            ValidateUserAccess(order.UserId, currentUserId);

            var orderDto = _mapper.Map<OrderWithDetailsDto>(order);

            return orderDto;
        }

        public async Task<IEnumerable<OrderDto>> GetUserOrdersAsync(string userId)
        {
            var currentUserId = GetCurrentUserId();
            ValidateUserAccess(userId, currentUserId);

            var orders = await _manager.Order.GetUserOrdersAsync(userId, false);
            var ordersDto = _mapper.Map<IEnumerable<OrderDto>>(orders);

            return ordersDto;
        }

        public async Task<OperationResult<OrderWithDetailsDto>> UpdateOrderStatusAsync(OrderDtoForUpdate orderDto)
        {
            _manager.ClearTracker();
            var order = await _manager.Order.GetOrderByIdAsync(orderDto.OrderId, true);
            if (order == null)
            {
                return OperationResult<OrderWithDetailsDto>.Failure("Sipariş bulunamadı.", ResultType.NotFound);
            }

            var userId = GetCurrentUserId();
            var hasChanges = false;
            if (orderDto.OrderStatus.HasValue && order.OrderStatus != orderDto.OrderStatus.Value)
            {
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
                _manager.OrderHistory.CreateOrderHistory(historyEntry);
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

        public async Task<OperationResult<OrderWithDetailsDto>> CancelOrderAsync(int orderId, string reason)
        {
            _manager.ClearTracker();
            var userId = GetCurrentUserId();
            var order = await _manager.Order.GetOrderWithDetailsAsync(orderId, true);

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

            // Check if payment was completed - if so, refund it
            if (order.PaymentStatus == PaymentStatus.Completed)
            {
                _logger.LogInformation("Order has completed payment, initiating refund. OrderId: {OrderId}", orderId);

                // Get payment transactions for this order
                var paymentTransactions = await _manager.OrderLinePaymentTransaction
                    .GetByOrderIdAsync(orderId, true);

                if (paymentTransactions != null && paymentTransactions.Any())
                {
                    var userIp = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                    var refundErrors = new List<string>();

                    // Refund each item transaction
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

                        var refundResult = await _paymentProvider.RefundPaymentAsync(refundRequest);

                        if (refundResult.IsSuccess && refundResult.Data != null)
                        {
                            // Mark transaction as refunded
                            transaction.IsRefunded = true;
                            transaction.RefundTransactionId = refundResult.Data.PaymentTransactionId;
                            transaction.RefundedAt = DateTime.UtcNow;
                            _manager.OrderLinePaymentTransaction.UpdateOrderLinePaymentTransaction(transaction);

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

                    // If any refund failed, return error
                    if (refundErrors.Any())
                    {
                        return OperationResult<OrderWithDetailsDto>.Failure(
                            $"Sipariş iptal edildi ancak ödeme iadesi kısmen başarısız: {string.Join(", ", refundErrors)}. Lütfen müşteri hizmetleri ile iletişime geçin.",
                            ResultType.Error);
                    }

                    // Mark payment as refunded
                    order.PaymentStatus = PaymentStatus.Refunded;
                    _logger.LogInformation("Payment fully refunded for cancelled order. OrderId: {OrderId}", orderId);
                }
                else
                {
                    _logger.LogWarning("No payment transactions found for paid order cancellation. OrderId: {OrderId}", orderId);
                }
            }

            // Cancel the order
            order.Cancel(reason);

            // Create history entry
            var historyEntry = OrderHistory.CreateEvent(
                orderId: order.OrderId,
                eventType: OrderEventType.Cancelled,
                description: $"Sipariş müşteri tarafından iptal edildi. Sebep: {reason}",
                userId: userId
            );
            _manager.OrderHistory.CreateOrderHistory(historyEntry);

            // Restore product stock
            foreach (var line in order.Lines)
            {
                var product = await _manager.Product.GetOneProductAsync(line.ProductId, true);
                if (product != null)
                {
                    product.IncreaseStock(line.Quantity);
                    _logger.LogInformation("Stock restored for product {ProductId}: +{Quantity}",
                        line.ProductId, line.Quantity);
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
            var order = await _manager.Order.GetOrderByIdAsync(orderId, true);
            if (order == null)
            {
                return OperationResult<OrderWithDetailsDto>.Failure("Sipariş bulunamadı.", ResultType.NotFound);
            }

            var userId = GetCurrentUserId();
            order.MarkAsShipped(trackingNumber, companyName, serviceName);

            var historyEntry = OrderHistory.CreateEvent(
                orderId: order.OrderId,
                eventType: OrderEventType.Shipped,
                description: $"Sipariş kargoya verildi. Takip No: {trackingNumber}",
                userId: userId
            );
            _manager.OrderHistory.CreateOrderHistory(historyEntry);

            await _manager.SaveAsync();

            _logger.LogInformation(
                "Order marked as shipped. OrderId: {OrderId}, TrackingNumber: {TrackingNumber}",
                orderId, trackingNumber);

            var orderDto = _mapper.Map<OrderWithDetailsDto>(order);
            return OperationResult<OrderWithDetailsDto>.Success(orderDto, "Sipariş kargoya verildi.");
        }

        public async Task<OperationResult<OrderWithDetailsDto>> MarkAsDeliveredAsync(int orderId)
        {
            _manager.ClearTracker();
            var order = await _manager.Order.GetOrderByIdAsync(orderId, true);
            if (order == null)
            {
                return OperationResult<OrderWithDetailsDto>.Failure("Sipariş bulunamadı.", ResultType.NotFound);
            }

            var userId = GetCurrentUserId();
            order.MarkAsDelivered();

            var historyEntry = OrderHistory.CreateEvent(
                orderId: order.OrderId,
                eventType: OrderEventType.Delivered,
                description: "Sipariş teslim edildi",
                userId: userId
            );
            _manager.OrderHistory.CreateOrderHistory(historyEntry);

            await _manager.SaveAsync();

            _logger.LogInformation(
                "Order marked as delivered. OrderId: {OrderId}",
                orderId);

            var orderDto = _mapper.Map<OrderWithDetailsDto>(order);
            return OperationResult<OrderWithDetailsDto>.Success(orderDto, "Sipariş teslim edildi.");
        }

        public async Task<OperationResult<OrderWithDetailsDto>> HandlePaymentCallbackAsync(PaymentCallbackDto callback)
        {
            var retrieveResult = await _paymentProvider.VerifyPaymentAsync(callback.Token);
            if (!retrieveResult.IsSuccess || retrieveResult.Data == null)
            {
                _logger.LogWarning("Payment callback retrieval failed. Token: {Token}", callback.Token);
                return OperationResult<OrderWithDetailsDto>.Failure(
                    "Ödeme sonucu alınamadı.", 
                    ResultType.Error);
            }

            var paymentResult = retrieveResult.Data;

            _manager.ClearTracker();
            var order = await _manager.Order.GetOrderByNumberAsync(paymentResult.BasketId, true);
            if (order == null)
            {
                _logger.LogError("Order not found for payment callback. BasketId: {BasketId}", paymentResult.BasketId);
                return OperationResult<OrderWithDetailsDto>.Failure("Sipariş bulunamadı.", ResultType.NotFound);
            }

            if (paymentResult.PaymentStatus == "SUCCESS" && paymentResult.FraudStatus == 1)
            {
                string? bankName = null;
                // BIN sorgusu ile banka adını al
                if (!string.IsNullOrEmpty(paymentResult.BinNumber))
                {
                    try
                    {
                        // Sadece ilk 6 veya 8 hane yeterli
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

                order.MarkAsPaid(
                    paymentResult.PaymentId, 
                    "Iyzico",
                    cardType: paymentResult.CardType,
                    cardAssociation: paymentResult.CardAssociation,
                    cardFamily: paymentResult.CardFamily,
                    bankName: bankName, 
                    installmentCount: paymentResult.Installment,
                    lastFourDigits: paymentResult.LastFourDigits
                );

                // Save itemTransactions for refund capability
                if (paymentResult.ItemTransactions != null && paymentResult.ItemTransactions.Any())
                {
                    foreach (var item in paymentResult.ItemTransactions)
                    {
                        // Find matching order line by itemId
                        // ItemId format from Iyzico: "BI{ProductId}" or similar
                        var orderLine = order.Lines.FirstOrDefault(l =>
                            item.ItemId.Contains(l.ProductId.ToString()) ||
                            item.ItemId.Contains(l.OrderLineId.ToString()));

                        if (orderLine != null)
                        {
                            // Check if transaction already exists
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

                                _manager.OrderLinePaymentTransaction.CreateOrderLinePaymentTransaction(paymentTransaction);

                                _logger.LogInformation(
                                    "Payment transaction saved. OrderLineId: {OrderLineId}, PaymentTransactionId: {PaymentTransactionId}",
                                    orderLine.OrderLineId, item.PaymentTransactionId);
                            }
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Could not match itemTransaction to order line. ItemId: {ItemId}, OrderId: {OrderId}",
                                item.ItemId, order.OrderId);
                        }
                    }
                }

                // Create history entry
                var historyEntry = OrderHistory.CreateEvent(
                    orderId: order.OrderId,
                    eventType: OrderEventType.PaymentCompleted,
                    description: $"Ödeme alındı. İşlem ID: {paymentResult.PaymentId}, Kart: {paymentResult.CardAssociation} {paymentResult.LastFourDigits}",
                    userId: null,
                    isSystemEvent: true
                );
                _manager.OrderHistory.CreateOrderHistory(historyEntry);

                _logger.LogInformation(
                    "Payment successful. OrderId: {OrderId}, PaymentId: {PaymentId}, FraudStatus: {FraudStatus}",
                    order.OrderId, paymentResult.PaymentId, paymentResult.FraudStatus);
            }
            else
            {
                // Payment failed or fraud detected
                var failureReason = paymentResult.PaymentStatus != "SUCCESS" 
                    ? $"Ödeme başarısız: {paymentResult.ErrorMessage}" 
                    : $"Fraud kontrolü başarısız. FraudStatus: {paymentResult.FraudStatus}";

                order.MarkAsFailed(failureReason);

                // Restore product stock since payment failed
                foreach (var line in order.Lines)
                {
                    var product = await _manager.Product.GetOneProductAsync(line.ProductId, true);
                    if (product != null)
                    {
                        product.IncreaseStock(line.Quantity);
                        _logger.LogInformation("Stock restored for failed order. ProductId: {ProductId}, Quantity: +{Quantity}",
                            line.ProductId, line.Quantity);
                    }
                }

                // Create history entry for failed payment
                var historyEntry = OrderHistory.CreateEvent(
                    orderId: order.OrderId,
                    eventType: OrderEventType.PaymentFailed,
                    description: failureReason,
                    userId: null,
                    isSystemEvent: true
                );
                _manager.OrderHistory.CreateOrderHistory(historyEntry);

                _logger.LogWarning(
                    "Payment failed or fraud detected. OrderId: {OrderId}, Status: {Status}, FraudStatus: {FraudStatus}, OrderStatus: Failed",
                    order.OrderId, paymentResult.PaymentStatus, paymentResult.FraudStatus);
            }


            await _manager.SaveAsync();

            var orderDto = _mapper.Map<OrderWithDetailsDto>(order);
            return OperationResult<OrderWithDetailsDto>.Success(orderDto);
        }

        public async Task<OperationResult<OrderWithDetailsDto>> RefundOrderAsync(int orderId)
        {
            _manager.ClearTracker();
            var order = await _manager.Order.GetOrderWithDetailsAsync(orderId, true);
            if (order == null)
            {
                return OperationResult<OrderWithDetailsDto>.Failure("Sipariş bulunamadı.", ResultType.NotFound);
            }
            if (!order.CanBeRefunded())
            {
                return OperationResult<OrderWithDetailsDto>.Failure("Bu sipariş iade edilemez.", ResultType.ValidationError);
            }
            // Get payment transactions for this order
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
            // Refund each item transaction
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
                var refundResult = await _paymentProvider.RefundPaymentAsync(refundRequest);
                if (refundResult.IsSuccess && refundResult.Data != null)
                {
                    // Mark transaction as refunded
                    transaction.IsRefunded = true;
                    transaction.RefundTransactionId = refundResult.Data.PaymentTransactionId;
                    transaction.RefundedAt = DateTime.UtcNow;
                    _manager.OrderLinePaymentTransaction.UpdateOrderLinePaymentTransaction(transaction);
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
            // If any refund failed, return error
            if (refundErrors.Any())
            {
                return OperationResult<OrderWithDetailsDto>.Failure(
                    $"İade işlemi kısmen başarısız: {string.Join(", ", refundErrors)}",
                    ResultType.Error);
            }
            // All refunds successful - mark order as refunded
            order.MarkAsRefunded();
            // Create history entry
            var historyEntry = OrderHistory.CreateEvent(
                orderId: order.OrderId,
                eventType: OrderEventType.Refunded,
                description: "Sipariş iade edildi. İyzico üzerinden otomatik iade yapıldı.",
                userId: userId
            );
            _manager.OrderHistory.CreateOrderHistory(historyEntry);
            // Restore product stock
            foreach (var line in order.Lines)
            {
                var product = await _manager.Product.GetOneProductAsync(line.ProductId, true);
                if (product != null)
                {
                    product.IncreaseStock(line.Quantity);
                }
            }
            await _manager.SaveAsync();
            _logger.LogInformation(
                "Order refunded successfully. OrderId: {OrderId}, User: {UserId}",
                orderId, userId);
            var orderDto = _mapper.Map<OrderWithDetailsDto>(order);
            return OperationResult<OrderWithDetailsDto>.Success(orderDto, "Sipariş başarıyla iade edildi.");
        }



        public async Task<int> GetUserOrdersCountAsync(string userId)
        {
            return await _manager.Order.GetUserOrdersCountAsync(userId);
        }

        public async Task<decimal> GetUserTotalSpentAsync(string userId)
        {
            return await _manager.Order.GetUserTotalSpentAsync(userId);
        }
    }
}
