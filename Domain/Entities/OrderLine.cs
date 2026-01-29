namespace Domain.Entities

{
    public class OrderLine : SoftDeletableEntity
    {
        public int OrderLineId { get; set; }
        public int OrderId { get; set; }
        public Order? Order { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public int Quantity { get; set; }
        public string ProductName { get; set; } = null!;
        public decimal? DiscountPrice { get; set; }
        public decimal ActualPrice { get; set; }
        public string? ImageUrl { get; set; }
        public decimal LineTotal => (DiscountPrice ?? ActualPrice) * Quantity;  
    }
}