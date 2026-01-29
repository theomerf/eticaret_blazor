using Application.DTOs;
using Application.Queries.RequestParameters;

namespace ETicaret.Models
{
    public class ProductListViewModelAdmin
    {
        public IEnumerable<ProductDto> Products { get; set; } = new List<ProductDto>();
        public int TotalCount { get; set; }
        public Pagination Pagination { get; set; } = new();
        public ProductFilterParametersAdmin FilterParams { get; set; } = new();
    }

    public class ProductFilterParametersAdmin : ProductRequestParametersAdmin
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
