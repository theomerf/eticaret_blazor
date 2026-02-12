using Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ETicaret.Controllers.Api
{
    [ApiController]
    [Route("api/products")]
    public class ProductsApiController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsApiController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet("variants/{variantId}")]
        public async Task<IActionResult> GetVariantAsync(int variantId)
        {
            try
            {
                var variant = await _productService.GetVariantByIdAsync(variantId);
                
                if (variant == null)
                {
                    return NotFound(new { success = false, message = "Varyant bulunamadı." });
                }

                var response = new
                {
                    success = true,
                    data = new
                    {
                        variantId = variant.ProductVariantId,
                        price = variant.Price,
                        discountPrice = variant.DiscountPrice,
                        discount = variant.Discount,
                        stock = variant.Stock,
                        color = variant.Color,
                        size = variant.Size,
                        images = variant.Images?.Select(img => new
                        {
                            imageUrl = img.ImageUrl,
                            isPrimary = img.IsPrimary,
                            altText = img.Caption
                        }).ToList(),
                        specifications = variant.VariantSpecifications
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Bir hata oluştu.", error = ex.Message });
            }
        }
    }
}
