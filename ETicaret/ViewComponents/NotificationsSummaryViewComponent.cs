using Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ETicaret.ViewComponents
{
    public class NotificationsSummaryViewComponent : ViewComponent
    {
        private readonly INotificationService _notificationService;

        public NotificationsSummaryViewComponent(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = (User as ClaimsPrincipal)?.FindFirstValue(ClaimTypes.NameIdentifier);
            var notifications = await _notificationService.GetAllNotificationsOfOneUserAsync(userId!);
            var unreadNotifications = notifications.Where(n => n.IsRead == false);

            return View(unreadNotifications.Count());
        }
    }
}
