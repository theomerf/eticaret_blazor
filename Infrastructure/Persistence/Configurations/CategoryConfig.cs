using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class CategoryConfig : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.HasKey(c => c.CategoryId);

            builder.Property(c => c.CategoryName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(c => c.Slug)
                .IsRequired()
                .HasMaxLength(250);

            builder.Property(p => p.MetaTitle)
                .HasMaxLength(60);

            builder.Property(p => p.MetaDescription)
                .HasMaxLength(160);

            builder.Property(c => c.Description)
                .HasMaxLength(1000);

            builder.Property(c => c.IconUrl)
                .HasMaxLength(500);

            builder.Property(c => c.DeletedByUserId)
                .HasMaxLength(450);

            builder.Property(p => p.CreatedByUserId)
                .HasMaxLength(450);

            builder.Property(p => p.UpdatedByUserId)
                .HasMaxLength(450);

            builder.HasQueryFilter(p => !p.IsDeleted);

            builder.HasIndex(c => c.Slug)
                .IsUnique()
                .HasDatabaseName("IX_Categories_Slug");

            builder.HasIndex(c => c.ParentId)
                .HasDatabaseName("IX_Categories_ParentId");

            builder.HasIndex(c => c.IsDeleted)
                .HasFilter("\"IsDeleted\" = false")
                .HasDatabaseName("IX_Categories_IsDeleted_Filtered");

            builder.HasIndex(c => new { c.IsVisible, c.DisplayOrder, c.IsDeleted })
                .HasFilter("\"IsDeleted\" = false AND \"IsVisible\" = true")
                .HasDatabaseName("IX_Categories_IsVisible_Order");

            builder.HasMany(c => c.Products)
                .WithOne(p => p.Category)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasMany(c => c.VariantAttributes)
                .WithOne(va => va.Category)
                .HasForeignKey(va => va.CategoryId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(c => c.ParentCategory)
                .WithMany(pc => pc.ChildCategories)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
