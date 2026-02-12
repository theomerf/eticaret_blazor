using System.Text.Json.Serialization;

namespace Application.DTOs
{
    public class CartLineDto
    {
        public int CartLineId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public decimal ActualPrice { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int Quantity { get; set; }
        public int ProductVariantId { get; set; }
        public string? SelectedColor { get; set; }
        public string? SelectedSize { get; set; }
        public List<ProductSpecificationDto> VariantSpecifications { get; set; } = [];
        public int CartId { get; set; }
        [JsonIgnore]
        public CartDto Cart { get; set; } = new();
    }

}
