using Application.Mappings;

namespace Application.Queries.RequestParameters
{
    public record ProductRequestParameters : RequestParameters
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 9;

        public int? CursorId { get; set; }
        public decimal? CursorPrice { get; set; }
        public double? CursorRating { get; set; }
        public int? CursorReviewCount { get; set; }

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
    }

    public enum ProductSort
    {
        Default,
        PriceAsc,
        PriceDesc,
        TopReviews,
        MostReviews
    }

}