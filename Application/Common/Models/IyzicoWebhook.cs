using System.Text.Json.Serialization;

namespace Application.Common.Models
{
    public class IyzicoWebhook
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = null!;

        [JsonPropertyName("paymentId")]
        public long PaymentId { get; set; }

        [JsonPropertyName("paymentConversationId")]
        public string? PaymentConversationId { get; set; }

        [JsonPropertyName("conversationId")]
        public string? ConversationId { get; set; }

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("paidPrice")]
        public decimal PaidPrice { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("iyziReferenceCode")]
        public string? IyziReferenceCode { get; set; }

        [JsonPropertyName("iyziEventType")]
        public string? IyziEventType { get; set; }

        [JsonPropertyName("iyziEventTime")]
        public long IyziEventTime { get; set; }

        public bool IsSuccess => Status?.Equals("success", StringComparison.OrdinalIgnoreCase) ?? false;
    }
}
