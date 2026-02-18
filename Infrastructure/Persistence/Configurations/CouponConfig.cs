using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class CouponConfig : IEntityTypeConfiguration<Coupon>
    {
        public void Configure(EntityTypeBuilder<Coupon> builder)
        {
            builder.HasKey(c => c.CouponId);

            builder.Property(c => c.Code)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(c => c.Value)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(c => c.MinOrderAmount)
                .HasPrecision(18, 2);

            builder.Property(c => c.MaxDiscountAmount)
                .HasPrecision(18, 2);

            builder.Property(c => c.RowVersion)
                .IsRowVersion()
                .HasColumnName("xmin");

            builder.Property(c => c.DeletedByUserId)
                .HasMaxLength(450);

            builder.HasQueryFilter(c => !c.IsDeleted);

            builder.HasIndex(c => c.Code)
                .IsUnique()
                .HasDatabaseName("IX_Coupons_Code");

            builder.HasIndex(c => new { c.IsActive, c.StartsAt, c.EndsAt })
                .HasFilter("\"IsActive\" = true AND \"IsDeleted\" = false")
                .HasDatabaseName("IX_Coupons_Active_Dates");

            builder.HasIndex(c => c.IsDeleted)
                .HasFilter("\"IsDeleted\" = false")
                .HasDatabaseName("IX_Coupons_IsDeleted_Filtered");

            builder.HasMany(c => c.Usages)
                .WithOne(cu => cu.Coupon)
                .HasForeignKey(cu => cu.CouponId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
