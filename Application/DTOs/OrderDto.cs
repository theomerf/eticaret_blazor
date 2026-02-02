using Domain.Entities;

namespace Application.DTOs
{
    public record OrderDto
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = null!;
        
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string FullName => $"{FirstName} {LastName}";
        
        public OrderStatus OrderStatus { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        
        public DateTime OrderedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = "TRY";
        
        public int LineCount { get; set; }
        public string? FirstProductImage { get; set; }
    }
}
