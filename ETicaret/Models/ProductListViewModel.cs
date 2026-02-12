using Application.DTOs;
using Application.Queries.RequestParameters;

namespace ETicaret.Models
{
    public class ProductListViewModel
    {
        public IEnumerable<ProductDto> Products { get; set; } = new List<ProductDto>();
        public int TotalCount { get; set; }
        public ProductFilterParameters FilterParams { get; set; } = new();
    }

    public class ProductFilterParameters : ProductRequestParameters
    {
        public bool HasActiveFilters =>
            !string.IsNullOrEmpty(SearchTerm) ||
            MinPrice.HasValue ||
            MaxPrice.HasValue ||
            !string.IsNullOrEmpty(Brand) ||
            IsShowCase.HasValue ||
            IsDiscount.HasValue;
    }
}
