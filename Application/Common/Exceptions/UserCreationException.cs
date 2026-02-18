using Application.DTOs;
using Domain.Entities;

namespace Application.Common.Exceptions
{
    public class UserCreationException : Exception
    {
        public OperationResult<UserDto> Result { get; }
        public UserCreationException(OperationResult<UserDto> result) : base("Kullanıcı oluşturma işlemi başarısız oldu.")
            => Result = result;
    }
}
