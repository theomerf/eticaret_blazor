namespace Application.DTOs
{
    public class AuthRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public bool RememberMe { get; set; }
        public string IpAddress { get; set; } = null!;
        public string? CaptchaToken { get; set; }
        public bool SkipCaptcha { get; set; } = false;
    }
}
