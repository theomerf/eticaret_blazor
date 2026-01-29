using Application.Queries.RequestParameters;

namespace Application.Mappings
{
    public static class ProductSortMapper
    {
        public static ProductSort FromQuery(string? sort)
        {
            return sort switch
            {
                "price_asc" => ProductSort.PriceAsc,
                "price_desc" => ProductSort.PriceDesc,
                "topReviews" => ProductSort.TopReviews,
                "mostReviews" => ProductSort.MostReviews,
                _ => ProductSort.Default
            };
        }

        public static string ToQuery(ProductSort sort)
        {
            return sort switch
            {
                ProductSort.PriceAsc => "price_asc",
                ProductSort.PriceDesc => "price_desc",
                ProductSort.TopReviews => "topReviews",
                ProductSort.MostReviews => "mostReviews",
                _ => "default"
            };
        }
    }

}
