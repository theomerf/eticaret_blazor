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
        private readonly IPaymentService _paymentService;
        private readonly ICouponService _couponService;
        private readonly ICampaignService _campaignService;
        private readonly ResiliencePipeline _retryPipeline;

        public OrderManager(
            IRepositoryManager manager,
            IMapper mapper,
            ILogger<OrderManager> logger,
            IHttpContextAccessor httpContextAccessor,
            ISecurityLogService securityLogService,
            IPaymentService paymentService,
            ICouponService couponService,
            ICampaignService campaignService)
        {
            _manager = manager;
            _mapper = mapper;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _securityLogService = securityLogService;
            _paymentService = paymentService;
            _couponService = couponService;
            _campaignService = campaignService;

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

        public async Task<OperationResult<int>> CreateOrderAsync(OrderDtoForCreation orderDto, string userId)
        {
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

                    // Create order lines from cart
                    foreach (var cartLine in orderDto.CartLines)
                    {
                        // Verify product exists and has stock
                        var product = await _manager.Product.GetOneProductAsync(cartLine.ProductId, false);
                        if (product == null)
                        {
                            return OperationResult<int>.Failure($"Ürün bulunamadı: {cartLine.ProductName}", ResultType.NotFound);
                        }

                        if (product.Stock < cartLine.Quantity)
                        {
                            return OperationResult<int>.Failure($"Yetersiz stok: {product.ProductName}. Mevcut: {product.Stock}", ResultType.ValidationError);
                        }

                        var orderLine = new OrderLine
                        {
                            ProductId = cartLine.ProductId,
                            ProductName = cartLine.ProductName,
                            Quantity = cartLine.Quantity,
                            ActualPrice = product.ActualPrice,
                            DiscountPrice = product.DiscountPrice,
                            ImageUrl = cartLine.ImageUrl
                        };

                        orderLine.ValidateForCreation();
                        order.Lines.Add(orderLine);

                        // Decrease product stock
                        product.DecreaseStock(cartLine.Quantity);
                    }

                    // Calculate subtotal
                    order.SubTotal = order.Lines.Sum(l => (l.DiscountPrice ?? l.ActualPrice) * l.Quantity);

                    // Track remaining amount for discount calculations
                    decimal remainingAmount = order.SubTotal;

                    // Apply coupon first if provided
                    if (!string.IsNullOrWhiteSpace(orderDto.CouponCode))
                    {
                        var couponResult = await _couponService.ValidateCouponForOrderAsync(orderDto.CouponCode, order.SubTotal, userId);
                        if (couponResult.IsSuccess && couponResult.Data != null)
                        {
                            var coupon = couponResult.Data;
                            order.CouponCode = coupon.Code;
                            order.CouponDiscountAmount = coupon.CalculateDiscount(order.SubTotal);
                            
                            // Reduce remaining amount by coupon discount
                            remainingAmount -= order.CouponDiscountAmount.Value;
                            
                            _logger.LogInformation(
                                "Coupon applied. Code: {CouponCode}, Discount: {Discount}, Remaining: {Remaining}",
                                coupon.Code, order.CouponDiscountAmount, remainingAmount);
                        }
                    }

                    // Apply campaigns with priority and stackable logic
                    var applicableCampaigns = await _campaignService.GetApplicableCampaignsAsync(order.SubTotal);
                    
                    // Sort campaigns by priority (higher priority first)
                    var sortedCampaigns = applicableCampaigns.OrderByDescending(c => c.Priority).ToList();
                    
                    decimal campaignDiscountTotal = 0;
                    bool nonStackableApplied = false;

                    foreach (var campaign in sortedCampaigns)
                    {
                        // Skip if a non-stackable campaign was already applied
                        if (nonStackableApplied)
                            break;

                        // Calculate discount on remaining amount (after previous discounts)
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
                            
                            // Reduce remaining amount for next campaign
                            remainingAmount -= discount;

                            _logger.LogInformation(
                                "Campaign applied. Name: {CampaignName}, Priority: {Priority}, Discount: {Discount}, Remaining: {Remaining}, Stackable: {Stackable}",
                                campaign.Name, campaign.Priority, discount, remainingAmount, campaign.IsStackable);

                            // If campaign is not stackable, stop applying more campaigns
                            if (!campaign.IsStackable)
                            {
                                nonStackableApplied = true;
                            }
                        }
                    }

                    order.CampaignDiscountTotal = campaignDiscountTotal;
                    order.TotalDiscountAmount = (order.CouponDiscountAmount ?? 0) + (order.CampaignDiscountTotal ?? 0);

                    // Ensure total discount doesn't exceed subtotal
                    if (order.TotalDiscountAmount > order.SubTotal)
                    {
                        _logger.LogWarning(
                            "Total discount ({TotalDiscount}) exceeds subtotal ({SubTotal}). Capping discount.",
                            order.TotalDiscountAmount, order.SubTotal);
                        order.TotalDiscountAmount = order.SubTotal;
                    }

                    // Calculate shipping cost
                    decimal baseShippingCost = order.ShippingMethod switch
                    {
                        ShippingMethod.Standard => 29.99m,
                        ShippingMethod.Express => 49.99m,
                        ShippingMethod.HandlingOnly => 0m,
                        _ => 0
                    };

                    // Apply free shipping threshold (e.g., free shipping for orders over 500 TL)
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

                    // Calculate total
                    order.CalculateTotals();

                    // Domain validation
                    order.ValidateForCreation();

                    _manager.Order.CreateOrder(order);

                    // Create OrderHistory entry
                    var currentUserId = GetCurrentUserId();
                    var historyEntry = OrderHistory.CreateEvent(
                        orderId: 0, // Will be set after save
                        eventType: OrderEventType.OrderCreated,
                        description: "Sipariş oluşturuldu",
                        userId: currentUserId
                    );
                    order.History.Add(historyEntry);

                    await _manager.SaveAsync();

                    // Record coupon usage if coupon was applied
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

        public async Task<OperationResult<OrderWithDetailsDto>> GetOrderByIdAsync(int orderId, string userId)
        {
            var currentUserId = GetCurrentUserId();
            
            var order = await _manager.Order.GetOrderWithDetailsAsync(orderId, false);
            if (order == null)
            {
                return OperationResult<OrderWithDetailsDto>.Failure("Sipariş bulunamadı.", ResultType.NotFound);
            }

            // Validate user access
            ValidateUserAccess(order.UserId, currentUserId);

            var orderDto = _mapper.Map<OrderWithDetailsDto>(order);
            return OperationResult<OrderWithDetailsDto>.Success(orderDto);
        }

        public async Task<OperationResult<OrderWithDetailsDto>> GetOrderByNumberAsync(string orderNumber, string userId)
        {
            var currentUserId = GetCurrentUserId();
            
            var order = await _manager.Order.GetOrderByNumberAsync(orderNumber, false);
            if (order == null)
            {
                return OperationResult<OrderWithDetailsDto>.Failure("Sipariş bulunamadı.", ResultType.NotFound);
            }

            // Validate user access
            ValidateUserAccess(order.UserId, currentUserId);

            var orderDto = _mapper.Map<OrderWithDetailsDto>(order);
            return OperationResult<OrderWithDetailsDto>.Success(orderDto);
        }

        public async Task<OperationResult<IEnumerable<OrderDto>>> GetUserOrdersAsync(string userId)
        {
            var currentUserId = GetCurrentUserId();
            ValidateUserAccess(userId, currentUserId);

            var orders = await _manager.Order.GetUserOrdersAsync(userId, false);
            var ordersDto = _mapper.Map<IEnumerable<OrderDto>>(orders);
            return OperationResult<IEnumerable<OrderDto>>.Success(ordersDto);
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

            // Update order status if provided
            if (orderDto.OrderStatus.HasValue && order.OrderStatus != orderDto.OrderStatus.Value)
            {
                order.OrderStatus = orderDto.OrderStatus.Value;
                hasChanges = true;

                // Create history entry based on new status
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

            // Update payment status if provided
            if (orderDto.PaymentStatus.HasValue && order.PaymentStatus != orderDto.PaymentStatus.Value)
            {
                order.PaymentStatus = orderDto.PaymentStatus.Value;
                hasChanges = true;
            }

            // Update shipping info
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

            // Update admin notes
            if (!string.IsNullOrWhiteSpace(orderDto.AdminNotes))
            {
                order.AdminNotes = orderDto.AdminNotes;
                hasChanges = true;
            }

            // Update payment info
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

            var orderDto2 = _mapper.Map<OrderWithDetailsDto>(order);
            return OperationResult<OrderWithDetailsDto>.Success(orderDto2, "Sipariş güncellendi.");
        }

        public async Task<OperationResult<OrderWithDetailsDto>> CancelOrderAsync(int orderId, string reason, string userId)
        {
            _manager.ClearTracker();
            var order = await _manager.Order.GetOrderByIdAsync(orderId, true);
            if (order == null)
            {
                return OperationResult<OrderWithDetailsDto>.Failure("Sipariş bulunamadı.", ResultType.NotFound);
            }

            var currentUserId = GetCurrentUserId();
            ValidateUserAccess(order.UserId, currentUserId);

            if (!order.CanBeCancelled())
            {
                return OperationResult<OrderWithDetailsDto>.Failure("Bu sipariş iptal edilemez.", ResultType.ValidationError);
            }

            order.Cancel(reason);

            // Create history entry
            var historyEntry = OrderHistory.CreateEvent(
                orderId: order.OrderId,
                eventType: OrderEventType.Cancelled,
                description: $"Sipariş iptal edildi. Sebep: {reason}",
                userId: currentUserId
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
                "Order cancelled. OrderId: {OrderId}, Reason: {Reason}, User: {UserId}",
                orderId, reason, currentUserId);

            var orderDto = _mapper.Map<OrderWithDetailsDto>(order);
            return OperationResult<OrderWithDetailsDto>.Success(orderDto, "Sipariş iptal edildi.");
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

            // Create history entry
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

            // Create history entry
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

        public async Task<OperationResult<PaymentResponse>> InitiatePaymentAsync(int orderId, string userId)
        {
            var order = await _manager.Order.GetOrderByIdAsync(orderId, false);
            if (order == null)
            {
                return OperationResult<PaymentResponse>.Failure("Sipariş bulunamadı.", ResultType.NotFound);
            }

            var currentUserId = GetCurrentUserId();
            ValidateUserAccess(order.UserId, currentUserId);

            if (order.PaymentStatus != PaymentStatus.Pending)
            {
                return OperationResult<PaymentResponse>.Failure("Bu sipariş için ödeme zaten işlenmiş.", ResultType.ValidationError);
            }

            var paymentRequest = new PaymentRequest
            {
                OrderNumber = order.OrderNumber,
                Amount = order.TotalAmount,
                Currency = order.Currency,
                CustomerEmail = $"{order.UserId}@example.com", // In production, get from user profile
                CustomerName = $"{order.FirstName} {order.LastName}",
                CallbackUrl = $"https://yourdomain.com/api/orders/payment-callback"
            };

            var paymentResult = await _paymentService.InitiatePaymentAsync(paymentRequest);
            
            if (paymentResult.IsSuccess && paymentResult.Data != null)
            {
                // Update order with payment provider info
                _manager.ClearTracker();
                var orderToUpdate = await _manager.Order.GetOrderByIdAsync(orderId, true);
                if (orderToUpdate != null)
                {
                    orderToUpdate.PaymentProvider = "MockPaymentGateway";
                    orderToUpdate.PaymentTransactionId = paymentResult.Data.TransactionId;
                    await _manager.SaveAsync();
                }

                _logger.LogInformation(
                    "Payment initiated. OrderId: {OrderId}, TransactionId: {TransactionId}",
                    orderId, paymentResult.Data.TransactionId);
            }

            return paymentResult;
        }

        public async Task<OperationResult<OrderWithDetailsDto>> HandlePaymentCallbackAsync(PaymentCallbackDto callback)
        {
            // Validate callback
            var validationResult = await _paymentService.ValidateCallbackAsync(callback);
            if (!validationResult.IsSuccess)
            {
                return OperationResult<OrderWithDetailsDto>.Failure("Ödeme callback doğrulanamadı.", ResultType.ValidationError);
            }

            _manager.ClearTracker();
            var order = await _manager.Order.GetOrderByNumberAsync(callback.OrderNumber, true);
            if (order == null)
            {
                return OperationResult<OrderWithDetailsDto>.Failure("Sipariş bulunamadı.", ResultType.NotFound);
            }

            if (callback.IsSuccess)
            {
                order.MarkAsPaid(callback.TransactionId, callback.Provider ?? "Unknown");

                // Create history entry
                var historyEntry = OrderHistory.CreateEvent(
                    orderId: order.OrderId,
                    eventType: OrderEventType.PaymentCompleted,
                    description: $"Ödeme alındı. İşlem ID: {callback.TransactionId}",
                    userId: "System"
                );
                _manager.OrderHistory.CreateOrderHistory(historyEntry);

                _logger.LogInformation(
                    "Payment successful. OrderId: {OrderId}, TransactionId: {TransactionId}",
                    order.OrderId, callback.TransactionId);
            }
            else
            {
                // Create history entry for failed payment
                var historyEntry = OrderHistory.CreateEvent(
                    orderId: order.OrderId,
                    eventType: OrderEventType.PaymentFailed,
                    description: $"Ödeme başarısız. Sebep: {callback.FailureReason}",
                    userId: "System"
                );
                _manager.OrderHistory.CreateOrderHistory(historyEntry);

                _logger.LogWarning(
                    "Payment failed. OrderId: {OrderId}, Reason: {Reason}",
                    order.OrderId, callback.FailureReason);
            }

            await _manager.SaveAsync();

            var orderDto = _mapper.Map<OrderWithDetailsDto>(order);
            return OperationResult<OrderWithDetailsDto>.Success(orderDto);
        }

        public async Task<OperationResult<OrderWithDetailsDto>> RefundOrderAsync(int orderId)
        {
            _manager.ClearTracker();
            var order = await _manager.Order.GetOrderByIdAsync(orderId, true);
            if (order == null)
            {
                return OperationResult<OrderWithDetailsDto>.Failure("Sipariş bulunamadı.", ResultType.NotFound);
            }

            if (!order.CanBeRefunded())
            {
                return OperationResult<OrderWithDetailsDto>.Failure("Bu sipariş iade edilemez.", ResultType.ValidationError);
            }

            // Initiate refund with payment provider
            if (!string.IsNullOrWhiteSpace(order.PaymentTransactionId))
            {
                var refundResult = await _paymentService.RefundPaymentAsync(order.PaymentTransactionId, order.TotalAmount);
                if (!refundResult.IsSuccess)
                {
                    return OperationResult<OrderWithDetailsDto>.Failure("İade işlemi başarısız oldu.", ResultType.Error);
                }
            }

            var userId = GetCurrentUserId();
            order.MarkAsRefunded();

            // Create history entry
            var historyEntry = OrderHistory.CreateEvent(
                orderId: order.OrderId,
                eventType: OrderEventType.Refunded,
                description: "Sipariş iade edildi",
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
                "Order refunded. OrderId: {OrderId}, User: {UserId}",
                orderId, userId);

            var orderDto = _mapper.Map<OrderWithDetailsDto>(order);
            return OperationResult<OrderWithDetailsDto>.Success(orderDto, "Sipariş iade edildi.");
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
