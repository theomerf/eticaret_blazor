using Domain.Exceptions;

namespace Application.Common.Exceptions
{
    internal class CampaignNotFoundException : NotFoundException
    {
        public CampaignNotFoundException(int id) : base($"{id} id'sine sahip kampanya bulunamadı.")
        {
        }
    }
}
