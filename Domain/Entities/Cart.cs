namespace Domain.Entities
{
    public class Cart
    {
        public int CartId { get; set; }
        public string? UserId { get; set; }

        public int Version { get; private set; } = 1;
        public List<CartLine> Lines { get; set; } = new();

        #region Validation Methods

        public virtual CartOperationResult SetQuantity(int productId, int variantId, int newQuantity)
        {
            if (newQuantity < 0)
            {
                return CartOperationResult.Failure("Miktar negatif olamaz");
            }

            var line = Lines.FirstOrDefault(l => l.ProductId == productId && l.ProductVariantId == variantId);

            if (line == null)
            {
                return CartOperationResult.Failure("Ürün sepette bulunamadı");
            }

            if (newQuantity == 0)
            {
                Lines.Remove(line);
                Touch();
                return CartOperationResult.Success("Ürün sepetten kaldırıldı");
            }

            if (line.Quantity == newQuantity)
            {
                return CartOperationResult.Success("Miktar zaten aynı", newQuantity, newQuantity);
            }

            var oldQuantity = line.Quantity;
            line.Quantity = newQuantity;
            Touch();

            return CartOperationResult.Success(
                "Miktar güncellendi",
                oldQuantity,
                newQuantity
            );
        }

        public virtual CartOperationResult AddOrUpdateItem(Product product, ProductVariant variant, int quantity)
        {
            if (quantity <= 0)
            {
                return CartOperationResult.Failure("Miktar 0'dan büyük olmalıdır");
            }

            // Find existing cart line for this specific variant
            var line = Lines.FirstOrDefault(l => l.ProductId.Equals(product.ProductId) && l.ProductVariantId == variant.ProductVariantId);

            if (line == null)
            {
                Lines.Add(new CartLine
                {
                    ProductId = product.ProductId,
                    ProductVariantId = variant.ProductVariantId,
                    ProductName = product.ProductName,
                    ImageUrl = product.Variants.FirstOrDefault(v => v.ProductVariantId == variant.ProductVariantId)?.Images?.FirstOrDefault()?.ImageUrl,
                    Price = variant.Price,
                    DiscountPrice = variant.DiscountPrice,
                    SelectedColor = variant.Color,
                    SelectedSize = variant.Size,
                    SpecificationsJson = variant.VariantSpecificationsJson,
                    CartId = CartId,
                    Cart = this,
                    Quantity = quantity
                });

                Touch();
                return CartOperationResult.Success("Ürün sepete eklendi", 0, quantity);
            }


            if (line.Quantity == quantity)
            {
                return CartOperationResult.Success("Miktar zaten aynı", quantity, quantity);
            }

            var oldQuantity = line.Quantity;
            line.Quantity = quantity;

            Touch();
            return CartOperationResult.Success("Miktar güncellendi", oldQuantity, quantity);
        }

        public virtual CartOperationResult RemoveItem(int productId, int variantId)
        {
            var line = Lines.FirstOrDefault(l => l.ProductId == productId && l.ProductVariantId == variantId);

            if (line == null)
            {
                return CartOperationResult.Success("Ürün zaten sepette yok");
            }

            Lines.Remove(line);

            Touch();
            return CartOperationResult.Success("Ürün sepetten kaldırıldı");
        }

        public virtual void Clear() 
        {
            if (Lines.Count == 0)
                return;

            Lines.Clear();
            Touch();
        }


        public decimal ComputeTotalValue() =>
            Lines.Sum(e => e.Price * e.Quantity);

        public decimal ComputeTotalDiscountValue() =>
            Lines.Sum(e => (e.DiscountPrice ?? 0) * e.Quantity);

        private void Touch()
        {
            Version++;
        }

        #endregion

    }

    public class CartOperationResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? OldQuantity { get; set; }
        public int? NewQuantity { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }

        public static CartOperationResult Success(
            string message,
            int? oldQty = null,
            int? newQty = null)
        {
            return new CartOperationResult
            {
                IsSuccess = true,
                Message = message,
                OldQuantity = oldQty,
                NewQuantity = newQty
            };
        }

        public static CartOperationResult Failure(string message)
        {
            return new CartOperationResult
            {
                IsSuccess = false,
                Message = message
            };
        }
    }
}