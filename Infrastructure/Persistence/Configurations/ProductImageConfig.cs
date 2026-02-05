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

            builder.HasIndex(pi => pi.ProductId)
                   .HasDatabaseName("IX_ProductImage_ProductId");

            builder.HasIndex(pi => new { pi.ProductId, pi.IsPrimary })
                .HasFilter("[IsPrimary] = 1 AND [IsDeleted] = 0")
                .HasDatabaseName("IX_ProductImage_ProductId_Primary_Filtered");

            builder.HasOne(pi => pi.Product)
                   .WithMany(p => p.Images)
                   .HasForeignKey(pi => pi.ProductId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
