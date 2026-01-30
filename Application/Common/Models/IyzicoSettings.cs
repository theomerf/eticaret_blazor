namespace Application.Common.Models
{
    public class IyzicoSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://sandbox-api.iyzipay.com";
        public bool UseMockResponses { get; set; } = true;
        public string CallbackUrl { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 30;
    }
}
