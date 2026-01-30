using Domain.Entities;

namespace Application.DTOs
{
    /// <summary>
    /// DTO for order history events
    /// </summary>
    public record OrderHistoryDto
    {
        public int OrderHistoryId { get; set; }
        public int OrderId { get; set; }
        
        public OrderEventType EventType { get; set; }
        public string Description { get; set; } = null!;
        
        public DateTime CreatedAt { get; set; }
        public string? CreatedByUserId { get; set; }
        public string? CreatedByUserName { get; set; }
        
        public bool IsSystemEvent { get; set; }
    }
}
