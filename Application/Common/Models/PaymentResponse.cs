namespace Application.Common.Models
{
    public class PaymentResponse
    {
        public bool IsSuccess { get; set; }
        public string TransactionId { get; set; } = null!;
        public string? PaymentUrl { get; set; }
        public string Message { get; set; } = null!;
        public string? ErrorCode { get; set; }
    }
}
