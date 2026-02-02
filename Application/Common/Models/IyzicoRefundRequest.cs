namespace Application.Common.Models
{
    /// <summary>
    /// Request model for Iyzico Refund API
    /// </summary>
    public class IyzicoRefundRequest
    {
        public string PaymentTransactionId { get; set; } = null!;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "TRY";
        public string Ip { get; set; } = null!;
        
        /// <summary>
        /// Refund reason: OTHER, DOUBLE_PAYMENT, BUYER_REQUEST, FRAUD
        /// </summary>
        public string Reason { get; set; } = "BUYER_REQUEST";
        
        public string? Description { get; set; }
    }
}
