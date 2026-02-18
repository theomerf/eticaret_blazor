namespace Application.DTOs
{
    public record UserEmailLookupDto
    {
        public string Id { get; set; } = null!;
        public string Email { get; set; } = null!;
    }
}
