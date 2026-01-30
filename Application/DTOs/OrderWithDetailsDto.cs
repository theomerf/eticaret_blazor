using Domain.Entities;

namespace Application.DTOs
{
    /// <summary>
    /// Detailed DTO for order detail page - complete information
    /// </summary>
    public record OrderWithDetailsDto
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = null!;
        
        // Customer Information
        public string UserId { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Phone { get; set; } = null!;
        
        // Address
        public string City { get; set; } = null!;
        public string District { get; set; } = null!;
        public string AddressLine { get; set; } = null!;
        public string? PostalCode { get; set; }
        
        // Order Lines
        public ICollection<OrderLineDto> Lines { get; set; } = new List<OrderLineDto>();
        
        // Status & Dates
        public OrderStatus OrderStatus { get; set; }
        public DateTime OrderedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        
        // Pricing
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = "TRY";
        
        // Discounts
        public decimal? TotalDiscountAmount { get; set; }
        public decimal? CouponDiscountAmount { get; set; }
        public decimal? CampaignDiscountTotal { get; set; }
        public string? CouponCode { get; set; }
        
        // Applied Campaigns
        public ICollection<OrderCampaignDto> AppliedCampaigns { get; set; } = new List<OrderCampaignDto>();
        
        // Shipping
        public ShippingMethod ShippingMethod { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public string? TrackingNumber { get; set; }
        public string? ShippingCompanyName { get; set; }
        public string? ShippingServiceName { get; set; }
        
        // Payment
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public string? PaymentProvider { get; set; }
        public string? PaymentTransactionId { get; set; }
        public DateTime? PaidAt { get; set; }
        
        // Additional
        public bool GiftWrap { get; set; }
        public string? CustomerNotes { get; set; }
        public string? AdminNotes { get; set; }
        
        // History
        public ICollection<OrderHistoryDto> History { get; set; } = new List<OrderHistoryDto>();
    }
}
