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

        [Authorize]
        [HttpGet("{orderId:int}")]
        public async Task<IActionResult> GetOrder(int orderId)
        {
            var order = await _orderService.GetOrderByIdAsync(orderId);

            return Ok(new
            {
                data = order
            });
        }

        [Authorize]
        [HttpGet("by-number/{orderNumber}")]
        public async Task<IActionResult> GetOrderByNumber(string orderNumber)
        {
            var order = await _orderService.GetOrderByNumberAsync(orderNumber);

            return Ok(new
            {
                data = order
            });
        }

        [Authorize]
        [HttpGet("my-orders")]
        public async Task<IActionResult> GetMyOrders()
        {
            var userId = GetUserId();
            var order = await _orderService.GetUserOrdersAsync(userId);

            return Ok(new
            {
                data = order
            });
        }

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

            var result = await _orderService.CancelOrderAsync(orderId, request.Reason);

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

        [Authorize]
        [HttpPost("{orderId}/refund")]
        public async Task<IActionResult> RefundOrder(int orderId)
        {
            var result = await _orderService.RefundOrderAsync(orderId);

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
                message = "Sipariş iadesi başarıyla tamamlandı.",
                type = "success",
                data = result.Data
            });
        }

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

        [Authorize]
        [HttpGet("active-campaigns")]
        public async Task<IActionResult> GetActiveCampaigns()
        {
            var campaigns = await _campaignService.GetActiveCampaignsAsync();

            return Ok(new
            {
                success = true,
                data = campaigns
            });
        }

        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> Webhook([FromBody] IyzicoWebhook webhook)
        {
            System.Diagnostics.Debug.WriteLine($"Webhook received: PaymentId={webhook.PaymentId}, Status={webhook.Status}");

            if (webhook.IsSuccess && !string.IsNullOrEmpty(webhook.PaymentConversationId))
            {
                var callback = new PaymentCallbackDto
                {
                    OrderNumber = webhook.PaymentConversationId,
                    TransactionId = webhook.PaymentId.ToString(),
                    IsSuccess = true,
                    Provider = "Iyzico"
                };

                await _orderService.HandlePaymentCallbackAsync(callback);
            }

            return Ok(new
            {
                success = true,
                message = "Webhook processed."
            });
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
}
