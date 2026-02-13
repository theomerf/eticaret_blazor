using Application.DTOs;
using Application.Services.Interfaces;
using Domain.Entities;
using ETicaret.Extensions;
using ETicaret.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

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
            var version = await _cartService.GetVersionAsync(userId!);

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
                        ActualPrice = l.Price,
                        DiscountPrice = l.DiscountPrice,
                        Quantity = l.Quantity,
                        ProductVariantId = l.ProductVariantId,
                        SelectedColor = l.SelectedColor,
                        SelectedSize = l.SelectedSize,
                        VariantSpecifications = string.IsNullOrEmpty(l.SpecificationsJson) ? [] : JsonSerializer.Deserialize<List<ProductSpecificationDto>>(l.SpecificationsJson, (JsonSerializerOptions?)null)!
                    }).ToList()
                };

                return Ok(sessionCartDto);
            }

            var cart = await _cartService.GetByUserIdAsync(userId);
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
                request.ProductVariantId,
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

            var cart = await _cartService.GetByUserIdAsync(userId);
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
                var productDto = await _productService.GetByIdAsync(request.ProductId);

                if (productDto.Variants.FirstOrDefault(v => v.ProductVariantId == request.ProductVariantId) == null)
                {
                    return BadRequest(new { success = false, message = "Geçersiz ürün varyantı", type = "danger" });
                }

                var variantDto = await _productService.GetVariantByIdAsync(request.ProductVariantId, true);

                if (variantDto.Stock < request.Quantity)
                {
                    return BadRequest(new { success = false, message = $"Yetersiz stok. Mevcut: {variantDto.Stock}", type = "danger" });
                }

                var productEntity = new Product
                {
                    ProductId = productDto.ProductId,
                    ProductName = productDto.ProductName,
                };

                var variantEntity = new ProductVariant
                {
                    ProductVariantId = variantDto.ProductVariantId,
                    ProductId = productDto.ProductId,
                    Price = variantDto.Price,
                    DiscountPrice = variantDto.DiscountPrice,
                    Color = variantDto.Color,
                    Size = variantDto.Size,
                    Stock = variantDto.Stock,
                    Images = variantDto.Images?.Where(i => i.IsPrimary == true).Select(i => new ProductImage
                    {
                        ProductImageId = i.ProductImageId,
                        ProductVariantId = i.ProductVariantId,
                        ImageUrl = i.ImageUrl,
                        IsPrimary = i.IsPrimary,
                    }).ToList()
                };

                var result = _sessionCart.AddOrUpdateItem(productEntity, variantEntity, request.Quantity);

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
                        ActualPrice = l.Price,
                        DiscountPrice = l.DiscountPrice,
                        Quantity = l.Quantity,
                        ProductVariantId = l.ProductVariantId,
                        SelectedColor = l.SelectedColor,
                        SelectedSize = l.SelectedSize,
                        VariantSpecifications = string.IsNullOrEmpty(l.SpecificationsJson) ? [] : JsonSerializer.Deserialize<List<ProductSpecificationDto>>(l.SpecificationsJson, (JsonSerializerOptions?)null)!
                    }).ToList()
                };

                return Ok(new { success = true, message = result.Message, cart, type = "success" });
            }

            var serviceResult = await _cartService.AddOrUpdateItemAsync(userId, request.ProductId, request.ProductVariantId, request.Quantity);

            if (!serviceResult.IsSuccess)
            {
                return BadRequest(new { success = false, message = serviceResult.Message, type = "danger" });
            }

            var dbCart = await _cartService.GetByUserIdAsync(userId);

            SyncSessionCart(dbCart);

            return Ok(new { success = true, message = serviceResult.Message, cart = dbCart, type = "success" });
        }

        [HttpDelete("items/{productId:int}/variants/{variantId:int}")]
        public async Task<IActionResult> RemoveItem([FromRoute] int productId, [FromRoute] int variantId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                var result = _sessionCart.RemoveItem(productId, variantId);

                var cart = new CartDto
                {
                    UserId = "",
                    Lines = _sessionCart.Lines.Select(l => new CartLineDto
                    {
                        ProductId = l.ProductId,
                        ProductName = l.ProductName,
                        ImageUrl = l.ImageUrl,
                        ActualPrice = l.Price,
                        DiscountPrice = l.DiscountPrice,
                        Quantity = l.Quantity,
                        ProductVariantId = l.ProductVariantId,
                        SelectedColor = l.SelectedColor,
                        SelectedSize = l.SelectedSize,
                        VariantSpecifications = string.IsNullOrEmpty(l.SpecificationsJson) ? [] : JsonSerializer.Deserialize<List<ProductSpecificationDto>>(l.SpecificationsJson, (JsonSerializerOptions?)null)!
                    }).ToList()
                };

                return Ok(new { success = true, message = result.Message, cart, type = "success" });
            }

            var serviceResult = await _cartService.RemoveItemAsync(userId, productId, variantId);
            var dbCart = await _cartService.GetByUserIdAsync(userId);

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
                    cart = await _cartService.GetByUserIdAsync(null)
                });
            }


            var hasChanges = await _cartService.ValidateAsync(userId);
            var cart = await _cartService.GetByUserIdAsync(userId);

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
                    Price = line.ActualPrice,
                    DiscountPrice = line.DiscountPrice,
                    Quantity = line.Quantity,
                    ProductVariantId = line.ProductVariantId,
                    SelectedColor = line.SelectedColor,
                    SelectedSize = line.SelectedSize,
                    SpecificationsJson = System.Text.Json.JsonSerializer.Serialize(line.VariantSpecifications.Select(x => new Application.DTOs.ProductSpecificationDto { Key = x.Key, Value = x.Value })),
                    Cart = _sessionCart
                });
            }

            if (_sessionCart is SessionCart sc && sc.Session != null)
            {
                sc.Session.SetJson("cart", sc);
            }

             _logger.LogDebug("SessionCart senkronize edildi. Ürün sayısı: {Count}", _sessionCart.Lines.Count);
        }

        public record SetQuantityRequest(int Quantity, int ProductVariantId);
        public record AddItemRequest(int ProductId, int ProductVariantId, int Quantity);
    }
}
