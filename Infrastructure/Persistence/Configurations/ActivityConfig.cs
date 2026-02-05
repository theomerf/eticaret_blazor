using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class ActivityConfig : IEntityTypeConfiguration<Activity>
    {
        public void Configure(EntityTypeBuilder<Activity> builder)
        {
            builder.HasKey(a => a.ActivityId);

            builder.Property(a => a.Title)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(a => a.Description)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(a => a.Icon)
                .HasMaxLength(50)
                .HasDefaultValue("fa-circle");

            builder.Property(a => a.ColorClass)
                .HasMaxLength(100)
                .HasDefaultValue("text-blue-500 bg-blue-100");
                
            builder.Property(a => a.Link)
                .HasMaxLength(250);

            builder.Property(a => a.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
        }
    }
}
