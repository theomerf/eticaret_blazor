using Domain.Exceptions;

namespace Domain.Entities
{
    public class Product : AuditableEntity
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;

        public string Slug { get; set; } = null!;
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }

        public string? Summary { get; set; }
        public string? LongDescription { get; set; }

        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        public ICollection<ProductImage>? Images { get; set; }

        public decimal ActualPrice { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int Stock { get; set; }
        public int Discount => DiscountPrice.HasValue && DiscountPrice.Value > 0
           ? (int)((1 - DiscountPrice.Value / ActualPrice) * 100) : 0;

        public double AverageRating { get; set; } = 0;
        public int ReviewCount { get; set; } = 0;
        public string? Brand { get; set; }

        public string? Gtin { get; set; }
        public string? Color { get; set; } 

        public bool ShowCase { get; set; } = false;
        public ICollection<UserReview>? UserReviews { get; set; }

        public void ValidatePrice()
        {
            if (ActualPrice <= 0)
            {
                throw new InvalidPriceException("Ürün fiyatı 0'dan büyük olmalıdır.");
            }

            if (DiscountPrice.HasValue && DiscountPrice.Value < 0)
            {
                throw new InvalidPriceException("İndirimli fiyat 0'dan küçük olamaz.");
            }

            if (DiscountPrice.HasValue && DiscountPrice.Value >= ActualPrice)
            {
                throw new InvalidPriceException(ActualPrice, DiscountPrice);
            }
        }

        public void ValidateStock()
        {
            if (Stock < 0)
            {
                throw new ProductValidationException("Stok miktarı 0'dan küçük olamaz.");
            }
        }

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

            ValidatePrice();
            ValidateStock();
        }

        public void DecreaseStock(int quantity)
        {
            if (quantity <= 0)
            {
                throw new ProductValidationException("Azaltılacak miktar 0'dan büyük olmalıdır.");
            }

            if (Stock < quantity)
            {
                throw new InsufficientStockException(ProductId, quantity, Stock);
            }

            Stock -= quantity;
        }

        public void IncreaseStock(int quantity)
        {
            if (quantity <= 0)
            {
                throw new ProductValidationException("Artırılacak miktar 0'dan büyük olmalıdır.");
            }

            Stock += quantity;
        }

        public void SoftDelete(string deletedByUserId)
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
            DeletedByUserId = deletedByUserId;
        }
    }
}

