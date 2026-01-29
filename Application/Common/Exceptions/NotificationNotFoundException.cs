using Domain.Exceptions;

namespace Application.Common.Exceptions
{
    internal class NotificationNotFoundException : NotFoundException
    {
        public NotificationNotFoundException(int id) : base($"{id} id'sine sahip bildirim bulunamadı.")
        {
        }
    }
}
