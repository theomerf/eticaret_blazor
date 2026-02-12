using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Infrastructure.Persistence.Configurations
{
    public class ProductImageConfig : IEntityTypeConfiguration<ProductImage>
    {
        public void Configure(EntityTypeBuilder<ProductImage> builder)
        {
            builder.HasKey(pi => pi.ProductImageId);

            builder.Property(pi => pi.ImageUrl)
                   .IsRequired()
                   .HasMaxLength(2048);

            builder.Property(pi => pi.Caption)
                   .HasMaxLength(512);

            builder.Property(pi => pi.DeletedByUserId)
                   .HasMaxLength(450);

            builder.Property(pi => pi.DisplayOrder)
                   .HasDefaultValue(0);

            builder.Property(p => p.CreatedByUserId)
                    .HasMaxLength(450);

            builder.Property(p => p.UpdatedByUserId)
                    .HasMaxLength(450);

            builder.HasQueryFilter(pi => !pi.IsDeleted);

            builder.HasIndex(pi => pi.ProductVariantId)
                   .HasDatabaseName("IX_ProductImage_ProductVariantId");

            builder.HasIndex(pi => new { pi.ProductVariantId, pi.IsPrimary })
                .HasFilter("\"IsPrimary\" = true AND \"IsDeleted\" = false")
                .HasDatabaseName("IX_ProductImage_ProductVariantId_Primary_Filtered");

            builder.HasOne(pi => pi.ProductVariant)
                   .WithMany(pv => pv.Images)
                   .HasForeignKey(pi => pi.ProductVariantId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
