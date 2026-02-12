namespace Application.DTOs
{
    public record CategoryWithDetailsDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? Description { get; set; }
        public string? IconUrl { get; set; }
        public int? ParentId { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsVisible { get; set; }
        public bool IsFeatured { get; set; } 
    }
}
