using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class OrderCampaignConfig : IEntityTypeConfiguration<OrderCampaign>
    {
        public void Configure(EntityTypeBuilder<OrderCampaign> builder)
        {
            builder.HasKey(oc => oc.OrderCampaignId);

            builder.Property(oc => oc.CampaignName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(oc => oc.CampaignValue)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(oc => oc.DiscountAmount)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.HasIndex(oc => oc.OrderId)
                .HasDatabaseName("IX_OrderCampaigns_OrderId");

            builder.HasIndex(oc => new { oc.OrderId, oc.CampaignId })
                .HasDatabaseName("IX_OrderCampaigns_Order_Campaign");

            builder.HasQueryFilter(oc => !oc.Order.IsDeleted);
        }
    }
}
