using Application.DTOs;

namespace Application.Common.Models
{
    public class RegisterRequest
    {
        public string IpAddress { get; set; } = null!;
        public string? CaptchaToken { get; set; }
        public bool SkipCaptcha { get; set; } = false;
        public RegisterDto RegisterDto { get; set; } = null!;
        public string ConfirmationLinkTemplate { get; set; } = null!;
    }
}
