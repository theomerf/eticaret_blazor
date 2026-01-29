using Application.Mappings;

namespace Application.Queries.RequestParameters
{
    public class ProductRequestParametersAdmin : RequestParameters
    {
        public int? CategoryId { get; set; }
        public int? MinPrice { get; set; }
        public int? MaxPrice { get; set; }
        public bool? IsValidPrice =>
            MinPrice.HasValue && MaxPrice.HasValue
                ? MaxPrice > MinPrice
                : null;
        public string? Brand { get; set; }
        public bool? IsShowCase { get; set; }
        public bool? IsDiscount { get; set; }
        public string? SortBy { get; set; }
        public ProductSort SortEnum => ProductSortMapper.FromQuery(SortBy);

        const int _maxPageSize = 100;
        private int _pageSize = 10;
        public int PageNumber { get; set; } = 1;
        public int PageSize
        {
            get { return _pageSize; }
            set { _pageSize = (value > _maxPageSize) ? _maxPageSize : value; }
        }

        public virtual void Validate()
        {
            if (PageNumber < 1)
                PageNumber = 1;

            if (PageSize < 1)
                PageSize = 10;
        }
    }
}
