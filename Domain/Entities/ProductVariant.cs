using Domain.Exceptions;

namespace Domain.Entities
{
    public class ProductVariant : AuditableEntity
    {
        public int ProductVariantId { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public ICollection<ProductImage>? Images { get; set; }

        public string? Color { get; set; }
        public string? Size { get; set; }
        public decimal? WeightOverride { get; set; }
        public decimal? LengthOverride { get; set; }
        public decimal? WidthOverride { get; set; }
        public decimal? HeightOverride { get; set; }
        public string? VariantSpecificationsJson { get; set; }
        public string? CombinationKey { get; set; }

        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int Stock { get; set; }
        public string? Gtin { get; set; }
        public string? Sku { get; set; }
        
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; } = false;

        public int Discount => DiscountPrice.HasValue && DiscountPrice.Value > 0
           ? (int)((1 - DiscountPrice.Value / Price) * 100) : 0;

        #region Validation Methods

        public void ValidateStock()
        {
            if (Stock < 0)
            {
                throw new ProductValidationException("Stok miktarı 0'dan küçük olamaz.");
            }
        }

        public void DecreaseStock(int quantity)
        {
            if (quantity <= 0)
            {
                throw new ProductValidationException("Azaltılacak miktar 0'dan büyük olmalıdır.");
            }

            if (Stock < quantity)
            {
                throw new InsufficientStockException(ProductVariantId, quantity, Stock);
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
