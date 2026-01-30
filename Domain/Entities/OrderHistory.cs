namespace Domain.Entities
{
    public class OrderHistory : SoftDeletableEntity
    {
        public int OrderHistoryId { get; set; }
        
        public int OrderId { get; set; }
        public Order? Order { get; set; }

        public OrderEventType EventType { get; set; }
        public string Description { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }
        
        public bool IsSystemEvent { get; set; }

        #region Helper Methods

        public static OrderHistory CreateEvent(
            int orderId, 
            OrderEventType eventType, 
            string description, 
            string? userId = null, 
            bool isSystemEvent = false)
        {
            return new OrderHistory
            {
                OrderId = orderId,
                EventType = eventType,
                Description = description,
                CreatedByUserId = userId,
                IsSystemEvent = isSystemEvent,
                CreatedAt = DateTime.UtcNow
            };
        }

        #endregion
    }

    public enum OrderEventType
    {
        OrderCreated,               // Sipariş oluşturuldu
        PaymentCompleted,           // Ödeme tamamlandı
        PaymentFailed,              // Ödeme başarısız
        OrderProcessing,            // Sipariş işleme alındı
        Shipped,                    // Kargoya verildi
        ShipmentStatusUpdated,      // Kargo durumu güncellendi
        Delivered,                  // Teslim edildi
        Cancelled,                  // İptal edildi
        Refunded,                   // İade edildi
        Returned                    // Ürün iade edildi
    }
}
