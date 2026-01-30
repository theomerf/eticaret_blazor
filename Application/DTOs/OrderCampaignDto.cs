using Domain.Entities;

namespace Application.DTOs
{
    /// <summary>
    /// DTO for applied campaigns in orders
    /// </summary>
    public record OrderCampaignDto
    {
        public int OrderCampaignId { get; set; }
        public int CampaignId { get; set; }
        public string CampaignName { get; set; } = null!;
        
        public CampaignType CampaignType { get; set; }
        public CampaignScope CampaignScope { get; set; }
        
        public decimal CampaignValue { get; set; }
        public decimal DiscountAmount { get; set; }
        public int Priority { get; set; }
    }
}
