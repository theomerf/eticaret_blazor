using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class OrderLineConfig : IEntityTypeConfiguration<OrderLine>
    {
        public void Configure(EntityTypeBuilder<OrderLine> builder)
        {
            builder.HasKey(ol => ol.OrderLineId);

            builder.Property(ol => ol.ProductName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(ol => ol.ActualPrice)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(ol => ol.DiscountPrice)
                .HasPrecision(18, 2);

            builder.Property(ol => ol.ImageUrl)
                .HasMaxLength(2048);

            builder.Property(ol => ol.DeletedByUserId)
                .HasMaxLength(450);

            builder.HasQueryFilter(ol => !ol.IsDeleted);

            builder.HasIndex(ol => ol.OrderId)
                .HasDatabaseName("IX_OrderLines_OrderId");

            builder.HasIndex(ol => ol.ProductId)
                .HasDatabaseName("IX_OrderLines_ProductId");

            builder.Ignore(ol => ol.LineTotal);
        }
    }
}
