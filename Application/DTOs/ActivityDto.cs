namespace Application.DTOs
{
    public record ActivityDto
    {
        public int ActivityId { get; init; }
        public string Title { get; init; } = null!;
        public string Description { get; init; } = null!;
        public string Icon { get; init; } = null!;
        public string ColorClass { get; init; } = null!;
        public string? Link { get; init; }
        public DateTime CreatedAt { get; init; }
    }
}
