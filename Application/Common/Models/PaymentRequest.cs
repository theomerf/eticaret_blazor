namespace Application.Common.Models
{
    public class PaymentRequest
    {
        public string OrderNumber { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "TRY";
        public string CustomerEmail { get; set; } = null!;
        public string CustomerName { get; set; } = null!;
        public string CallbackUrl { get; set; } = null!;
    }
}
