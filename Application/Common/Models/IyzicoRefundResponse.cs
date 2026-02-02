namespace Application.Common.Models
{
    /// <summary>
    /// Response model from Iyzico Refund API
    /// </summary>
    public class IyzicoRefundResponse
    {
        public string Status { get; set; } = null!;
        public string Locale { get; set; } = null!;
        public long SystemTime { get; set; }
        public string ConversationId { get; set; } = null!;
        public string PaymentId { get; set; } = null!;
        public string PaymentTransactionId { get; set; } = null!;
        public decimal Price { get; set; }
        public string Currency { get; set; } = null!;
        
        // Error fields
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ErrorGroup { get; set; }
    }
}
