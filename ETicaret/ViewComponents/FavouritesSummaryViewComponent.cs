using Microsoft.AspNetCore.Mvc;

namespace ETicaret.ViewComponents
{
    public class FavouritesSummaryViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var favouritesCookie = Request.Cookies["FavouriteProducts"];

            if (string.IsNullOrWhiteSpace(favouritesCookie))
            {
                return View(0);
            }

            var favourites = favouritesCookie
                .Split('|', StringSplitOptions.RemoveEmptyEntries)
                .Select(id =>
                {
                    if (int.TryParse(id.Trim(), out int parsedId) && parsedId > 0)
                        return parsedId;
                    return (int?)null;
                })
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();

            return View(favourites.Count);
        }
    }
}
