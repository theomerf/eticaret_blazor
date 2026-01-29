namespace Application.DTOs
{
    public record FavouriteResultDto
    {
        public ICollection<int> FavouriteProductsId { get; set; } = new List<int>();
    }
}
