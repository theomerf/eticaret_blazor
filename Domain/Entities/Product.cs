using Domain.Exceptions;
using NpgsqlTypes;

namespace Domain.Entities
{
    public class Product : AuditableEntity
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;

        public string Slug { get; set; } = null!;
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }

        public string Summary { get; set; } = null!;
        public string? LongDescription { get; set; }

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public int TotalStock => Variants?.Sum(v => v.Stock) ?? 0;
        
        public decimal MinPrice => Variants?.Count != 0 
            ? Variants!.Min(v => v.DiscountPrice ?? v.Price) 
            : 0;
        public decimal MaxPrice => Variants?.Count != 0 
            ? Variants!.Max(v => v.DiscountPrice ?? v.Price) 
            : 0;

        public double AverageRating { get; set; } = 0;
        public int ReviewCount { get; set; } = 0;
        public string? Brand { get; set; }

        public ICollection<ProductVariant> Variants { get; set; } = [];

        public decimal? DefaultWeight { get; set; }
        public decimal? DefaultLength { get; set; }
        public decimal? DefaultWidth { get; set; }
        public decimal? DefaultHeight { get; set; }
        public string? ManufacturingCountry { get; set; }
        public string? WarrantyInfo { get; set; }
        public string? SpecificationsJson { get; set; }

        public bool ShowCase { get; set; } = false;
        public ICollection<UserReview>? UserReviews { get; set; }

        public NpgsqlTsVector SearchVector { get; set; } = null!;

        #region Validation Methods

        public void ValidateForCreation()
        {
            if (string.IsNullOrWhiteSpace(ProductName))
            {
                throw new ProductValidationException("Ürün adı boş olamaz.");
            }

            if (ProductName.Length < 2 || ProductName.Length > 200)
            {
                throw new ProductValidationException("Ürün adı 2-200 karakter arasında olmalıdır.");
            }

            if (CategoryId <= 0)
            {
                throw new ProductValidationException("Geçerli bir kategori seçilmelidir.");
            }

            if (Variants == null || !Variants.Any())
            {
                throw new ProductValidationException("Ürünün en az bir varyantı olmalıdır.");
            }

            foreach (var variant in Variants)
            {
                if (variant.Price <= 0)
                {
                    throw new InvalidPriceException("Varyant fiyatı 0'dan büyük olmalıdır.");
                }

                if (variant.DiscountPrice.HasValue && variant.DiscountPrice.Value < 0)
                {
                    throw new InvalidPriceException("İndirimli fiyat 0'dan küçük olamaz.");
                }

                if (variant.DiscountPrice.HasValue && variant.DiscountPrice.Value >= variant.Price)
                {
                    throw new InvalidPriceException(variant.Price, variant.DiscountPrice);
                }
            }
        }

        #endregion

        #region Business Logic Methods

        public void SoftDelete(string deletedByUserId)
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
            DeletedByUserId = deletedByUserId;
        }

        #endregion
    }
}

