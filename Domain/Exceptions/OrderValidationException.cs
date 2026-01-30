namespace Domain.Exceptions
{
    public class OrderValidationException : Exception
    {
        public OrderValidationException(string message) : base(message)
        {
        }

        public OrderValidationException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}
