using Application.Common.Validation.Attributes;
using Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    /// <summary>
    /// DTO for creating new orders with comprehensive validation
    /// </summary>
    public record OrderDtoForCreation
    {
        // Customer Information
        [Required(ErrorMessage = "Ad gereklidir.")]
        [MaxLength(100, ErrorMessage = "Ad en fazla 100 karakter olabilir.")]
        [MinLength(2, ErrorMessage = "Ad en az 2 karakter olmalıdır.")]
        [NoXss]
        public string FirstName { get; set; } = null!;

        [Required(ErrorMessage = "Soyad gereklidir.")]
        [MaxLength(100, ErrorMessage = "Soyad en fazla 100 karakter olabilir.")]
        [MinLength(2, ErrorMessage = "Soyad en az 2 karakter olmalıdır.")]
        [NoXss]
        public string LastName { get; set; } = null!;

        [Required(ErrorMessage = "Telefon numarası gereklidir.")]
        [MaxLength(20, ErrorMessage = "Telefon numarası en fazla 20 karakter olabilir.")]
        [RegularExpression(@"^[\d\s\-\+\(\)]+$", ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
        public string Phone { get; set; } = null!;

        // Address Information
        [Required(ErrorMessage = "Şehir gereklidir.")]
        [MaxLength(100, ErrorMessage = "Şehir en fazla 100 karakter olabilir.")]
        [MinLength(2, ErrorMessage = "Şehir en az 2 karakter olmalıdır.")]
        [NoXss]
        public string City { get; set; } = null!;

        [Required(ErrorMessage = "İlçe gereklidir.")]
        [MaxLength(100, ErrorMessage = "İlçe en fazla 100 karakter olabilir.")]
        [MinLength(2, ErrorMessage = "İlçe en az 2 karakter olmalıdır.")]
        [NoXss]
        public string District { get; set; } = null!;

        [Required(ErrorMessage = "Adres detayı gereklidir.")]
        [MaxLength(500, ErrorMessage = "Adres detayı en fazla 500 karakter olabilir.")]
        [MinLength(10, ErrorMessage = "Adres detayı en az 10 karakter olmalıdır.")]
        [NoXss]
        public string AddressLine { get; set; } = null!;

        [MaxLength(20, ErrorMessage = "Posta kodu en fazla 20 karakter olabilir.")]
        [RegularExpression(@"^[\d\-\s]*$", ErrorMessage = "Geçerli bir posta kodu giriniz.")]
        public string? PostalCode { get; set; }

        // Shipping Method
        [Required(ErrorMessage = "Kargo yöntemi gereklidir.")]
        public ShippingMethod ShippingMethod { get; set; }

        // Payment Method
        [Required(ErrorMessage = "Ödeme yöntemi gereklidir.")]
        public PaymentMethod PaymentMethod { get; set; }

        // Optional Fields
        public bool GiftWrap { get; set; }

        [MaxLength(1000, ErrorMessage = "Müşteri notu en fazla 1000 karakter olabilir.")]
        [NoXss]
        public string? CustomerNotes { get; set; }

        // Coupon
        [MaxLength(50, ErrorMessage = "Kupon kodu en fazla 50 karakter olabilir.")]
        [RegularExpression(@"^[A-Z0-9\-]*$", ErrorMessage = "Kupon kodu sadece büyük harf, rakam ve tire içerebilir.")]
        public string? CouponCode { get; set; }

        // Cart Lines (will be converted to OrderLines)
        [Required(ErrorMessage = "Sepet boş olamaz.")]
        [MinLength(1, ErrorMessage = "Sepet en az bir ürün içermelidir.")]
        public ICollection<CartLineDto> CartLines { get; set; } = new List<CartLineDto>();
    }
}
