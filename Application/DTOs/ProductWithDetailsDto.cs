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
        public string? Summary { get; set; }
        public string? LongDescription { get; set; }
        public int CategoryId { get; set; }
        public ICollection<ProductImage>? Images { get; set; }
        public decimal ActualPrice { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int Stock { get; set; }
        public int Discount => DiscountPrice.HasValue && DiscountPrice.Value > 0
           ? (int)((1 - DiscountPrice.Value / ActualPrice) * 100) : 0;
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public string? Brand { get; set; }
        public string? Gtin { get; set; }
        public string? Color { get; set; }
        public bool ShowCase { get; set; } = false;
    }
}
