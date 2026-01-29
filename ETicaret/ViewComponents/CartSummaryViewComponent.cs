using Application.Services.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ETicaret.ViewComponents
{
    public class CartSummaryViewComponent : ViewComponent
    {
        private readonly ICartService _cartService;  
        private readonly Cart _sessionCart;

        public CartSummaryViewComponent(ICartService cartService, Cart sessionCart)
        {
            _cartService = cartService;
            _sessionCart = sessionCart;
        }

        public async Task<IViewComponentResult> InvokeAsync() 
        {
            var userId = HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
            {
                var count = _sessionCart.Lines.Count;
                return View(count);
            }
            else
            {
                var count = await _cartService.GetCartLinesCountAsync(userId);
                return View(count);
            }
        }
    }
}
