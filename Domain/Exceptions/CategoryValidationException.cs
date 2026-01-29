namespace Domain.Exceptions
{
    public class CategoryValidationException : Exception
    {
        public CategoryValidationException(string message) : base(message)
        {
        }

        public CategoryValidationException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}
