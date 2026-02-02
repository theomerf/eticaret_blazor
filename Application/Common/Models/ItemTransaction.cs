using System.Text.Json.Serialization;

namespace Application.Common.Models
{
    /// <summary>
    /// Item transaction details from Iyzico payment callback
    /// Used to parse itemTransactions array from IyzicoCheckoutFormRetrieveResponse
    /// </summary>
    public class ItemTransaction
    {
        [JsonPropertyName("itemId")]
        public string ItemId { get; set; } = null!;
        
        [JsonPropertyName("paymentTransactionId")]
        public string PaymentTransactionId { get; set; } = null!;
        
        [JsonPropertyName("transactionStatus")]
        public int TransactionStatus { get; set; }
        
        [JsonPropertyName("price")]
        public decimal Price { get; set; }
        
        [JsonPropertyName("paidPrice")]
        public decimal PaidPrice { get; set; }
        
        [JsonPropertyName("merchantCommissionRate")]
        public decimal MerchantCommissionRate { get; set; }
        
        [JsonPropertyName("merchantCommissionRateAmount")]
        public decimal MerchantCommissionRateAmount { get; set; }
        
        [JsonPropertyName("iyziCommissionRateAmount")]
        public decimal IyziCommissionRateAmount { get; set; }
        
        [JsonPropertyName("iyziCommissionFee")]
        public decimal IyziCommissionFee { get; set; }
        
        [JsonPropertyName("blockageRate")]
        public decimal BlockageRate { get; set; }
        
        [JsonPropertyName("subMerchantPrice")]
        public decimal SubMerchantPrice { get; set; }
        
        [JsonPropertyName("subMerchantPayoutRate")]
        public decimal SubMerchantPayoutRate { get; set; }
        
        [JsonPropertyName("subMerchantPayoutAmount")]
        public decimal SubMerchantPayoutAmount { get; set; }
        
        [JsonPropertyName("merchantPayoutAmount")]
        public decimal MerchantPayoutAmount { get; set; }
    }
}
