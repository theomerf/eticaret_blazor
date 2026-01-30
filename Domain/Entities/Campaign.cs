using Domain.Exceptions;

namespace Domain.Entities
{
    public class Campaign : AuditableEntity
    {
        public int CampaignId { get; set; }

        public string Name { get; set; } = null!;
        public string? Description { get; set; }

        public CampaignType Type { get; set; } // Yüzde / Sabit
        public decimal Value { get; set; }

        public CampaignScope Scope { get; set; } // Sipariş / Ürün / Kategori

        public decimal? MinOrderAmount { get; set; }
        public decimal? MaxDiscountAmount { get; set; }

        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }

        public bool IsActive { get; set; } = true;
        public int Priority { get; set; } // Çakışma çözümü için öncelik

        public bool IsStackable { get; set; } = false; // Diğer kampanyalarla birleştirilebilir mi

        #region Validation Methods

        public void ValidateForCreation()
        {
            ValidateName();
            ValidateValue();
            ValidateDates();
            ValidatePriority();
        }

        private void ValidateName()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                throw new CampaignValidationException("Kampanya adı boş olamaz.");
            }

            if (Name.Length < 3 || Name.Length > 200)
            {
                throw new CampaignValidationException("Kampanya adı 3-200 karakter arasında olmalıdır.");
            }
        }

        private void ValidateValue()
        {
            if (Value <= 0)
            {
                throw new CampaignValidationException("Kampanya değeri 0'dan büyük olmalıdır.");
            }

            if (Type == CampaignType.Percentage && Value > 100)
            {
                throw new CampaignValidationException("Yüzde indirimi 100'den fazla olamaz.");
            }

            if (MinOrderAmount.HasValue && MinOrderAmount.Value < 0)
            {
                throw new CampaignValidationException("Minimum sipariş tutarı 0'dan küçük olamaz.");
            }

            if (MaxDiscountAmount.HasValue && MaxDiscountAmount.Value <= 0)
            {
                throw new CampaignValidationException("Maksimum indirim tutarı 0'dan büyük olmalıdır.");
            }
        }

        private void ValidateDates()
        {
            if (EndsAt <= StartsAt)
            {
                throw new CampaignValidationException("Bitiş tarihi başlangıç tarihinden sonra olmalıdır.");
            }
        }

        private void ValidatePriority()
        {
            if (Priority < 0)
            {
                throw new CampaignValidationException("Öncelik 0'dan küçük olamaz.");
            }
        }

        #endregion

        #region Business Logic Methods

        public bool IsActiveNow()
        {
            if (!IsActive)
                return false;

            var now = DateTime.UtcNow;
            return now >= StartsAt && now <= EndsAt;
        }

        public decimal CalculateDiscount(decimal amount)
        {
            if (!IsActiveNow())
                return 0;

            if (MinOrderAmount.HasValue && amount < MinOrderAmount.Value)
                return 0;

            decimal discount = 0;

            if (Type == CampaignType.Percentage)
            {
                discount = amount * (Value / 100);
            }
            else // FixedAmount
            {
                discount = Value;
            }

            // Apply max discount limit if set
            if (MaxDiscountAmount.HasValue && discount > MaxDiscountAmount.Value)
            {
                discount = MaxDiscountAmount.Value;
            }

            // Discount cannot exceed amount
            if (discount > amount)
            {
                discount = amount;
            }

            return discount;
        }

        public void Activate()
        {
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public void SoftDelete(string deletedByUserId)
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
            DeletedByUserId = deletedByUserId;
        }

        #endregion
    }

    public enum CampaignType
    {
        Percentage,     // Yüzde indirim
        FixedAmount     // Sabit tutar indirim
    }

    public enum CampaignScope
    {
        OrderTotal,     // Sipariş toplamına uygulanır
        Product,        // Belirli ürünlere uygulanır
        Category        // Belirli kategorilere uygulanır
    }
}
