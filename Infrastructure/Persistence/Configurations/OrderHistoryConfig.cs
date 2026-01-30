using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class OrderHistoryConfig : IEntityTypeConfiguration<OrderHistory>
    {
        public void Configure(EntityTypeBuilder<OrderHistory> builder)
        {
            builder.HasKey(oh => oh.OrderHistoryId);

            builder.Property(oh => oh.Description)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(oh => oh.CreatedByUserId)
                .HasMaxLength(450);

            builder.Property(oh => oh.DeletedByUserId)
                .HasMaxLength(450);

            builder.HasQueryFilter(oh => !oh.IsDeleted);

            builder.HasIndex(oh => oh.OrderId)
                .HasDatabaseName("IX_OrderHistory_OrderId");

            builder.HasIndex(oh => new { oh.OrderId, oh.CreatedAt })
                .IsDescending(false, true)
                .HasDatabaseName("IX_OrderHistory_Order_CreatedAt");

            builder.HasIndex(oh => oh.EventType)
                .HasDatabaseName("IX_OrderHistory_EventType");

            builder.HasOne(oh => oh.CreatedByUser)
                .WithMany()
                .HasForeignKey(oh => oh.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
