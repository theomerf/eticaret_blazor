using System;

namespace Domain.Entities
{
    public class Activity
    {
        public int ActivityId { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Icon { get; set; } = "fa-circle";
        public string ColorClass { get; set; } = "text-blue-500 bg-blue-100";
        public string? Link { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
