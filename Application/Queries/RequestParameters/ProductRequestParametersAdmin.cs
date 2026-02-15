using Application.Mappings;

namespace Application.Queries.RequestParameters
{
    public record ProductRequestParametersAdmin : RequestParametersAdmin
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
    }
}
