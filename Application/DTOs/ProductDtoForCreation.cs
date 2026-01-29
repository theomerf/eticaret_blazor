using Application.Common.Validation.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public record ProductDtoForCreation
    {
        [Required(ErrorMessage = "Ürün adı gereklidir.")]
        [MaxLength(200, ErrorMessage = "Ürün adı en fazla 200 karakter olabilir.")]
        [MinLength(2, ErrorMessage = "Ürün adı en az 2 karakter olabilir.")]
        [NoXss]
        public string ProductName { get; set; } = null!;

        [MaxLength(60, ErrorMessage = "Meta başlık en fazla 60 karakter olabilir.")]
        [NoXss]
        public string? MetaTitle { get; set; }

        [MaxLength(160, ErrorMessage = "Meta açıklama en fazla 160 karakter olabilir.")]
        [NoXss]
        public string? MetaDescription { get; set; }

        [MaxLength(1000, ErrorMessage = "Özet en fazla 1000 karakter olabilir.")]
        [MinLength(5, ErrorMessage = "Özet en az 5 karakter olabilir.")]
        [NoXss]
        public string? Summary { get; set; }

        public string? LongDescription { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir kategori seçiniz.")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Fiyat gereklidir.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Fiyat 0'dan büyük olmalıdır.")]
        public decimal ActualPrice { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "İndirimli fiyat 0'dan küçük olamaz.")]
        public decimal? DiscountPrice { get; set; }

        [Required(ErrorMessage = "Stok miktarı gereklidir.")]
        [Range(0, int.MaxValue, ErrorMessage = "Stok 0'dan küçük olamaz.")]
        public int Stock { get; set; }

        [MaxLength(100, ErrorMessage = "Marka en fazla 100 karakter olabilir.")]
        [MinLength(2, ErrorMessage = "Marka en az 2 karakter olabilir.")]
        [NoXss]
        public string? Brand { get; set; }

        [MaxLength(50, ErrorMessage = "GTIN en fazla 50 karakter olabilir.")]
        [MinLength(2, ErrorMessage = "GTIN en az 2 karakter olabilir.")]
        [RegularExpression(@"^[0-9A-Za-z\-]+$")]
        public string? Gtin { get; set; }

        [MaxLength(50, ErrorMessage = "Renk en fazla 50 karakter olabilir.")]
        [MinLength(2, ErrorMessage = "Renk en az 2 karakter olabilir.")]
        [NoXss]
        public string? Color { get; set; }

        public bool ShowCase { get; set; }
    }
}
