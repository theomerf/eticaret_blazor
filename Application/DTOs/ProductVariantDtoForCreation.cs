using Application.Common.Validation.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class ProductVariantDtoForCreation
    {
        public int ProductVariantId { get; set; }
        public int ProductId { get; set; }

        [MaxLength(50, ErrorMessage = "Renk en fazla 50 karakter olabilir.")]
        [NoXss]
        public string? Color { get; set; }

        [MaxLength(50, ErrorMessage = "Beden en fazla 50 karakter olabilir.")]
        [NoXss]
        public string? Size { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Varyant ağırlığı 0'dan küçük olamaz.")]
        public decimal? WeightOverride { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Varyant uzunluğu 0'dan küçük olamaz.")]
        public decimal? LengthOverride { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Varyant genişliği 0'dan küçük olamaz.")]
        public decimal? WidthOverride { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Varyant yüksekliği 0'dan küçük olamaz.")]
        public decimal? HeightOverride { get; set; }

        [Required(ErrorMessage = "Fiyat gereklidir.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Fiyat 0'dan büyük olabilir.")]
        public decimal Price { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "İndirimli fiyat 0'dan küçük olamaz.")]
        public decimal? DiscountPrice { get; set; }

        [Required(ErrorMessage = "Stok gereklidir.")]
        [Range(0, int.MaxValue, ErrorMessage = "Stok 0'dan küçük olamaz.")]
        public int Stock { get; set; }

        [MaxLength(50, ErrorMessage = "GTIN en fazla 50 karakter olabilir.")]
        public string? Gtin { get; set; }
        
        [MaxLength(50, ErrorMessage = "SKU en fazla 50 karakter olabilir.")]
        [NoXss]
        public string? Sku { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; } = false;

        public List<ProductSpecificationDto> VariantSpecifications { get; set; } = [];

        public List<ProductImageDtoForCreation> Images { get; set; } = [];
    }
}
