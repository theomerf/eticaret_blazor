using Domain.Entities;

namespace Application.DTOs
{
    public record ProductImageDto
    {
        public int ProductImageId { get; set; }
        public int ProductId { get; set; }
        public string ImageUrl { get; set; } = null!;
        public bool IsPrimary { get; set; }
        public string? Caption { get; set; }
        public int DisplayOrder { get; set; }
    }
}
