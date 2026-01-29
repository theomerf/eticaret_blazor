using Domain.Exceptions;

namespace Application.Common.Exceptions
{
    public sealed class UserNotFoundException : NotFoundException
    {
        public UserNotFoundException(string id) : base($"{id} id'sine sahip kullanıcı bulunamadı.")
        {
        }
    }
}
