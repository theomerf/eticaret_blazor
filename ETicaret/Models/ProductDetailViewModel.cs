using Application.DTOs;

namespace ETicaret.Models
{
    public class ProductDetailViewModel
    {
        public ProductWithDetailsDto Product { get; set; } = null!;
        public IEnumerable<ProductDto> RelatedProducts { get; set; } = null!;
    }
}
