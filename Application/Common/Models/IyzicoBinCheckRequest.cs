namespace Application.Common.Models
{
    public class IyzicoBinCheckRequest
    {
        public string Locale { get; set; } = "tr";
        public string ConversationId { get; set; } = null!;
        public string BinNumber { get; set; } = null!;
    }
}
