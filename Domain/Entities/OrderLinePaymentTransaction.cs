namespace Domain.Entities
{
    public class OrderLinePaymentTransaction : SoftDeletableEntity
    {
        public int OrderLinePaymentTransactionId { get; set; }
        
        public int OrderLineId { get; set; }
        public OrderLine OrderLine { get; set; } = null!;
        
        public string ItemId { get; set; } = null!;
        public string PaymentTransactionId { get; set; } = null!;
        public int TransactionStatus { get; set; } // 0=waiting, 1=approved, 2=completed
        public decimal Price { get; set; }
        public decimal PaidPrice { get; set; }
        
        public bool IsRefunded { get; set; }
        public string? RefundTransactionId { get; set; }
        public DateTime? RefundedAt { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public OrderLinePaymentTransaction()
        {
            CreatedAt = DateTime.UtcNow;
            IsRefunded = false;
        }
    }
}
