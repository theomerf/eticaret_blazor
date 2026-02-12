using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public class CategoryVariantAttributeConfig : IEntityTypeConfiguration<CategoryVariantAttribute>
    {
        public void Configure(EntityTypeBuilder<CategoryVariantAttribute> builder)
        {
            builder.HasKey(cva => cva.VariantAttributeId);

            builder.Property(cva => cva.Key)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(cva => cva.DisplayName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(cva => cva.Type)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(c => c.DeletedByUserId)
                .HasMaxLength(450);

            builder.HasQueryFilter(p => !p.IsDeleted);

            builder.HasIndex(c => c.IsDeleted)
                .HasFilter("\"IsDeleted\" = false")
                .HasDatabaseName("IX_CategoryVariantAttributes_IsDeleted_Filtered");

            builder.HasIndex(x => x.CategoryId)
                .HasDatabaseName("IX_CategoryVariantAttributes_CategoryId");

            builder.HasIndex(x => new { x.CategoryId, x.IsVariantDefiner, x.SortOrder })
                .HasDatabaseName("IX_CategoryVariantAttributes_CategoryId_IsVariantDefiner_SortOrder");

            builder.HasIndex(x => new { x.CategoryId, x.IsTechnicalSpec, x.SortOrder })
                .HasDatabaseName("IX_CategoryVariantAttributes_CategoryId_IsTechnicalSpec_SortOrder");

            builder.HasIndex(x => new { x.CategoryId, x.Key })
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false")
                .HasDatabaseName("UX_CategoryVariantAttributes_CategoryId_Key_Active");

            builder.HasOne(cva => cva.Category)
                .WithMany(c => c.VariantAttributes)
                .HasForeignKey(cva => cva.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
