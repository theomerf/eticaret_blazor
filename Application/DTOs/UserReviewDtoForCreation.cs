using Application.Common.Validation.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public record UserReviewDtoForCreation
    {
        [Required(ErrorMessage = "Değerlendirme puanı gereklidir.")]
        [Range(1, 5, ErrorMessage = "Değerlendirme puanı 1 ile 5 arasında olabilir.")]
        public int Rating { get; set; }

        [MaxLength(2000, ErrorMessage = "Değerlendirme metni en fazla 2000 karakter olabilir.")]
        [MinLength(5, ErrorMessage = "Değerlendirme metni en az 5 karakter olabilir.")]
        [NoXss]
        public string? ReviewText { get; set; }

        [MaxLength(200, ErrorMessage = "Değerlendirme başlığı en fazla 200 karakter olabilir.")]
        [MinLength(3, ErrorMessage = "Değerlendirme başlığı en az 3 karakter olabilir.")]
        [NoXss]
        public string? ReviewTitle { get; set; }

        [MaxLength(500, ErrorMessage = "Resim URL'si en fazla 500 karakter olabilir.")]
        public string? ReviewPictureUrl { get; set; }

        [Required(ErrorMessage = "Ürün ID gereklidir.")]
        [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir ürün ID'si giriniz.")]
        public int ProductId { get; set; }
    }
}
