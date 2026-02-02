using Application.Common.Validation.Attributes;
using Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public record CouponDtoForCreation
    {
        [Required(ErrorMessage = "Kupon kodu gereklidir.")]
        [MaxLength(50, ErrorMessage = "Kupon kodu en fazla 50 karakter olabilir.")]
        [MinLength(3, ErrorMessage = "Kupon kodu en az 3 karakter olmalıdır.")]
        [RegularExpression(@"^[A-Z0-9\-]+$", ErrorMessage = "Kupon kodu sadece büyük harf, rakam ve tire içerebilir.")]
        public string Code { get; set; } = null!;

        [Required(ErrorMessage = "Kapsam gereklidir.")]
        public CouponScope Scope { get; set; }

        [Required(ErrorMessage = "İndirim tipi gereklidir.")]
        public CouponType Type { get; set; }

        [Required(ErrorMessage = "İndirim değeri gereklidir.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "İndirim değeri 0'dan büyük olmalıdır.")]
        public decimal Value { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Minimum sipariş tutarı 0'dan küçük olamaz.")]
        public decimal? MinOrderAmount { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Maksimum indirim tutarı 0'dan büyük olmalıdır.")]
        public decimal? MaxDiscountAmount { get; set; }

        [Required(ErrorMessage = "Başlangıç tarihi gereklidir.")]
        public DateTime StartsAt { get; set; }

        [Required(ErrorMessage = "Bitiş tarihi gereklidir.")]
        public DateTime EndsAt { get; set; }

        public bool IsSingleUsePerUser { get; set; }

        [Required(ErrorMessage = "Kullanım limiti gereklidir.")]
        [Range(0, int.MaxValue, ErrorMessage = "Kullanım limiti 0'dan küçük olamaz.")]
        public int UsageLimit { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
