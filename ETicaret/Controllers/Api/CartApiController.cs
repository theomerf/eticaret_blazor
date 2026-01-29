using Application.DTOs;
using Application.Services.Interfaces;
using Domain.Entities;
using ETicaret.Extensions;
using ETicaret.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ETicaret.Controllers.Api
{
    [ApiController]
    [Route("api/cart")]
    [IgnoreAntiforgeryToken]
    public class CartApiController : ControllerBase
    {
        private readonly Cart _sessionCart;
        private readonly ICartService _cartService;
        private readonly IProductService _productService;
        private readonly ILogger<CartApiController> _logger;

        public CartApiController(Cart sessionCart, ICartService cartService, ILogger<CartApiController> logger, IProductService productService)
        {
            _sessionCart = sessionCart;
            _cartService = cartService;
            _logger = logger;
            _productService = productService;
        }

        [HttpGet("count")]
        public IActionResult GetCartItemCount()
        {
            var count = _sessionCart.Lines.Sum(l => l.Quantity);

            return Ok(new { count });
        }

        [HttpGet("version")]
        public async Task<IActionResult> GetCartVersion()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var version = await _cartService.GetCartVersionAsync(userId!);

            return Ok(new { version });
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                var sessionCartDto = new CartDto
                {
                    UserId = "",
                    Lines = _sessionCart.Lines.Select(l => new CartLineDto
                    {
                        ProductId = l.ProductId,
                        ProductName = l.ProductName,
                        ImageUrl = l.ImageUrl,
                        ActualPrice = l.ActualPrice,
                        DiscountPrice = l.DiscountPrice,
                        Quantity = l.Quantity
                    }).ToList()
                };

                return Ok(sessionCartDto);
            }

            var cart = await _cartService.GetCartAsync(userId);
            HttpContext.Session.SetJson("cart", cart);

            return Ok(cart);
        }

        [HttpPut("items/{productId:int}/quantity")]
        public async Task<IActionResult> SetQuantity([FromRoute] int productId, [FromBody] SetQuantityRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (request.Quantity < 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Miktar negatif olamaz",
                    type = "danger"
                });
            }

            var result = await _cartService.SetQuantityAsync(
                userId,
                productId,
                request.Quantity
            );

            if (!result.IsSuccess)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message,
                    type = "danger"
                });
            }

            var cart = await _cartService.GetCartAsync(userId);
            return Ok(new
            {
                success = true,
                message = result.Message,
                cart,
                type = "success"
            });
        }

        [HttpPost("items")]
        public async Task<IActionResult> AddOrUpdateItem([FromBody] AddItemRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                var product = await _productService.GetOneProductAsync(request.ProductId);

                if (product == null)
                {
                    return BadRequest(new { success = false, message = "Ürün bulunamadı", type = "danger" });
                }

                if (product.Stock < request.Quantity)
                {
                    return BadRequest(new { success = false, message = $"Yetersiz stok. Mevcut: {product.Stock}", type = "danger" });
                }

                var productEntity = new Product
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    Images = product.Images,
                    ActualPrice = product.ActualPrice,
                    DiscountPrice = product.DiscountPrice,
                };

                var result = _sessionCart.AddOrUpdateItem(productEntity, request.Quantity);

                if (!result.IsSuccess)
                {
                    return BadRequest(new { success = false, message = result.Message, type = "danger" });
                }

                var cart = new CartDto
                {
                    UserId = "",
                    Lines = _sessionCart.Lines.Select(l => new CartLineDto
                    {
                        ProductId = l.ProductId,
                        ProductName = l.ProductName,
                        ImageUrl = l.ImageUrl,
                        ActualPrice = l.ActualPrice,
                        DiscountPrice = l.DiscountPrice,
                        Quantity = l.Quantity
                    }).ToList()
                };

                return Ok(new { success = true, message = result.Message, cart, type = "success" });
            }

            var serviceResult = await _cartService.AddOrUpdateItemAsync(userId, request.ProductId, request.Quantity);

            if (!serviceResult.IsSuccess)
            {
                return BadRequest(new { success = false, message = serviceResult.Message, type = "danger" });
            }

            var dbCart = await _cartService.GetCartAsync(userId);

            SyncSessionCart(dbCart);

            return Ok(new { success = true, message = serviceResult.Message, cart = dbCart, type = "success" });
        }

        [HttpDelete("items/{productId:int}")]
        public async Task<IActionResult> RemoveItem([FromRoute] int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                var result = _sessionCart.RemoveItem(productId);

                var cart = new CartDto
                {
                    UserId = "",
                    Lines = _sessionCart.Lines.Select(l => new CartLineDto
                    {
                        ProductId = l.ProductId,
                        ProductName = l.ProductName,
                        ImageUrl = l.ImageUrl,
                        ActualPrice = l.ActualPrice,
                        DiscountPrice = l.DiscountPrice,
                        Quantity = l.Quantity
                    }).ToList()
                };

                return Ok(new { success = true, message = result.Message, cart, type = "success" });
            }

            var serviceResult = await _cartService.RemoveItemAsync(userId, productId);
            var dbCart = await _cartService.GetCartAsync(userId);

            SyncSessionCart(dbCart);

            return Ok(new { success = true, message = serviceResult.Message, cart = dbCart, type = "success" });
        }

        [HttpPost("validate")]
        public async Task<IActionResult> ValidateCart()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Ok(new
                {
                    hasChanges = false,
                    cart = await _cartService.GetCartAsync(null)
                });
            }


            var hasChanges = await _cartService.ValidateCartAsync(userId);
            var cart = await _cartService.GetCartAsync(userId);

            return Ok(new
            {
                hasChanges,
                cart
            });
        }

        private void SyncSessionCart(CartDto dbCart)
        {
            _sessionCart.Lines.Clear();
            _sessionCart.UserId = dbCart.UserId;

            foreach (var line in dbCart.Lines)
            {
                _sessionCart.Lines.Add(new CartLine
                {
                    ProductId = line.ProductId,
                    ProductName = line.ProductName,
                    ImageUrl = line.ImageUrl,
                    ActualPrice = line.ActualPrice,
                    DiscountPrice = line.DiscountPrice,
                    Quantity = line.Quantity,
                    Cart = _sessionCart
                });
            }

            if (_sessionCart is SessionCart sc && sc.Session != null)
            {
                sc.Session.SetJson("cart", sc);
            }

             _logger.LogDebug("SessionCart senkronize edildi. Ürün sayısı: {Count}", _sessionCart.Lines.Count);
        }

        public record SetQuantityRequest(int Quantity);
        public record AddItemRequest(int ProductId, int Quantity);
    }
}
