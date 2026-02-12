using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class OrderLinePaymentTransactionConfig : IEntityTypeConfiguration<OrderLinePaymentTransaction>
    {
        public void Configure(EntityTypeBuilder<OrderLinePaymentTransaction> builder)
        {
            builder.HasKey(t => t.OrderLinePaymentTransactionId);

            builder.Property(t => t.ItemId)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(t => t.PaymentTransactionId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(t => t.TransactionStatus)
                .IsRequired();

            builder.Property(t => t.Price)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(t => t.PaidPrice)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(t => t.IsRefunded)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(t => t.RefundTransactionId)
                .HasMaxLength(100);

            builder.Property(t => t.RefundedAt);

            builder.Property(t => t.CreatedAt)
                .IsRequired();

            builder.Property(ol => ol.DeletedByUserId)
                .HasMaxLength(450);

            builder.HasQueryFilter(ol => !ol.IsDeleted);

            builder.HasIndex(t => t.OrderLineId)
                .IsUnique()
                .HasDatabaseName("IX_OrderLinePaymentTransactions_OrderLineId");

            builder.HasIndex(t => t.PaymentTransactionId)
                .HasDatabaseName("IX_OrderLinePaymentTransactions_PaymentTransactionId");

            builder.HasIndex(t => t.IsRefunded)
                .HasDatabaseName("IX_OrderLinePaymentTransactions_IsRefunded");

            builder.HasIndex(t => new { t.OrderLineId, t.IsRefunded })
                .HasDatabaseName("IX_OrderLinePaymentTransactions_OrderLine_Refund");

            builder.HasOne(t => t.OrderLine)
                .WithMany()
                .HasForeignKey(t => t.OrderLineId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
