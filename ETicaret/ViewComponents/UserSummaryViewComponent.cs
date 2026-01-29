using Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ETicaret.ViewComponents
{
    public class UserSummaryViewComponent : ViewComponent
    {
        private readonly IAuthService _authService;

        public UserSummaryViewComponent(IAuthService authService)
        {
            _authService = authService;
        }

        public async Task<int> InvokeAsync()
        {
            var users = await _authService.GetUsersCountAsync();

            return users;
        }
    }
}
