using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class ProductVariantConfig : IEntityTypeConfiguration<ProductVariant>
    {
        public void Configure(EntityTypeBuilder<ProductVariant> builder)
        {
            builder.HasKey(v => v.ProductVariantId);

            builder.Property(v => v.Price)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(v => v.DiscountPrice)
                .HasPrecision(18, 2);

            builder.Property(v => v.Color)
                .HasMaxLength(50);

            builder.Property(v => v.Size)
                .HasMaxLength(50);

            builder.Property(v => v.WeightOverride)
                .HasPrecision(18, 2);

            builder.Property(v => v.LengthOverride)
                .HasPrecision(18, 2);

            builder.Property(v => v.WidthOverride)
                .HasPrecision(18, 2);

            builder.Property(v => v.HeightOverride)
                .HasPrecision(18, 2);

            builder.Property(p => p.VariantSpecificationsJson);

            builder.Property(v => v.Gtin)
                .HasMaxLength(50);

            builder.Property(v => v.Sku)
                .HasMaxLength(50);

            builder.Ignore(p => p.Discount);
            
            builder.HasQueryFilter(v => !v.IsDeleted);

            builder.HasIndex(v => new { v.ProductId, v.CombinationKey })
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false")
                .HasDatabaseName("IX_ProductVariants_Unique_Attributes");

            builder.HasOne(v => v.Product)
                .WithMany(p => p.Variants)
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.Images)
                .WithOne(pi => pi.ProductVariant)
                .HasForeignKey(pi => pi.ProductVariantId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
