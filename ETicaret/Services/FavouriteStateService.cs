using Application.Services.Interfaces;
using ETicaret.Helpers;
using Microsoft.JSInterop;

namespace ETicaret.Services
{
    public interface IFavouriteStateService
    {
        List<int> FavouriteIds { get; }
        event Action? OnChange;
        void LoadFavourites();
        Task AddToFavouritesAsync(int productId);
        Task RemoveFromFavouritesAsync(int productId);
        bool IsFavourite(int productId);
    }

    public class FavouriteStateService : IFavouriteStateService
    {
        private readonly IUserService _userService;
        private readonly IJSRuntime _jsRuntime;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public List<int> FavouriteIds { get; private set; } = new();

        public event Action? OnChange;

        public FavouriteStateService(
            IUserService userService,
            IJSRuntime jsRuntime,
            IHttpContextAccessor httpContextAccessor)
        {
            _userService = userService;
            _jsRuntime = jsRuntime;
            _httpContextAccessor = httpContextAccessor;
        }

        public void LoadFavourites()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var context = _httpContextAccessor.HttpContext;
                if (context != null)
                {
                    FavouriteIds = CookieHelper.GetFavouriteProductIds(context.Request);
                }
            }
            OnChange?.Invoke();
        }

        public async Task AddToFavouritesAsync(int productId)
        {
            var result = await _userService.AddToFavouritesAsync(productId);
            if (result.IsSuccess && result.Data != null)
            {
                FavouriteIds = result.Data.FavouriteProductVariantsId.ToList();
                await UpdateCookieAsync();
                OnChange?.Invoke();
            }
        }

        public async Task RemoveFromFavouritesAsync(int productId)
        {
            var result = await _userService.RemoveFromFavouritesAsync(productId);
            if (result.IsSuccess && result.Data != null)
            {
                FavouriteIds = result.Data.FavouriteProductVariantsId.ToList();
                await UpdateCookieAsync();
                OnChange?.Invoke();
            }
        }

        public bool IsFavourite(int productId) => FavouriteIds.Contains(productId);

        private async Task UpdateCookieAsync()
        {
            var cookieValue = FavouriteIds.Any() ? string.Join("|", FavouriteIds) : string.Empty;
            await _jsRuntime.InvokeVoidAsync("setCookie", "FavouriteProducts", cookieValue, 365);
        }
    }
}
