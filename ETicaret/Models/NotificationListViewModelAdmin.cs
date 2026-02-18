using Application.DTOs;
using Application.Queries.RequestParameters;

namespace ETicaret.Models
{
    public class NotificationListViewModelAdmin
    {
        public IEnumerable<NotificationAdminGroupDto> Notifications { get; set; } = [];
        public Pagination Pagination { get; set; } = new();
        public int TotalCount { get; set; }
        public NotificationRequestParametersAdmin FilterParams { get; set; } = new();
    }
}
