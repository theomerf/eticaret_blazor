using Domain.Entities;

namespace Application.Queries.RequestParameters
{
    public record NotificationRequestParametersAdmin : RequestParametersAdmin
    {
        public NotificationType? NotificationType { get; set; }
        public bool? IsSent { get; set; }
        public string? SortBy { get; set; }
    }
}
