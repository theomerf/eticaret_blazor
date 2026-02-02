namespace Application.Common.Models
{
    public class IyzicoCheckoutFormRetrieveResponse
    {
        public string Status { get; set; } = null!;
        public string Locale { get; set; } = null!;
        public long SystemTime { get; set; }
        public string ConversationId { get; set; } = null!;
        public decimal Price { get; set; }
        public decimal PaidPrice { get; set; }
        public int Installment { get; set; }
        public string PaymentId { get; set; } = null!;
        public int FraudStatus { get; set; } // Sadece 1 için kargo onaylanır
        public decimal MerchantCommissionRate { get; set; }
        public decimal MerchantCommissionRateAmount { get; set; }
        public decimal IyziCommissionRateAmount { get; set; }
        public decimal IyziCommissionFee { get; set; }
        public string CardType { get; set; } = null!;
        public string CardAssociation { get; set; } = null!;
        public string CardFamily { get; set; } = null!;
        public string BinNumber { get; set; } = null!;
        public string LastFourDigits { get; set; } = null!;
        public string BasketId { get; set; } = null!;
        public string Currency { get; set; } = null!;
        public List<ItemTransaction> ItemTransactions { get; set; } = new List<ItemTransaction>();
        public string AuthCode { get; set; } = null!;
        public string Phase { get; set; } = null!;
        public string HostReference { get; set; } = null!;
        public string Signature { get; set; } = null!;
        public string Token { get; set; } = null!;
        public string CallbackUrl { get; set; } = null!;
        public string PaymentStatus { get; set; } = null!;

        public string ErrorCode { get; set; } = null!;
        public string ErrorMessage { get; set; } = null!;
    }
}
