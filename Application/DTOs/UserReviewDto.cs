using Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public record UserReviewDto
    {
        public int UserReviewId { get; set; }
        public int Rating { get; set; }
        public string? ReviewText { get; set; }
        public string? ReviewTitle { get; set; }
        public DateTime ReviewDate { get; set; }
        public DateTime ReviewUpdateDate { get; set; }
        public string? ReviewPictureUrl { get; set; }
        public bool IsFeatured { get; set; }
        public string ReviewerName { get; set; } = null!;
        public int HelpfulCount { get; set; }
        public int NotHelpfulCount { get; set; }
        public ProductDto? Product { get; set; }
    }
}
