namespace Application.DTOs
{
    public record ProductDto
    {
        public int ProductId { get; set; }
        public int DefaultVariantId { get; set; }
        public string ProductName { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public List<ProductImageDto>? Images { get; set; }
        
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        
        public int TotalStock { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public bool ShowCase { get; set; }
        public int Discount { get; set; }
    }
}
