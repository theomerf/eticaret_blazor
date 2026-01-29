using Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public record OrderDto
    {
        public int OrderId { get; set; }
        public string? UserName { get; set; }
        public ICollection<OrderLine> Lines { get; set; } = new List<OrderLine>();

        [Required(ErrorMessage = "Name is required.")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Line1 is required.")]
        public string? Line1 { get; set; }
        public string? Line2 { get; set; }
        public string? Line3 { get; set; }
        public string? City { get; set; }
        public bool Shipped { get; set; } = false;
        public bool GiftWrap { get; set; }
        public DateTime? OrderedAt { get; set; } = DateTime.UtcNow;
    }
}
