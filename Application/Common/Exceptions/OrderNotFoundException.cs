using Domain.Exceptions;

namespace Application.Common.Exceptions
{
    public class OrderNotFoundException : NotFoundException
    {
        public OrderNotFoundException(int id) : base($"{id} id'sine sahip sipariş bulunamadı.")
        {
        }

        public OrderNotFoundException(string orderNumber) : base($"{orderNumber} numaralı sipariş bulunamadı.")
        {
        }
    }
}
