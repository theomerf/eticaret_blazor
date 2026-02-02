using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class ProductConfig : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.HasKey(p => p.ProductId);

            builder.Property(p => p.ProductName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(p => p.Slug)
                .IsRequired()
                .HasMaxLength(250);

            builder.Property(p => p.MetaTitle)
                .HasMaxLength(60);

            builder.Property(p => p.MetaDescription)
                .HasMaxLength(160);

            builder.Property(p => p.Summary)
                .HasMaxLength(1000);

            builder.Property(p => p.ActualPrice)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(p => p.DiscountPrice)
                .HasPrecision(18, 2);

            builder.Property(p => p.Brand)
                .HasMaxLength(100);

            builder.Property(p => p.Gtin)
                .HasMaxLength(50);

            builder.Property(p => p.Color)
                .HasMaxLength(50);

            builder.Property(p => p.DeletedByUserId)
                .HasMaxLength(450);

            builder.Property(p => p.CreatedByUserId)
                .HasMaxLength(450);

            builder.Property(p => p.UpdatedByUserId)
                .HasMaxLength(450);

            builder.HasQueryFilter(p => !p.IsDeleted);

            builder.HasIndex(p => p.Slug)
                .IsUnique()
                .HasDatabaseName("IX_Products_Slug");

            builder.HasIndex(p => p.CategoryId)
                .HasDatabaseName("IX_Products_CategoryId");

            builder.HasIndex(p => p.IsDeleted)
                .HasFilter("\"IsDeleted\" = false")
                .HasDatabaseName("IX_Products_IsDeleted_Filtered");

            builder.HasIndex(p => new { p.CategoryId, p.IsDeleted, p.ShowCase })
                .HasDatabaseName("IX_Products_Category_Active_ShowCase");

            builder.HasIndex(p => p.CreatedAt)
                .HasDatabaseName("IX_Products_CreatedAt_Desc");

            builder.HasIndex(p => p.Brand)
                .HasDatabaseName("IX_Products_Brand");

            builder.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(c => c.UserReviews)
                .WithOne(sc => sc.Product)
                .HasForeignKey(sc => sc.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.Images)
                .WithOne(pi => pi.Product)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasGeneratedTsVectorColumn(
                p => p.SearchVector,
                "turkish",
                p => new { p.ProductName, p.Summary, p.Brand })
            .HasIndex(p => p.SearchVector)
            .HasMethod("GIN");

            builder.HasGeneratedTsVectorColumn(
                p => p.SearchVector,
                "turkish",
                p => new { p.ProductName, p.Brand, p.Summary, p.LongDescription, p.MetaTitle, p.MetaDescription, p.Gtin })
                .HasIndex(p => p.SearchVector)
                .HasMethod("GIN"); // Hızlı tam metin arama indeksi

            builder.Ignore(p => p.Discount);
        }
    }
}
