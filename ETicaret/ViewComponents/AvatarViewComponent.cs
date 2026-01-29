using Application.DTOs;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace ETicaret.ViewComponents
{
    public class AvatarViewComponent : ViewComponent
    {
        private readonly IAuthService _authService;
        private readonly IMemoryCache _cache;

        public AvatarViewComponent(IAuthService authService, IMemoryCache cache)
        {
            _authService = authService;
            _cache = cache;
        }

        public async Task<string> InvokeAsync()
        {
            string cacheKey = "user";

            if (_cache.TryGetValue(cacheKey, out UserDto? cachedUser))
            {
                return cachedUser?.AvatarUrl!;
            }

            var user = await _authService.GetOneUserAsync((User as ClaimsPrincipal)!.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var cacheOptions = new MemoryCacheEntryOptions()
                 .SetSlidingExpiration(TimeSpan.FromMinutes(30));

            _cache.Set(cacheKey, user, cacheOptions);

            if (user != null && user.AvatarUrl != null)
            {
                return user.AvatarUrl;
            }
            return "avatars/default.png";
        }
    }
}
