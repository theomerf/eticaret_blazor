namespace Application.DTOs
{
    public record ProductSalesDto
    {
        public int ProductId { get; init; }
        public string ProductName { get; init; } = null!;
        public int TotalQuantitySold { get; init; }
        public decimal TotalRevenue { get; init; }
    }
}