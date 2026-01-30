using Application.Common.Models;
using Application.DTOs;
using Application.Services.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ETicaret.Controllers.Api
{
    [ApiController]
    [Route("api/orders")]
    public class OrderApiController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ICouponService _couponService;
        private readonly ICampaignService _campaignService;

        public OrderApiController(
            IOrderService orderService,
            ICouponService couponService,
            ICampaignService campaignService)
        {
            _orderService = orderService;
            _couponService = couponService;
            _campaignService = campaignService;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        /// <summary>
        /// AJAX: Get order details by ID
        /// </summary>
        [Authorize]
        [HttpGet("{orderId:int}")]
        public async Task<IActionResult> GetOrder(int orderId)
        {
            var userId = GetUserId();
            var result = await _orderService.GetOrderByIdAsync(orderId, userId);

            if (!result.IsSuccess)
            {
                return result.Type == ResultType.NotFound
                    ? NotFound(new
                    {
                        success = false,
                        message = result.Message,
                        type = result.Type
                    })
                    : StatusCode(500, new
                    {
                        success = false,
                        message = result.Message,
                        type = result.Type
                    });
            }

            return Ok(new
            {
                success = true,
                data = result.Data
            });
        }

        /// <summary>
        /// AJAX: Get order by order number
        /// </summary>
        [Authorize]
        [HttpGet("by-number/{orderNumber}")]
        public async Task<IActionResult> GetOrderByNumber(string orderNumber)
        {
            var userId = GetUserId();
            var result = await _orderService.GetOrderByNumberAsync(orderNumber, userId);

            if (!result.IsSuccess)
            {
                return result.Type == ResultType.NotFound
                    ? NotFound(new
                    {
                        success = false,
                        message = result.Message,
                        type = result.Type
                    })
                    : StatusCode(500, new
                    {
                        success = false,
                        message = result.Message,
                        type = result.Type
                    });
            }

            return Ok(new
            {
                success = true,
                data = result.Data
            });
        }

        /// <summary>
        /// AJAX: Get user's order list
        /// </summary>
        [Authorize]
        [HttpGet("my-orders")]
        public async Task<IActionResult> GetMyOrders()
        {
            var userId = GetUserId();
            var result = await _orderService.GetUserOrdersAsync(userId);

            if (!result.IsSuccess)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = result.Message,
                    type = result.Type
                });
            }

            return Ok(new
            {
                success = true,
                data = result.Data
            });
        }

        /// <summary>
        /// AJAX: Cancel order
        /// </summary>
        [Authorize]
        [HttpPost("{orderId:int}/cancel")]
        public async Task<IActionResult> CancelOrder(int orderId, [FromBody] CancelOrderRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "İptal sebebi belirtilmelidir.",
                    type = "danger"
                });
            }

            var userId = GetUserId();
            var result = await _orderService.CancelOrderAsync(orderId, request.Reason, userId);

            if (!result.IsSuccess)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = result.Message,
                    type = result.Type
                });
            }

            return Ok(new
            {
                success = true,
                message = result.Message,
                type = result.Type,
                data = result.Data
            });
        }

        /// <summary>
        /// Webhook: Payment callback from Iyzico (or other payment gateway)
        /// </summary>
        [HttpPost("payment-callback")]
        [AllowAnonymous] // Payment gateway needs to call this without authentication
        public async Task<IActionResult> PaymentCallback([FromBody] PaymentCallbackDto callback)
        {
            var result = await _orderService.HandlePaymentCallbackAsync(callback);

            if (!result.IsSuccess)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = result.Message,
                    type = result.Type
                });
            }

            // Redirect user to complete page
            var redirectUrl = callback.IsSuccess
                ? $"/Order/Complete?orderNumber={callback.OrderNumber}&success=true"
                : $"/Order/Complete?orderNumber={callback.OrderNumber}&success=false&reason={Uri.EscapeDataString(callback.FailureReason ?? "Unknown")}";

            return Ok(new
            {
                success = true,
                message = "Ödeme işlendi.",
                type = "success",
                redirectUrl = redirectUrl,
                data = result.Data
            });
        }

        /// <summary>
        /// AJAX: Get user order statistics
        /// </summary>
        [Authorize]
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            var userId = GetUserId();
            var ordersCount = await _orderService.GetUserOrdersCountAsync(userId);
            var totalSpent = await _orderService.GetUserTotalSpentAsync(userId);

            return Ok(new
            {
                success = true,
                data = new
                {
                    ordersCount,
                    totalSpent
                }
            });
        }

        /// <summary>
        /// AJAX: Validate coupon code
        /// </summary>
        [Authorize]
        [HttpPost("validate-coupon")]
        public async Task<IActionResult> ValidateCoupon([FromBody] ValidateCouponRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CouponCode))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Kupon kodu belirtilmelidir.",
                    type = "danger"
                });
            }

            var userId = GetUserId();
            var result = await _couponService.ValidateAndCalculateDiscountAsync(
                request.CouponCode,
                request.OrderAmount,
                userId);

            if (!result.IsSuccess)
            {
                return Ok(new
                {
                    success = false,
                    message = result.Message,
                    type = result.Type
                });
            }

            return Ok(new
            {
                success = true,
                message = "Kupon geçerli!",
                type = "success",
                discountAmount = result.Data
            });
        }

        /// <summary>
        /// AJAX: Get active campaigns
        /// </summary>
        [Authorize]
        [HttpGet("active-campaigns")]
        public async Task<IActionResult> GetActiveCampaigns()
        {
            var result = await _campaignService.GetActiveCampaignsAsync();

            return Ok(new
            {
                success = true,
                data = result
            });
        }
    }

    public record CancelOrderRequest
    {
        public string Reason { get; set; } = null!;
    }

    public record ValidateCouponRequest
    {
        public string CouponCode { get; set; } = null!;
        public decimal OrderAmount { get; set; }
    }
}
