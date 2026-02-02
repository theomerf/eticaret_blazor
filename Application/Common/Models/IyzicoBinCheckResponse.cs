using System.Text.Json.Serialization;

namespace Application.Common.Models
{
    public class IyzicoBinCheckResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = null!;

        [JsonPropertyName("errorCode")]
        public string? ErrorCode { get; set; }

        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }

        [JsonPropertyName("locale")]
        public string? Locale { get; set; }

        [JsonPropertyName("systemTime")]
        public long SystemTime { get; set; }

        [JsonPropertyName("conversationId")]
        public string? ConversationId { get; set; }

        [JsonPropertyName("binNumber")]
        public string? BinNumber { get; set; }

        [JsonPropertyName("cardType")]
        public string? CardType { get; set; }

        [JsonPropertyName("cardAssociation")]
        public string? CardAssociation { get; set; }

        [JsonPropertyName("cardFamily")]
        public string? CardFamily { get; set; }

        [JsonPropertyName("bankName")]
        public string? BankName { get; set; }

        [JsonPropertyName("bankCode")]
        public long? BankCode { get; set; }

        [JsonPropertyName("commercial")]
        public int? Commercial { get; set; }
    }
}
