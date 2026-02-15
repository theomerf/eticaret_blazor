using Domain.Entities;

namespace Application.DTOs
{
    public record UserDto
    {
        public string Id { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateOnly BirthDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public bool IsActive { get; set; }
        public bool EmailConfirmed { get; set; }
        public string? AdminNotes { get; set; }
        public string? BannedReason { get; set; }
        public int RiskScore { get; set; }
        public string? LastLoginIpAddress { get; set; }
        public List<int> FavouriteProductsId { get; set; } = [];
        public HashSet<string> Roles { get; set; } = [];
    }
}
