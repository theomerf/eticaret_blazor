namespace Application.Common.Models
{
    public class RegisterResult
    {
        public bool Succeeded { get; private set; }
        public bool CaptchaFailed { get; private set; }
        public IEnumerable<string> Errors { get; private set; } = Array.Empty<string>();

        public string? UserId { get; private set; }
        public string? Email { get; private set; }
        public string? ConfirmationLink { get; private set; }

        public static RegisterResult Success(string userId, string email, string confirmationLink) => new()
        {
            Succeeded = true,
            UserId = userId,
            Email = email,
            ConfirmationLink = confirmationLink
        };

        public static RegisterResult Failure_CaptchaFailed() => new()
        {
            CaptchaFailed = true
        };

        public static RegisterResult Failure_ValidationErrors(IEnumerable<string> errors) => new()
        {
            Succeeded = false,
            Errors = errors
        };
    }
}
