using Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ETicaret.ViewComponents
{
    public class RatingStarsViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(string mode, int? ratingsCount = null, double? averageRating = null, ProductDto? productDto = null, IEnumerable<UserReviewDto>? userReviewsDto = null)
        {
           /* IEnumerable<int> ratings = await _manager.UserReviewService.GetAllRatingsForProductAsync(productId, false);*/
            if (mode == "ratings"){
                return View(userReviewsDto);
            }
            else if(mode == "average")
            {
                return View("average", averageRating);
            }
            else if (mode == "count")
            {
                return View("count", ratingsCount);
            }
            else if (mode == "stars")
            {
                return View("stars", averageRating);
            }
            else if (mode == "starsForCard")
            {
                return View("starsForCard", productDto);
            }
            else
            {
                return Content("Invalid mode");
            }

        }
    }
}
