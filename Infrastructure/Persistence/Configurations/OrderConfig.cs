using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class OrderConfig : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.HasKey(o => o.OrderId);

            builder.Property(o => o.OrderNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(o => o.UserId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(o => o.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(o => o.LastName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(o => o.Phone)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(o => o.City)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(o => o.District)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(o => o.AddressLine)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(o => o.PostalCode)
                .HasMaxLength(20);

            builder.Property(o => o.SubTotal)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(o => o.TaxAmount)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(o => o.ShippingCost)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(o => o.TotalAmount)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(o => o.Currency)
                .IsRequired()
                .HasMaxLength(3);

            builder.Property(o => o.TotalDiscountAmount)
                .HasPrecision(18, 2);

            builder.Property(o => o.CouponDiscountAmount)
                .HasPrecision(18, 2);

            builder.Property(o => o.CampaignDiscountTotal)
                .HasPrecision(18, 2);

            builder.Property(o => o.CouponCode)
                .HasMaxLength(50);

            builder.Property(o => o.TrackingNumber)
                .HasMaxLength(100);

            builder.Property(o => o.ShippingCompanyName)
                .HasMaxLength(100);

            builder.Property(o => o.ShippingServiceName)
                .HasMaxLength(100);

            builder.Property(o => o.PaymentProvider)
                .HasMaxLength(50);

            builder.Property(o => o.PaymentTransactionId)
                .HasMaxLength(200);

            builder.Property(o => o.CardType)
                .HasMaxLength(50);

            builder.Property(o => o.CardAssociation)
                .HasMaxLength(50);

            builder.Property(o => o.CardFamily)
                .HasMaxLength(50);

            builder.Property(o => o.BankName)
                .HasMaxLength(100);

            builder.Property(o => o.LastFourDigits)
                .HasMaxLength(4);

            builder.Property(o => o.CustomerNotes)
                .HasMaxLength(1000);

            builder.Property(o => o.AdminNotes)
                .HasMaxLength(2000);

            builder.Property(o => o.DeletedByUserId)
                .HasMaxLength(450);

            builder.HasQueryFilter(o => !o.IsDeleted);

            builder.HasIndex(o => o.OrderNumber)
                .IsUnique()
                .HasDatabaseName("IX_Orders_OrderNumber");

            builder.HasIndex(o => o.UserId)
                .HasDatabaseName("IX_Orders_UserId");

            builder.HasIndex(o => new { o.UserId, o.OrderStatus, o.IsDeleted })
                .HasFilter("\"IsDeleted\" = false")
                .HasDatabaseName("IX_Orders_User_Status");

            builder.HasIndex(o => o.OrderedAt)
                .IsDescending()
                .HasDatabaseName("IX_Orders_OrderedAt_Desc");

            builder.HasIndex(o => o.OrderStatus)
                .HasDatabaseName("IX_Orders_OrderStatus");

            builder.HasIndex(o => o.PaymentStatus)
                .HasDatabaseName("IX_Orders_PaymentStatus");

            builder.HasIndex(o => new { o.OrderStatus, o.PaymentStatus, o.IsDeleted })
                .HasFilter("\"IsDeleted\" = false")
                .HasDatabaseName("IX_Orders_Status_Payment");

            builder.HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(o => o.Lines)
                .WithOne(ol => ol.Order)
                .HasForeignKey(ol => ol.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(o => o.AppliedCampaigns)
                .WithOne(oc => oc.Order)
                .HasForeignKey(oc => oc.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(o => o.History)
                .WithOne(oh => oh.Order)
                .HasForeignKey(oh => oh.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
