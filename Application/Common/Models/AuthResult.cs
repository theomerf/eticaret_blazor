namespace Application.Common.Models
{
    public class AuthResult
    {
        public bool Succeeded { get; private set; }
        public bool IsLockedOut { get; private set; }
        public bool RequiresEmailConfirmation { get; private set; }
        public bool IsDeleted { get; private set; }
        public bool IpBlocked { get; private set; }
        public bool CaptchaFailed { get; private set; }
        public bool InvalidCredentials { get; private set; }
        public int? RemainingLockoutMinutes { get; private set; }

        public string? UserId { get; private set; }
        public string? UserName { get; private set; }
        public string? Email { get; private set; }

        public static AuthResult Success(string userId, string userName, string email) => new()
        {
            Succeeded = true,
            UserId = userId,
            UserName = userName,
            Email = email
        };

        public static AuthResult Failure_IpBlocked() => new() { IpBlocked = true };
        public static AuthResult Failure_CaptchaFailed() => new() { CaptchaFailed = true };
        public static AuthResult Failure_EmailNotConfirmed() => new() { RequiresEmailConfirmation = true };
        public static AuthResult Failure_AccountDeleted() => new() { IsDeleted = true };
        public static AuthResult Failure_LockedOut(int remainingMinutes) => new() { IsLockedOut = true, RemainingLockoutMinutes = remainingMinutes };
        public static AuthResult Failure_InvalidCredentials() => new() { InvalidCredentials = true };
    }
}
