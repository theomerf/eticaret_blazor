namespace Application.Common.Models
{
    public class ConfirmEmailResult
    {
        public bool Succeeded { get; private set; }
        public bool UserNotFound { get; private set; }
        public bool InvalidToken { get; private set; }
        public string? UserEmail { get; private set; }
        public string? UserFirstName { get; private set; }

        public static ConfirmEmailResult Success(string userEmail, string userFirstName) => new()
        {
            Succeeded = true,
            UserEmail = userEmail,
            UserFirstName = userFirstName
        };

        public static ConfirmEmailResult Failure_UserNotFound() => new()
        {
            UserNotFound = true
        };

        public static ConfirmEmailResult Failure_InvalidToken() => new()
        {
            InvalidToken = true
        };
    }
}
