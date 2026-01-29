namespace Application.DTOs
{
    public record OrderDtoForCreation : OrderDto
    {
        public ICollection<CartLineDto> CartLines { get; set; } = new List<CartLineDto>();
        public CartDto? Cart { get; set; } = null;
    }
}
