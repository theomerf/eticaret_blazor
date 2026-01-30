using Application.DTOs;
using Application.Services.Interfaces;
using ETicaret.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ETicaret.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ICartService _cartService;
        private readonly IAddressService _addressService;
        private readonly ICouponService _couponService;
        private readonly ICampaignService _campaignService;

        public OrderController(
            IOrderService orderService,
            ICartService cartService,
            IAddressService addressService,
            ICouponService couponService,
            ICampaignService campaignService)
        {
            _orderService = orderService;
            _cartService = cartService;
            _addressService = addressService;
            _couponService = couponService;
            _campaignService = campaignService;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        /// <summary>
        /// GET: Display checkout page with cart, addresses, and campaigns
        /// </summary>
        public async Task<IActionResult> Checkout()
        {
            var userId = GetUserId();
            
            var cartDto = SessionCart.GetCartDto(HttpContext.Session);
            
            if (cartDto.Lines == null || !cartDto.Lines.Any())
            {
                TempData["error"] = "Sepetiniz boş.";
                return RedirectToAction("Index", "Cart");
            }

            var addresses = await _addressService.GetAllAddressesOfOneUserAsync(userId);
            var campaigns = await _campaignService.GetActiveCampaignsAsync();

            var model = new CheckoutViewModel
            {
                Cart = cartDto,
                Addresses = addresses,
                ActiveCampaigns = campaigns
            };

            return View(model);
        }

        /// <summary>
        /// POST: Process checkout form, create order, and redirect to payment
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout([FromForm] OrderDtoForCreation orderDto)
        {
            if (!ModelState.IsValid)
            {
                TempData["error"] = "Lütfen tüm alanları doldurun.";
                return RedirectToAction("Checkout");
            }

            var userId = GetUserId();
            
            var cartDto = SessionCart.GetCartDto(HttpContext.Session);
            if (cartDto.Lines == null || !cartDto.Lines.Any())
            {
                TempData["error"] = "Sepetiniz boş.";
                return RedirectToAction("Index", "Cart");
            }

            // Populate cart lines from session
            orderDto.CartLines = cartDto.Lines.Select(l => new CartLineDto
            {
                ProductId = l.ProductId,
                ProductName = l.ProductName,
                Quantity = l.Quantity,
                ActualPrice = l.ActualPrice,
                DiscountPrice = l.DiscountPrice,
                ImageUrl = l.ImageUrl
            }).ToList();

            // Create order
            var orderResult = await _orderService.CreateOrderAsync(orderDto, userId);

            if (!orderResult.IsSuccess)
            {
                TempData["error"] = orderResult.Message;
                return RedirectToAction("Checkout");
            }

            // Clear cart after successful order creation
            HttpContext.Session.Remove("cart");

            // Initiate payment
            var paymentResult = await _orderService.InitiatePaymentAsync(orderResult.Data, userId);

            if (!paymentResult.IsSuccess)
            {
                TempData["error"] = "Sipariş oluşturuldu ancak ödeme başlatılamadı: " + paymentResult.Message;
                return RedirectToAction("OrderDetails", new { orderId = orderResult.Data });
            }

            // Redirect to external payment page (Iyzico)
            if (!string.IsNullOrEmpty(paymentResult.Data?.PaymentUrl))
            {
                return Redirect(paymentResult.Data.PaymentUrl);
            }

            // Fallback if payment URL is not available
            TempData["error"] = "Ödeme sayfasına yönlendirilemedi.";
            return RedirectToAction("OrderDetails", new { orderId = orderResult.Data });
        }

        /// <summary>
        /// GET: Display order completion page after payment
        /// </summary>
        public async Task<IActionResult> Complete(string orderNumber, bool success = true, string? reason = null)
        {
            if (string.IsNullOrEmpty(orderNumber))
            {
                TempData["error"] = "Sipariş numarası bulunamadı.";
                return RedirectToAction("MyOrders");
            }

            var userId = GetUserId();
            var result = await _orderService.GetOrderByNumberAsync(orderNumber, userId);

            if (!result.IsSuccess)
            {
                TempData["error"] = "Sipariş bulunamadı.";
                return RedirectToAction("MyOrders");
            }

            ViewBag.PaymentSuccess = success;
            ViewBag.FailureReason = reason;

            return View(result.Data);
        }

        /// <summary>
        /// GET: Display user's order list
        /// </summary>
        public async Task<IActionResult> MyOrders()
        {
            var userId = GetUserId();
            var result = await _orderService.GetUserOrdersAsync(userId);

            if (!result.IsSuccess)
            {
                TempData["error"] = result.Message;
                return View(new List<OrderDto>());
            }

            return View(result.Data);
        }

        /// <summary>
        /// GET: Display single order details
        /// </summary>
        public async Task<IActionResult> OrderDetails(int orderId)
        {
            var userId = GetUserId();
            var result = await _orderService.GetOrderByIdAsync(orderId, userId);

            if (!result.IsSuccess)
            {
                TempData["error"] = "Sipariş bulunamadı.";
                return RedirectToAction("MyOrders");
            }

            return View(result.Data);
        }
    }
}
