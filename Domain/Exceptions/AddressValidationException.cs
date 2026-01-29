using Domain.Exceptions;

namespace Domain.Exceptions
{
    public class AddressValidationException : Exception
    {
        public AddressValidationException(string message) : base(message)
        {
        }

        public AddressValidationException(string message, Exception innerException)
        : base(message, innerException)
        {
        }
    }
}
