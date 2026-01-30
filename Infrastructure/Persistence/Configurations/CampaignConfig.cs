using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class CampaignConfig : IEntityTypeConfiguration<Campaign>
    {
        public void Configure(EntityTypeBuilder<Campaign> builder)
        {
            builder.HasKey(c => c.CampaignId);

            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(c => c.Description)
                .HasMaxLength(1000);

            builder.Property(c => c.Value)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(c => c.MinOrderAmount)
                .HasPrecision(18, 2);

            builder.Property(c => c.MaxDiscountAmount)
                .HasPrecision(18, 2);

            builder.Property(c => c.DeletedByUserId)
                .HasMaxLength(450);

            builder.HasQueryFilter(c => !c.IsDeleted);

            builder.HasIndex(c => new { c.IsActive, c.StartsAt, c.EndsAt })
                .HasFilter("[IsActive] = 1 AND [IsDeleted] = 0")
                .HasDatabaseName("IX_Campaigns_Active_Dates");

            builder.HasIndex(c => c.Priority)
                .HasDatabaseName("IX_Campaigns_Priority");

            builder.HasIndex(c => new { c.Scope, c.IsActive })
                .HasFilter("[IsActive] = 1 AND [IsDeleted] = 0")
                .HasDatabaseName("IX_Campaigns_Scope_Active");

            builder.HasIndex(c => c.IsDeleted)
                .HasFilter("[IsDeleted] = 0")
                .HasDatabaseName("IX_Campaigns_IsDeleted_Filtered");
        }
    }
}
