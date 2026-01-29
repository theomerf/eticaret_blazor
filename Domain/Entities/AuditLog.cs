namespace Domain.Entities
{
    public class AuditLog
    {
        public long AuditLogId { get; set; }
        public string UserId { get; set; } = null!;
        public string? UserName { get; set; }
        public string Action { get; set; } = null!;
        public string EntityName { get; set; } = null!;
        public string? EntityId { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        public string IpAddress { get; set; } = null!;
        public string? UserAgent { get; set; }  
    }
}
