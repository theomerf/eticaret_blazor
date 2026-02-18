using Application.Common.Models;

namespace Application.Common.Exceptions
{
    public class RegistrationException : Exception
    {
        public RegisterResult Result { get; }
        public RegistrationException(RegisterResult result) : base("Kayıt işlemi başarısız oldu.")
            => Result = result;
    }
}
