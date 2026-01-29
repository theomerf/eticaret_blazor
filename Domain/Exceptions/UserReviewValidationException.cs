namespace Domain.Exceptions
{
    public class UserReviewValidationException : Exception
    {
        public UserReviewValidationException(string message) : base(message)
        {
        }

        public UserReviewValidationException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}
