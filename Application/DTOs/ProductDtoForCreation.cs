using Application.Common.Validation.Attributes;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        [MaxLength(250, ErrorMessage = "Slug en fazla 250 karakter olabilir.")]
        [NoXss]
        public string? Slug { get; set; }

        [MaxLength(60, ErrorMessage = "Meta başlık en fazla 60 karakter olabilir.")]
        [NoXss]
        public string? MetaTitle { get; set; }

        [MaxLength(160, ErrorMessage = "Meta açıklama en fazla 160 karakter olabilir.")]
        [NoXss]
        public string? MetaDescription { get; set; }

        [Required(ErrorMessage = "Özet gereklidir.")]
        [MaxLength(500, ErrorMessage = "Özet en fazla 500 karakter olabilir.")]
        [MinLength(5, ErrorMessage = "Özet en az 5 karakter olabilir.")]
        [NoXss]
        public string Summary { get; set; } = null!;

        public string? LongDescription { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir kategori seçiniz.")]
        public int CategoryId { get; set; }

        [MaxLength(100, ErrorMessage = "Marka en fazla 100 karakter olabilir.")]
        [MinLength(2, ErrorMessage = "Marka en az 2 karakter olabilir.")]
        [NoXss]
        public string? Brand { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Varsayılan ağırlık 0'dan küçük olamaz.")]
        public decimal? DefaultWeight { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Varsayılan uzunluk 0'dan küçük olamaz.")]
        public decimal? DefaultLength { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Varsayılan genişlik 0'dan küçük olamaz.")]
        public decimal? DefaultWidth { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Varsayılan yükseklik 0'dan küçük olamaz.")]
        public decimal? DefaultHeight { get; set; }

        [MaxLength(100, ErrorMessage = "Üretim yeri en fazla 100 karakter olabilir.")]
        [NoXss]
        public string? ManufacturingCountry { get; set; }

        [MaxLength(200, ErrorMessage = "Garanti bilgisi en fazla 200 karakter olabilir.")]
        [NoXss]
        public string? WarrantyInfo { get; set; }

        public List<ProductVariantDtoForCreation> Variants { get; set; } = [];

        public bool ShowCase { get; set; }

        public List<ProductSpecificationDto> Specifications { get; set; } = [];

        public List<CategoryVariantAttributeDtoForCreation> NewAttributeDefinitions { get; set; } = [];
    }
}
