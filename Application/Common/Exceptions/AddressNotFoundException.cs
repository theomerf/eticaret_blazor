using Domain.Exceptions;

namespace Application.Common.Exceptions
{
    internal class AddressNotFoundException : NotFoundException
    {
        public AddressNotFoundException(int id) : base($"{id} id'sine sahip adres bulunamadı.")
        {
        }
    }
}
