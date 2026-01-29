using Application.Services.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ETicaret.ViewComponents
{
    public class UserCountsViewComponent : ViewComponent
    {
        private readonly IAuthService _authService;
        private readonly UserManager<User> _userManager;

        public UserCountsViewComponent(IAuthService authService, UserManager<User> userManager)
        {
            _authService = authService;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync(string mode)
        {
            if (mode == "users")
            {
                var users = await _authService.GetAllUsersAsync();
                return Content(users.Count().ToString());
            }
            else if (mode == "admin")
            {
                var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                return Content(adminUsers.Count.ToString());
            }
            else if (mode == "active")
            {
                var users = await _authService.GetAllUsersAsync();
                var activeCount = users.Count();
                return Content(activeCount.ToString());
            }
            else if (mode == "passive")
            {
                var users = await _authService.GetAllUsersAsync();
                var passiveCount = users.Count();
                return Content(passiveCount.ToString());
            }

            return Content("Bilinmeyen mod");
        }
    }
}
