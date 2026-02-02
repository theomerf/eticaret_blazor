namespace Application.Common.Models
{
    public class IyzicoCheckoutFormInitResponse
    {
        public string Status { get; set; } = null!;
        public string Locale { get; set; } = null!;
        public long SystemTime { get; set; }
        public string ConversationId { get; set; } = null!;
        public string Token { get; set; } = null!;
        public string CheckoutFormContent { get; set; } = null!;
        public string PaymentPageUrl { get; set; } = null!;
        public string Signature { get; set; } = null!;

        public string ErrorCode { get; set; } = null!;
        public string ErrorMessage { get; set; } = null!;
    }
}
