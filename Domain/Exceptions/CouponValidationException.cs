namespace Domain.Exceptions
{
    public class CouponValidationException : Exception
    {
        public CouponValidationException(string message) : base(message)
        {
        }

        public CouponValidationException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}
