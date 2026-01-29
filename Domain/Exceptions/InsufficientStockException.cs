namespace Domain.Exceptions
{
    public class InsufficientStockException : Exception
    {
        public int ProductId { get; }
        public int RequestedQuantity { get; }
        public int AvailableStock { get; }

        public InsufficientStockException(int productId, int requestedQuantity, int availableStock)
            : base($"Yetersiz stok. Ürün ID: {productId}, İstenen: {requestedQuantity}, Mevcut: {availableStock}")
        {
            ProductId = productId;
            RequestedQuantity = requestedQuantity;
            AvailableStock = availableStock;
        }
    }
}
