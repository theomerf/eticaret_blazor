namespace Application.Services.Interfaces
{
    public interface ICaptchaService
    {
        Task<bool> ValidateAsync(string? token);
        Task<CaptchaResponse> VerifyAsync(string? token);
    }

    public class CaptchaResponse
    {
        public bool Success { get; set; }
        public double Score { get; set; }
        public string Action { get; set; } = string.Empty;
        public DateTime ChallengeTimestamp { get; set; }
        public string Hostname { get; set; } = string.Empty;
        public string[] ErrorCodes { get; set; } = Array.Empty<string>();
    }
}
