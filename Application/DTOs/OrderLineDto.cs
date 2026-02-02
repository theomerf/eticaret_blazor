namespace Application.DTOs
{
    public record OrderLineDto
    {
        public int OrderLineId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string CategoryName { get; set; } = null!;
        public string? SubCategoryName { get; set; }

        public int Quantity { get; set; }
        public decimal ActualPrice { get; set; }
        public decimal? DiscountPrice { get; set; }
        public string? ImageUrl { get; set; }
        
        // Computed property
        public decimal LineTotal => (DiscountPrice ?? ActualPrice) * Quantity;
        public decimal FinalPrice => DiscountPrice ?? ActualPrice;
    }
}
