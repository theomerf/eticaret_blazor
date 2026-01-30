using Application.Common.Validation.Attributes;
using Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    /// <summary>
    /// DTO for creating campaigns with comprehensive validation
    /// </summary>
    public record CampaignDtoForCreation
    {
        [Required(ErrorMessage = "Kampanya adı gereklidir.")]
        [MaxLength(200, ErrorMessage = "Kampanya adı en fazla 200 karakter olabilir.")]
        [MinLength(3, ErrorMessage = "Kampanya adı en az 3 karakter olmalıdır.")]
        [NoXss]
        public string Name { get; set; } = null!;

        [MaxLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir.")]
        [NoXss]
        public string? Description { get; set; }

        [Required(ErrorMessage = "İndirim tipi gereklidir.")]
        public CampaignType Type { get; set; }

        [Required(ErrorMessage = "İndirim değeri gereklidir.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "İndirim değeri 0'dan büyük olmalıdır.")]
        public decimal Value { get; set; }

        [Required(ErrorMessage = "Kapsam gereklidir.")]
        public CampaignScope Scope { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Minimum sipariş tutarı 0'dan küçük olamaz.")]
        public decimal? MinOrderAmount { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Maksimum indirim tutarı 0'dan büyük olmalıdır.")]
        public decimal? MaxDiscountAmount { get; set; }

        [Required(ErrorMessage = "Başlangıç tarihi gereklidir.")]
        public DateTime StartsAt { get; set; }

        [Required(ErrorMessage = "Bitiş tarihi gereklidir.")]
        public DateTime EndsAt { get; set; }

        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "Öncelik gereklidir.")]
        [Range(0, int.MaxValue, ErrorMessage = "Öncelik 0'dan küçük olamaz.")]
        public int Priority { get; set; }

        public bool IsStackable { get; set; } = false;
    }
}
