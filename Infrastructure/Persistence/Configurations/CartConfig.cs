using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class CartConfig : IEntityTypeConfiguration<Cart>
    {
        public void Configure(EntityTypeBuilder<Cart> builder)
        {
            builder.HasKey(c => c.CartId);

            builder.Property(c => c.UserId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(c => c.Version)
                .IsConcurrencyToken();

            builder.HasMany(c => c.Lines)
                .WithOne(cl => cl.Cart)
                .HasForeignKey(cl => cl.CartId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
