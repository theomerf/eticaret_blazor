using Application.DTOs;
using Application.Services.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETicaret.Controllers.Api
{
    [ApiController]
    [Route("api/products")]
    public class ProductsApiController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly IUserReviewService _userReviewService;

        public ProductsApiController(IProductService productService, IUserReviewService userReviewService)
        {
            _productService = productService;
            _userReviewService = userReviewService;
        }

        [HttpGet("variants/{variantId}")]
        public async Task<IActionResult> GetVariant(int variantId)
        {
            try
            {
                var variant = await _productService.GetVariantByIdAsync(variantId, true);
                
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

        [HttpPost("reviews/vote")]
        [Authorize]
        public async Task<IActionResult> SetVote([FromBody] UserReviewVoteDtoForCreation voteDto)
        {
            var result = await _userReviewService.SetVoteAsync(voteDto.UserReviewId, (voteDto.IsHelpful ? VoteType.Helpful : VoteType.NotHelpful));

            if (!result.IsSuccess)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = result.Message,
                    type = result.Type,
                });
            }

            return Ok(new
            {
                success = true,
                message = result.Message,
                type = result.Type,
                data = new
                {
                    current = result.Data.Item1,
                    helpfulCount = result.Data.Item2,
                    notHelpfulCount = result.Data.Item3
                }
            });
        }
    }
}
