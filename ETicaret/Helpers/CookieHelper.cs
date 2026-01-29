namespace ETicaret.Helpers
{
    public static class CookieHelper
    {
        public static List<int> GetFavouriteProductIds(HttpRequest request)
        {
            var cookie = request.Cookies["FavouriteProducts"];
            if (string.IsNullOrWhiteSpace(cookie)) return new List<int>();

            return cookie
                .Split('|', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => int.TryParse(id, out var i) ? i : 0)
                .Where(id => id > 0)
                .ToList();
        }
    }

}
