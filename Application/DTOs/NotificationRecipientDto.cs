namespace Application.DTOs
{
    public record NotificationRecipientDto
    {
        public string UserId { get; set; } = null!;
        public string? Email { get; set; }
        public bool IsSent { get; set; }
        public bool IsRead { get; set; }
    }
}
