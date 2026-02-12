namespace Application.DTOs
{
    public class ProductVariantDto
    {
        public int ProductVariantId { get; set; }
        public int ProductId { get; set; }

        public ICollection<ProductImageDto>? Images { get; set; }

        public string? Color { get; set; }
        public string? Size { get; set; }
        public decimal? WeightOverride { get; set; }
        public decimal? LengthOverride { get; set; }
        public decimal? WidthOverride { get; set; }
        public decimal? HeightOverride { get; set; }
        public List<ProductSpecificationDto> VariantSpecifications { get; set; } = new List<ProductSpecificationDto>();
        public decimal Price { get; set; }
        public int Discount { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int Stock { get; set; }
        public string? Gtin { get; set; }
        public string? Sku { get; set; }
        
        public bool IsActive { get; set; }
        public bool IsDefault { get; set; }
    }
}
