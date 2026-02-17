using Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ETicaret.ViewComponents
{
    public class UserSummaryViewComponent : ViewComponent
    {
        private readonly IUserService _userService;

        public UserSummaryViewComponent(IUserService userService)
        {
            _userService = userService;
        }

        public async Task<int> InvokeAsync()
        {
            var users = await _userService.GetUsersCountAsync();

            return users;
        }
    }
}
