namespace Domain.Entities
{
    public class SecurityLog
    {
        public long SecurityLogId { get; set; }
        public string EventType { get; set; } = null!;
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string IpAddress { get; set; } = null!;  
        public string? UserAgent { get; set; } 
        public bool IsSuccess { get; set; }
        public string? FailureReason { get; set; }
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        public string? AdditionalInfo { get; set; }
    }
}
