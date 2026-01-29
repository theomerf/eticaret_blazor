using Domain.Entities;

namespace Application.DTOs
{
    public record ProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public ICollection<ProductImageDto>? Images { get; set; }
        public decimal ActualPrice { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int Stock { get; set; }
        public int Discount => DiscountPrice.HasValue && DiscountPrice.Value > 0
            ? (int)((1 - DiscountPrice.Value / ActualPrice) * 100) : 0;
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public bool ShowCase { get; set; }
    }
}
