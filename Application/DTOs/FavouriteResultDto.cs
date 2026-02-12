namespace Application.DTOs
{
    public record FavouriteResultDto
    {
        public List<int> FavouriteProductVariantsId { get; set; } = [];
    }
}
