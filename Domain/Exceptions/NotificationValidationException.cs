namespace Domain.Exceptions
{
    public class NotificationValidationException : Exception
    {
        public NotificationValidationException(string message) : base(message)
        {
        }

        public NotificationValidationException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}
