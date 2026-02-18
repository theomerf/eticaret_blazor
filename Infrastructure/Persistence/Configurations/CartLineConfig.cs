using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class CartLineConfig : IEntityTypeConfiguration<CartLine>
    {
        public void Configure(EntityTypeBuilder<CartLine> builder)
        {
            builder.HasKey(cl => cl.CartLineId);

            builder.Property(cl => cl.ProductName)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(cl => cl.ImageUrl)
                   .HasMaxLength(2048);

            builder.Property(cl => cl.Price)
                   .IsRequired()
                   .HasPrecision(18, 2);

            builder.Property(cl => cl.DiscountPrice)
                   .HasPrecision(18, 2);

            builder.HasIndex(cl => new { cl.CartId, cl.ProductVariantId })
                .IsUnique();

            builder.HasOne(cl => cl.Product)
                .WithMany()
                .HasForeignKey(cl => cl.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(cl => cl.Variant)
                .WithMany()
                .HasForeignKey(cl => cl.ProductVariantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(cl => cl.SelectedColor)
                .HasMaxLength(50);

            builder.Property(cl => cl.SelectedSize)
                .HasMaxLength(50);

            builder.HasQueryFilter(cl => !cl.Product!.IsDeleted && (cl.Variant == null || !cl.Variant.IsDeleted));
        }
    }
}
