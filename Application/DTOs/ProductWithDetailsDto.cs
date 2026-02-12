using Domain.Entities;

namespace Application.DTOs
{
    public record ProductWithDetailsDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string Summary { get; set; } = null!;
        public string? LongDescription { get; set; }
        public int CategoryId { get; set; }
        
        public int TotalStock => Variants?.Sum(v => v.Stock) ?? 0;
        public decimal MinPrice => Variants?.Any() == true 
            ? Variants.Min(v => v.DiscountPrice ?? v.Price) 
            : 0;
        public decimal MaxPrice => Variants?.Any() == true 
            ? Variants.Max(v => v.DiscountPrice ?? v.Price) 
            : 0;
        
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public string? Brand { get; set; }

        public List<ProductVariantDto> Variants { get; set; } = [];
        public List<ProductVariantSelectorDto> VariantSelectors { get; set; } = [];

        public decimal? DefaultWeight { get; set; }
        public decimal? DefaultLength { get; set; }
        public decimal? DefaultWidth { get; set; }
        public decimal? DefaultHeight { get; set; }
        public string? ManufacturingCountry { get; set; }
        public string? WarrantyInfo { get; set; }
        public List<ProductSpecificationDto> Specifications { get; set; } = [];

        public bool ShowCase { get; set; } = false;
    }
}
