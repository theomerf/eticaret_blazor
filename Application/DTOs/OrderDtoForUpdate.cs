using Application.Common.Validation.Attributes;
using Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    /// <summary>
    /// DTO for updating order status and admin fields
    /// </summary>
    public record OrderDtoForUpdate
    {
        [Required(ErrorMessage = "Sipariş ID gereklidir.")]
        public int OrderId { get; set; }

        // Status Updates
        public OrderStatus? OrderStatus { get; set; }
        public PaymentStatus? PaymentStatus { get; set; }

        // Shipping Updates
        [MaxLength(100, ErrorMessage = "Takip numarası en fazla 100 karakter olabilir.")]
        public string? TrackingNumber { get; set; }

        [MaxLength(100, ErrorMessage = "Kargo şirketi adı en fazla 100 karakter olabilir.")]
        [NoXss]
        public string? ShippingCompanyName { get; set; }

        [MaxLength(100, ErrorMessage = "Kargo servisi adı en fazla 100 karakter olabilir.")]
        [NoXss]
        public string? ShippingServiceName { get; set; }

        // Admin Notes
        [MaxLength(2000, ErrorMessage = "Admin notu en fazla 2000 karakter olabilir.")]
        [NoXss]
        public string? AdminNotes { get; set; }

        // Payment Updates
        [MaxLength(50, ErrorMessage = "Ödeme sağlayıcısı en fazla 50 karakter olabilir.")]
        public string? PaymentProvider { get; set; }

        [MaxLength(200, ErrorMessage = "İşlem ID en fazla 200 karakter olabilir.")]
        public string? PaymentTransactionId { get; set; }
    }
}
