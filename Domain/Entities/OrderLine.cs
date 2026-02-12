using Domain.Exceptions;

namespace Domain.Entities
{
    public class OrderLine : SoftDeletableEntity
    {
        public int OrderLineId { get; set; }
        
        public int OrderId { get; set; }
        public Order? Order { get; set; }
        
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        
        public int ProductVariantId { get; set; }
        public ProductVariant? ProductVariant { get; set; }
        
        public string ProductName { get; set; } = null!;
        public string CategoryName { get; set; } = null!;
        public string? SubCategoryName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public string? ImageUrl { get; set; }
        
        public string? VariantColor { get; set; }
        public string? VariantSize { get; set; }
        public string? SpecificationsJson { get; set; }
        
        public decimal LineTotal => (DiscountPrice ?? Price) * Quantity;

        #region Validation Methods

        public void ValidateForCreation()
        {
            ValidateProductInfo();
            ValidateQuantity();
            ValidatePricing();
        }

        private void ValidateProductInfo()
        {
            if (ProductId <= 0)
            {
                throw new OrderValidationException("Geçerli bir ürün seçilmelidir.");
            }

            if (string.IsNullOrWhiteSpace(ProductName))
            {
                throw new OrderValidationException("Ürün adı boş olamaz.");
            }

            if (ProductName.Length < 2 || ProductName.Length > 200)
            {
                throw new OrderValidationException("Ürün adı 2-200 karakter arasında olmalıdır.");
            }
        }

        public void ValidateQuantity()
        {
            if (Quantity <= 0)
            {
                throw new OrderValidationException("Miktar 0'dan büyük olmalıdır.");
            }

            if (Quantity > 1000)
            {
                throw new OrderValidationException("Miktar 1000'den fazla olamaz.");
            }
        }

        public void ValidatePricing()
        {
            if (Price <= 0)
            {
                throw new OrderValidationException("Ürün fiyatı 0'dan büyük olmalıdır.");
            }

            if (DiscountPrice.HasValue && DiscountPrice.Value < 0)
            {
                throw new OrderValidationException("İndirimli fiyat 0'dan küçük olamaz.");
            }

            if (DiscountPrice.HasValue && DiscountPrice.Value >= Price)
            {
                throw new OrderValidationException("İndirimli fiyat normal fiyattan küçük olmalıdır.");
            }
        }

        #endregion

        #region Helper Methods

        public void UpdateQuantity(int newQuantity)
        {
            if (newQuantity <= 0)
            {
                throw new OrderValidationException("Miktar 0'dan büyük olmalıdır.");
            }

            if (newQuantity > 1000)
            {
                throw new OrderValidationException("Miktar 1000'den fazla olamaz.");
            }

            Quantity = newQuantity;
        }

        public decimal RecalculateLineTotal()
        {
            return (DiscountPrice ?? Price) * Quantity;
        }

        #endregion
    }
}