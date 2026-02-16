namespace Application.DTOs
{
    public record CampaignUsageDto
    {
        public int OrderCampaignId { get; set; }
        public int CampaignId { get; set; }
        public int OrderId { get; set; }
        public DateTime OrderedAt { get; set; }
        public string UserId { get; set; } = null!;
        public string UserEmail { get; set; } = null!;
        public decimal DiscountAmount { get; set; }
        public decimal OrderTotalAmount { get; set; }
    }
}
