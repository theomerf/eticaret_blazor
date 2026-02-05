using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class UserConfig : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasQueryFilter(a => !a.IsDeleted);

            builder.HasIndex(a => a.IsDeleted)
                .HasFilter("\"IsDeleted\" = false")
                .HasDatabaseName("IX_Accounts_IsDeleted_Filtered");

            builder.Property(a => a.FirstName)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(a => a.LastName)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(a => a.DeletedByUserId)
                   .HasMaxLength(450);

            builder.Property(a => a.PhoneNumber)
                   .IsRequired()
                   .HasMaxLength(15);

            builder.Property(a => a.Email)
                   .IsRequired()
                   .HasMaxLength(256);

            builder.HasMany(u => u.UserReviews)
                   .WithOne(ur => ur.User)
                   .HasForeignKey(ur => ur.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.Addresses)
                   .WithOne(a => a.User)
                   .HasForeignKey(a => a.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
