using System.Security.Claims;

namespace Application.Common.Models
{
    public class ImpersonationResult
    {
        public bool Succeeded { get; private set; }
        public IList<Claim>? Claims { get; private set; }
        public string? ErrorMessage { get; private set; }

        public bool CannotImpersonateSelf { get; private set; }
        public bool UserNotFound { get; private set; }
        public bool UserDeleted { get; private set; }
        public bool UserInactive { get; private set; }

        public static ImpersonationResult Success(IList<Claim> claims) => new()
        {
            Succeeded = true,
            Claims = claims
        };

        public static ImpersonationResult Failure_CannotImpersonateSelf() => new()
        {
            CannotImpersonateSelf = true,
            ErrorMessage = "Kendi hesabınıza geçiş yapamazsınız."
        };

        public static ImpersonationResult Failure_UserNotFound() => new()
        {
            UserNotFound = true,
            ErrorMessage = "Kullanıcı bulunamadı."
        };

        public static ImpersonationResult Failure_UserDeleted() => new()
        {
            UserDeleted = true,
            ErrorMessage = "Bu kullanıcı silinmiş."
        };

        public static ImpersonationResult Failure_UserInactive() => new()
        {
            UserInactive = true,
            ErrorMessage = "Pasif kullanıcı olarak oturum açılamaz."
        };
    }
}