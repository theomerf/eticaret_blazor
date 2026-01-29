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

            builder.Property(cl => cl.ActualPrice)
                   .IsRequired()
                   .HasPrecision(18, 2);

            builder.Property(cl => cl.DiscountPrice)
                   .HasPrecision(18, 2);

            builder.HasOne(cl => cl.Product)
                   .WithMany()
                   .HasForeignKey(cl => cl.ProductId)
                   .OnDelete(DeleteBehavior.Restrict)
                   .IsRequired(false);
        }
    }
}
