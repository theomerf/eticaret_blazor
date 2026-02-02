using Domain.Entities;

namespace Application.DTOs
{
    public record CampaignDto
    {
        public int CampaignId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        
        public CampaignType Type { get; set; }
        public decimal Value { get; set; }
        public CampaignScope Scope { get; set; }
        
        public decimal? MinOrderAmount { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }
        
        public bool IsActive { get; set; }
        public int Priority { get; set; }
        public bool IsStackable { get; set; }
        
        public bool IsCurrentlyActive => IsActive && DateTime.UtcNow >= StartsAt && DateTime.UtcNow <= EndsAt;
    }
}
