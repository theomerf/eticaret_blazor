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

        public ProductFilterParametersAdmin Clone()
        {
            return new ProductFilterParametersAdmin
            {
                SearchTerm = this.SearchTerm,
                MinPrice = this.MinPrice,
                MaxPrice = this.MaxPrice,
                Brand = this.Brand,
                IsShowCase = this.IsShowCase,
                IsDiscount = this.IsDiscount,
                SortBy = this.SortBy,
                PageNumber = this.PageNumber,
                PageSize = this.PageSize
            };
        }
    }


}
