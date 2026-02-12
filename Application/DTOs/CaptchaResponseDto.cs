namespace Application.DTOs
{
    public class CaptchaResponseDto
    {
        public bool Success { get; set; }
        public double Score { get; set; }
        public string Action { get; set; } = string.Empty;
        public DateTime ChallengeTimestamp { get; set; }
        public string Hostname { get; set; } = string.Empty;
        public string[] ErrorCodes { get; set; } = Array.Empty<string>();
    }
}
