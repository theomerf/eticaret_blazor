using System.Text.Json.Serialization;

namespace Domain.Entities
{
    public class CartLine
    {
        public int CartLineId { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public string ProductName { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int Quantity { get; set; }
        public int CartId { get; set; }
        [JsonIgnore]
        public Cart Cart { get; set; } = new();

        public int ProductVariantId { get; set; }
        public ProductVariant? Variant { get; set; }
        
        public string? SelectedColor { get; set; }
        public string? SelectedSize { get; set; }
        public string? SpecificationsJson { get; set; }
    }
}
