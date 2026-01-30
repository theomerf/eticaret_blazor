using Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ETicaret.ViewComponents
{
    public class RatingStarsViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(string mode, double? averageRating = null, ProductDto? productDto = null, IEnumerable<UserReviewDto>? userReviewsDto = null)
        {
            if (mode == "ratings"){
                return View(userReviewsDto);
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
                return Content("Geçersiz mod");
            }

        }
    }
}
