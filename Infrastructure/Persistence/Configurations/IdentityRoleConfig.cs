using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class IdentityRoleConfig : IEntityTypeConfiguration<IdentityRole>
    {
        public void Configure(EntityTypeBuilder<IdentityRole> builder)
        {
            builder.HasData(
                new IdentityRole
                {
                    Id = "c1a60b8a-1f4d-4fdc-8f5b-d1b8b1b6d3e1",
                    Name = "Admin",
                    NormalizedName = "ADMIN"
                },
                new IdentityRole
                {
                    Id = "d5f89a4c-2e3d-4f7e-9f5a-c4d8c1e7b5d2",
                    Name = "User",
                    NormalizedName = "USER"
                },
                new IdentityRole
                {
                    Id = "e2b6c7d8-3a9b-4e1f-8c5d-a7f3d2b4c9e6",
                    Name = "Editor",
                    NormalizedName = "EDITOR"
                }
            );
        }
    }
}
