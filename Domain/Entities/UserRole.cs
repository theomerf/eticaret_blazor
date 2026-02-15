using Microsoft.AspNetCore.Identity;
using System.Data;

namespace Domain.Entities
{
    public class UserRole : IdentityUserRole<string>
    {
        public User User { get; set; } = null!;
        public Role Role { get; set; } = null!;
    }
}
