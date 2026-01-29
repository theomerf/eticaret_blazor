using Application.Services.Interfaces;
using Domain.Entities;
using ETicaret.Models;
using Microsoft.AspNetCore.Mvc;

namespace ETicaret.ViewComponents.Admin
{
    public class ProductReviewsViewComponent : ViewComponent
    {
        private readonly IUserReviewService _userReviewService;

        public ProductReviewsViewComponent(IUserReviewService userReviewService)
        {
            _userReviewService = userReviewService;
        }

        public async Task<IViewComponentResult> InvokeAsync(int productId)
        {
            var reviews = await _userReviewService.GetAllUserReviewsOfOneProductAsync(productId);

            var model = new ProductReviewsViewModel
            {
                UserReviews = reviews,
                ProductId = productId
            };

            return View(model);
        }
    }
}
