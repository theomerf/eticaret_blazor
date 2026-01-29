using Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public record CategoryDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Slug { get; set; } = null!;
        public int? ParentId { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsFeatured { get; set; }
    }
}
