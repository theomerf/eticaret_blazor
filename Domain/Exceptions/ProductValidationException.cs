namespace Domain.Exceptions
{
    public class ProductValidationException : Exception
    {
        public ProductValidationException(string message) : base(message)
        {
        }

        public ProductValidationException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}
