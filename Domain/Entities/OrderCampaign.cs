using Domain.Exceptions;

namespace Domain.Entities
{
    public class OrderCampaign
    {
        public int OrderCampaignId { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public int CampaignId { get; set; }
        public string CampaignName { get; set; } = null!;

        public CampaignType CampaignType { get; set; }
        public CampaignScope CampaignScope { get; set; }

        public decimal CampaignValue { get; set; }
        public decimal DiscountAmount { get; set; }

        public int Priority { get; set; }

        #region Validation Methods

        public void ValidateForCreation()
        {
            if (CampaignId <= 0)
            {
                throw new CampaignValidationException("Geçerli bir kampanya seçilmelidir.");
            }

            if (string.IsNullOrWhiteSpace(CampaignName))
            {
                throw new CampaignValidationException("Kampanya adı boş olamaz.");
            }

            if (CampaignValue <= 0)
            {
                throw new CampaignValidationException("Kampanya değeri 0'dan büyük olmalıdır.");
            }

            if (DiscountAmount < 0)
            {
                throw new CampaignValidationException("İndirim miktarı 0'dan küçük olamaz.");
            }

            if (Priority < 0)
            {
                throw new CampaignValidationException("Öncelik 0'dan küçük olamaz.");
            }
        }

        #endregion
    }
}
