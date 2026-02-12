using Application.Common.Validation.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class ProductImageDtoForCreation
    {
        [Required(ErrorMessage = "Ürün varyant ID gereklidir.")]
        [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir ürün varyant ID'si giriniz.")]
        public int ProductVariantId { get; set; }
        [Required(ErrorMessage = "Ürün ID gereklidir.")]
        [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir ürün ID'si giriniz.")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Resim URL'si gereklidir.")]
        [MaxLength(2048, ErrorMessage = "Resim URL'si en fazla 2048 karakter olabilir.")]
        /*[Url(ErrorMessage = "Geçerli bir URL giriniz.")]*/
        public string ImageUrl { get; set; } = null!;

        public bool IsPrimary { get; set; }

        [MaxLength(512, ErrorMessage = "Başlık en fazla 512 karakter olabilir.")]
        [NoXss]
        public string? Caption { get; set; }

        [Range(0, 999, ErrorMessage = "Sıralama 0-999 arasında olmalıdır.")]
        public int DisplayOrder { get; set; }
    }
}
