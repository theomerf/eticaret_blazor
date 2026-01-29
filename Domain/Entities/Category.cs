using Domain.Exceptions;

namespace Domain.Entities
{
    public class Category : AuditableEntity
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;

        public string Slug { get; set; } = null!;
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }

        public string? Description { get; set; }
        public string? IconUrl { get; set; }

        public int? ParentId { get; set; }
        public Category? ParentCategory { get; set; }
        public ICollection<Category>? ChildCategories { get; set; }
        public ICollection<Product>? Products { get; set; }

        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;
        public bool IsFeatured { get; set; } = false;

        public void ValidateForCreation()
        {
            if (string.IsNullOrWhiteSpace(CategoryName))
            {
                throw new CategoryValidationException("Kategori adı boş olamaz.");
            }

            if (CategoryName.Length < 2 || CategoryName.Length > 200)
            {
                throw new CategoryValidationException("Kategori adı 2-200 karakter arasında olmalıdır.");
            }

            if (DisplayOrder < 0 || DisplayOrder > 999)
            {
                throw new CategoryValidationException("Sıralama 0-999 arasında olmalıdır.");
            }

            if (IconUrl != null && IconUrl.Length > 500)
            {
                throw new CategoryValidationException("Simge URL'si en fazla 500 karakter olabilir.");
            }

            if (IconUrl != null && !Uri.TryCreate(IconUrl, UriKind.Absolute, out _))
            {
                throw new CategoryValidationException("Geçerli bir simge URL'si giriniz.");
            }
        }

        public void ValidateForUpdate()
        {
            ValidateForCreation();

            if (ParentId.HasValue && ParentId.Value == CategoryId)
            {
                throw new CategoryValidationException("Kategori kendi üst kategorisi olamaz.");
            }
        }

        public bool HasCircularReference(int targetParentId, IEnumerable<Category> allCategories)
        {
            if (targetParentId == CategoryId)
                return true;

            var parent = allCategories.FirstOrDefault(c => c.CategoryId == targetParentId);
            if (parent == null)
                return false;

            if (!parent.ParentId.HasValue)
                return false;

            return HasCircularReference(parent.ParentId.Value, allCategories);
        }

        public void SoftDelete(string deletedByUserId)
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
            DeletedByUserId = deletedByUserId;
        }
    }
}
