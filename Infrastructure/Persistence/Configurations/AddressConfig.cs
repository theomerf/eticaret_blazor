using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class AddressConfig : IEntityTypeConfiguration<Address>
    {
        public void Configure(EntityTypeBuilder<Address> builder)
        {
            builder.HasKey(a => a.AddressId);

            builder.Property(a => a.Title)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(a => a.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(a => a.LastName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(a => a.Phone)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(a => a.City)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(a => a.District)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(a => a.AddressLine)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(a => a.PostalCode)
                .HasMaxLength(20);

            builder.Property(a => a.DeletedByUserId)
                .HasMaxLength(450);

            builder.HasQueryFilter(a => !a.IsDeleted);

            builder.HasIndex(r => r.UserId)
                .HasDatabaseName("IX_Addresses_UserId");

            builder.HasIndex(a => new { a.UserId, a.IsDefault, a.IsDeleted })
                .HasFilter("[IsDefault] = 1 AND [IsDeleted] = 0")
                .HasDatabaseName("IX_Addresses_UserId_IsDefault");

            builder.HasOne(a => a.User)
                .WithMany(u => u.Addresses)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
