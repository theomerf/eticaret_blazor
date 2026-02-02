using Application.Common.Models;
using Application.DTOs;
using Application.Services.Interfaces;
using Domain.Entities;
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
        private readonly IPaymentProvider _paymentProvider;
        private readonly ILogger<OrderController> _logger;

        public OrderController(
            IOrderService orderService,
            ICartService cartService,
            IAddressService addressService,
            ICouponService couponService,
            ICampaignService campaignService,
            IPaymentProvider paymentProvider,
            ILogger<OrderController> logger)
        {
            _orderService = orderService;
            _cartService = cartService;
            _addressService = addressService;
            _couponService = couponService;
            _campaignService = campaignService;
            _paymentProvider = paymentProvider;
            _logger = logger;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        private string GetUserEmail() => User.FindFirstValue(ClaimTypes.Name) ?? "";
        private string GetIdentityNumber() => User.FindFirstValue("identity_number") ?? "";

        public async Task<IActionResult> Checkout()
        {
            var userId = GetUserId();

            var cartDto = SessionCart.GetCartDto(HttpContext.Session);

            if (cartDto.Lines == null || !cartDto.Lines.Any())
            {
                TempData["toastContent"] = "Sepetiniz boş.";
                TempData["toastType"] = "error";

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout([FromForm] CheckoutFormModel formData)
        {
            var userId = GetUserId();

            _logger.LogInformation("Checkout form submitted. AddressId: {AddressId}, PaymentMethod: {PaymentMethod}, ShippingMethod: {ShippingMethod}",
                formData.AddressId, formData.PaymentMethod, formData.ShippingMethod);

            var cartDto = SessionCart.GetCartDto(HttpContext.Session);
            if (cartDto.Lines == null || !cartDto.Lines.Any())
            {
                TempData["toastContent"] = "Sepetiniz boş.";
                TempData["toastType"] = "error";

                return RedirectToAction("Index", "Cart");
            }

            if (formData.AddressId <= 0)
            {
                TempData["toastContent"] = "Lütfen bir teslimat adresi seçin.";
                TempData["toastType"] = "error";

                return RedirectToAction("Checkout");
            }

            var address = await _addressService.GetOneAddressAsync(formData.AddressId);

            var orderDto = new OrderDtoForCreation
            {
                FirstName = address.FirstName,
                LastName = address.LastName,
                Phone = address.Phone,
                City = address.City,
                District = address.District,
                AddressLine = address.AddressLine,
                PostalCode = address.PostalCode,
                ShippingMethod = formData.ShippingMethod,
                PaymentMethod = formData.PaymentMethod,
                GiftWrap = formData.GiftWrap,
                CustomerNotes = formData.CustomerNotes,
                CouponCode = formData.CouponCode,
                CartLines = cartDto.Lines.Select(l => new CartLineDto
                {
                    ProductId = l.ProductId,
                    ProductName = l.ProductName,
                    Quantity = l.Quantity,
                    ActualPrice = l.ActualPrice,
                    DiscountPrice = l.DiscountPrice,
                    ImageUrl = l.ImageUrl
                }).ToList()
            };

            var orderResult = await _orderService.CreateOrderAsync(orderDto);

            if (!orderResult.IsSuccess)
            {
                TempData["toastContent"] = orderResult.Message;
                TempData["toastType"] = "error";

                return RedirectToAction("Checkout");
            }

            var orderId = orderResult.Data;

            HttpContext.Session.Remove("cart");

            var order = await _orderService.GetOrderByIdAsync(orderId);

            if (formData.PaymentMethod == PaymentMethod.CreditCard)
            {
                // Kredi kartı ödemesi - Iyzico ödeme sayfasına yönlendir
                var callbackUrl = $"{Request.Scheme}://{Request.Host}/order/paymentCallback";

                var paymentRequest = new IyzicoCheckoutFormInitRequest
                {
                    OrderId = orderId,
                    OrderNumber = order.OrderNumber,
                    SubTotal = order.SubTotal,
                    TotalAmount = order.TotalAmount,
                    Currency = "TRY",
                    CustomerEmail = GetUserEmail(),
                    CustomerIdentityNumber = GetIdentityNumber(),
                    BillingAddress = address,
                    OrderLines = order.Lines,
                    CallbackUrl = callbackUrl
                };

                var paymentResult = await _paymentProvider.CreatePaymentAsync(paymentRequest);

                if (!paymentResult.IsSuccess || string.IsNullOrEmpty(paymentResult.Data?.PaymentPageUrl))
                {
                    _logger.LogError("Payment initiation failed. OrderId: {OrderId}, Error: {Error}",
                        orderId, paymentResult.Message);
                    TempData["toastContent"] = "Ödeme başlatılamadı: " + paymentResult.Message;
                    TempData["toastType"] = "error";

                    return Redirect("/account/orders");
                }

                HttpContext.Session.SetString("PaymentToken", paymentResult.Data.Token);
                HttpContext.Session.SetInt32("PaymentOrderId", orderId);

                return Redirect(paymentResult.Data.PaymentPageUrl);
            }
            else if (formData.PaymentMethod == PaymentMethod.BankTransfer)
            {
                TempData["toastContent"] = "Siparişiniz oluşturuldu. Havale/EFT bilgileri sipariş detaylarında görüntülenebilir.";
                TempData["toastType"] = "success";

                return RedirectToAction("Complete", new { orderId, success = true });
            }
            else
            {
                TempData["toastContent"] = "Siparişiniz başarıyla oluşturuldu. Kapıda ödeme yapılacaktır.";
                TempData["toastType"] = "success";

                return RedirectToAction("Complete", new { orderId, success = true });
            }
        }

        [AllowAnonymous]
        public async Task<IActionResult> PaymentCallback(string? token)
        {
            _logger.LogInformation("Payment callback received. Token: {Token}", token);

            var status = Request.Query["status"].FirstOrDefault();

            var checkoutToken = token ?? HttpContext.Session.GetString("PaymentToken");

            if (string.IsNullOrEmpty(checkoutToken))
            {
                _logger.LogWarning("Payment callback received with no token.");
                TempData["toastContent"] = "Ödeme bilgisi (token) bulunamadı.";
                TempData["toastType"] = "error";

                return Redirect("/account/orders");
            }

            try
            {
                var paymentCallback = new PaymentCallbackDto
                {
                    Token = checkoutToken,
                    Provider = "Iyzico"
                };

                var callbackResult = await _orderService.HandlePaymentCallbackAsync(paymentCallback);

                HttpContext.Session.Remove("PaymentToken");
                HttpContext.Session.Remove("PaymentOrderId");

                if (!callbackResult.IsSuccess || callbackResult.Data == null)
                {
                    _logger.LogWarning("Payment callback processing failed. Token: {Token}, Error: {Error}", 
                        checkoutToken, callbackResult.Message);
                    
                    TempData["toastContent"] = callbackResult.Message ?? "Ödeme işlemi tamamlanamadı.";
                    TempData["toastType"] = "error";

                    return Redirect("/account/orders");
                }

                var order = callbackResult.Data;
                var paymentSuccess = order.PaymentStatus == PaymentStatus.Completed;

                _logger.LogInformation(
                    "Payment callback processed. OrderId: {OrderId}, PaymentStatus: {PaymentStatus}",
                    order.OrderId, order.PaymentStatus);

                return RedirectToAction("Complete", new { orderId = order.OrderId, success = paymentSuccess });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment callback. Token: {Token}", checkoutToken);
                TempData["toastContent"] = "Ödeme işlemi sırasında beklenmedik bir hata oluştu.";
                TempData["toastType"] = "error";

                return Redirect("/account/orders");
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public Task<IActionResult> PaymentCallbackPost([FromForm] string? token)
        {
            return PaymentCallback(token);
        }

        public async Task<IActionResult> Complete(int orderId, bool success = true, string? reason = null)
        {
            var userId = GetUserId();
            var order = await _orderService.GetOrderByIdAsync(orderId);

            ViewBag.PaymentSuccess = success;
            ViewBag.FailureReason = reason;

            return View(order);
        }
    }

    public class CheckoutFormModel
    {
        public int AddressId { get; set; }
        public ShippingMethod ShippingMethod { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public bool GiftWrap { get; set; }
        public string? CustomerNotes { get; set; }
        public string? CouponCode { get; set; }
    }
}
