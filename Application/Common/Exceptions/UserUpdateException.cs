using Application.DTOs;
using Domain.Entities;

namespace Application.Common.Exceptions
{
    public class UserUpdateException : Exception
    {
        public OperationResult<UserDto> Result { get; }
        public UserUpdateException(OperationResult<UserDto> result) : base("Kullanıcı güncelleme işlemi başarısız oldu.")
            => Result = result;
    }
}
