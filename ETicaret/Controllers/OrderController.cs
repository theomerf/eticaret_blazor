using Application.DTOs;
using Application.Services.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Security.Claims;

namespace ETicaret.Controllers
{
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly INotificationService _notificationService;
        private readonly Cart _cart;

        public OrderController(IOrderService orderService, Cart cart, INotificationService notificationService)
        {
            _orderService = orderService;
            _cart = cart;
            _notificationService = notificationService;
        }

        [Authorize]
        public ViewResult Checkout()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout([FromForm] OrderDto order)
        {
            if (_cart.Lines.Count() == 0)
            {
                ModelState.AddModelError("", "Üzgünüm, sepetiniz boş.");
            }
            if (ModelState.IsValid) 
            {
                decimal? totalPrice = 0;
                foreach (var line in _cart.Lines)
                {
                    order.Lines.Add(new OrderLine
                    {
                        ProductId = line.ProductId,
                        Quantity = line.Quantity
                    });
                    totalPrice += line.DiscountPrice != null ? line.DiscountPrice : line.ActualPrice;
                }
                order.UserName = User.FindFirstValue(ClaimTypes.Name);
                await _orderService.SaveOrderAsync(order);
              /*  var cartDto = SessionCart.GetCartDto() */
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                /* await _manager.CartService.AddOrUpdateCartAsync(cartDto, userId); */
                _cart.Clear();

                await _notificationService.CreateNotificationAsync(new NotificationDtoForCreation
                {
                    NotificationType = NotificationType.Payment,
                    Title = "Ödemeniz tamamlandı",
                    Description = $"Siparişiniz için {totalPrice?.ToString("C2", new CultureInfo("tr-TR"))} ödemeniz alınmıştır.",
                    UserId = userId!
                });
                return RedirectToAction("Complete", new { orderId = order.OrderId });

            }
            else
            {
                return View(order);
            }
        }

        public IActionResult Complete(string orderId) 
        {
            return View((object)orderId);
        }

    }
}
