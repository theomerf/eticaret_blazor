using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities

{
    public class Order : SoftDeletableEntity
    {
        public int OrderId { get; set; }
        public string UserId { get; set; } = null!;
        public User? User { get; set; }
        public ICollection<OrderLine> Lines { get; set; } = new List<OrderLine>();
        public string OrderNumber { get; set; } = null!;
        public string? Name { get; set; }
        public string? Line1 { get; set; }
        public string? Line2 { get; set; }
        public string? Line3 { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; } = "Türkiye";
        public string? PhoneNumber { get; set; }
        public OrderStatus OrderStatus { get; set; } = OrderStatus.Pending;
        public bool GiftWrap { get; set; }
        public DateTime OrderedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public string? TrackingNumber { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; } = 0;
        public decimal ShippingCost { get; set; } = 0;
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CreditCard;
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
        public string? CustomerNotes { get; set; }
        public string? AdminNotes { get; set; }
    }

    public enum OrderStatus
    {
        Pending,
        Processing,
        Shipped,
        Delivered,
        Cancelled,
        Returned
    }

    public enum PaymentMethod
    {
        CreditCard,
        BankTransfer,
        CashOnDelivery
    }

    public enum PaymentStatus
    {
        Pending,
        Completed,
        Failed,
        Refunded
    }
}