using Domain.Entities;

namespace Application.DTOs
{
    public record CouponDto
    {
        public int CouponId { get; set; }
        public string Code { get; set; } = null!;
        
        public CouponScope Scope { get; set; }
        public CouponType Type { get; set; }
        public decimal Value { get; set; }
        
        public decimal? MinOrderAmount { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }
        
        public bool IsSingleUsePerUser { get; set; }
        public int UsageLimit { get; set; }
        public int UsedCount { get; set; }
        
        public bool IsActive { get; set; }
        
        // Computed properties
        public bool IsCurrentlyValid => IsActive && DateTime.UtcNow >= StartsAt && DateTime.UtcNow <= EndsAt;
        public int RemainingUses => UsageLimit > 0 ? Math.Max(0, UsageLimit - UsedCount) : int.MaxValue;
    }
}
