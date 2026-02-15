using Application.DTOs;
using Application.Queries.RequestParameters;

namespace ETicaret.Models
{
    public class ProductListViewModelAdmin
    {
        public IEnumerable<ProductDto> Products { get; set; } = [];
        public Pagination Pagination { get; set; } = new();
        public int TotalCount { get; set; }
        public ProductRequestParametersAdmin FilterParams { get; set; } = new();

        public bool HasFiltered()
        {
            if (FilterParams == null) return false;

            return !string.IsNullOrEmpty(FilterParams.SearchTerm) ||
                FilterParams.MinPrice.HasValue ||
                FilterParams.MaxPrice.HasValue ||
                !string.IsNullOrEmpty(FilterParams.Brand) ||
                FilterParams.IsShowCase == true ||
                FilterParams.IsDiscount == true;
        }
    }
}
