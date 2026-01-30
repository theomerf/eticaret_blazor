using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class CouponUsageConfig : IEntityTypeConfiguration<CouponUsage>
    {
        public void Configure(EntityTypeBuilder<CouponUsage> builder)
        {
            builder.HasKey(cu => cu.CouponUsageId);

            builder.Property(cu => cu.UserId)
                .IsRequired()
                .HasMaxLength(450);

            builder.HasIndex(cu => cu.CouponId)
                .HasDatabaseName("IX_CouponUsages_CouponId");

            builder.HasIndex(cu => cu.UserId)
                .HasDatabaseName("IX_CouponUsages_UserId");

            builder.HasIndex(cu => new { cu.CouponId, cu.UserId })
                .HasDatabaseName("IX_CouponUsages_Coupon_User");

            builder.HasIndex(cu => cu.OrderId)
                .HasDatabaseName("IX_CouponUsages_OrderId");

            builder.HasIndex(cu => cu.UsedAt)
                .IsDescending()
                .HasDatabaseName("IX_CouponUsages_UsedAt_Desc");

            // Query filter to match parent Coupon's soft delete filter
            builder.HasQueryFilter(cu => !cu.Coupon.IsDeleted);

            builder.HasOne(cu => cu.User)
                .WithMany()
                .HasForeignKey(cu => cu.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(cu => cu.Order)
                .WithMany()
                .HasForeignKey(cu => cu.OrderId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
