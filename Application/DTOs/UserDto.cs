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
        public DateTime BirthDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public List<int> FavouriteProductsId { get; set; } = new List<int>();
        public HashSet<string> Roles { get; set; } = new HashSet<string>();
    }
}
