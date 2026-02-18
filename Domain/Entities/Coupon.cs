using Domain.Exceptions;

namespace Domain.Entities
{
    public class Coupon : AuditableEntity
    {
        public int CouponId { get; set; }
        public string Code { get; set; } = null!;

        public CouponScope Scope { get; set; }
        public CouponType Type { get; set; } // Yüzde / Sabit
        public decimal Value { get; set; } // % ise 20, sabit ise 100

        public decimal? MinOrderAmount { get; set; }
        public decimal? MaxDiscountAmount { get; set; }

        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }

        public bool IsSingleUsePerUser { get; set; }
        public int UsageLimit { get; set; }
        public int UsedCount { get; set; }

        public bool IsActive { get; set; } = true;
        public uint RowVersion { get; private set; }

        public ICollection<CouponUsage> Usages { get; set; } = new List<CouponUsage>();

        #region Validation Methods

        public void ValidateForCreation()
        {
            ValidateCode();
            ValidateValue();
            ValidateDates();
            ValidateUsageLimit();
        }

        private void ValidateCode()
        {
            if (string.IsNullOrWhiteSpace(Code))
            {
                throw new CouponValidationException("Kupon kodu boş olamaz.");
            }

            if (Code.Length < 3 || Code.Length > 50)
            {
                throw new CouponValidationException("Kupon kodu 3-50 karakter arasında olmalıdır.");
            }

            // Kupon kodu sadece harf, rakam ve tire içermeli
            if (!System.Text.RegularExpressions.Regex.IsMatch(Code, @"^[A-Z0-9\-]+$"))
            {
                throw new CouponValidationException("Kupon kodu sadece büyük harf, rakam ve tire içerebilir.");
            }
        }

        private void ValidateValue()
        {
            if (Value <= 0)
            {
                throw new CouponValidationException("Kupon değeri 0'dan büyük olmalıdır.");
            }

            if (Type == CouponType.Percentage && Value > 100)
            {
                throw new CouponValidationException("Yüzde indirimi 100'den fazla olamaz.");
            }

            if (MinOrderAmount.HasValue && MinOrderAmount.Value < 0)
            {
                throw new CouponValidationException("Minimum sipariş tutarı 0'dan küçük olamaz.");
            }

            if (MaxDiscountAmount.HasValue && MaxDiscountAmount.Value <= 0)
            {
                throw new CouponValidationException("Maksimum indirim tutarı 0'dan büyük olmalıdır.");
            }
        }

        private void ValidateDates()
        {
            if (EndsAt <= StartsAt)
            {
                throw new CouponValidationException("Bitiş tarihi başlangıç tarihinden sonra olmalıdır.");
            }
        }

        private void ValidateUsageLimit()
        {
            if (UsageLimit < 0)
            {
                throw new CouponValidationException("Kullanım limiti 0'dan küçük olamaz.");
            }

            if (UsedCount < 0)
            {
                throw new CouponValidationException("Kullanım sayısı 0'dan küçük olamaz.");
            }
        }

        #endregion

        #region Business Logic Methods

        public bool IsValid()
        {
            if (!IsActive)
                return false;

            var now = DateTime.UtcNow;
            if (now < StartsAt || now > EndsAt)
                return false;

            if (UsageLimit > 0 && UsedCount >= UsageLimit)
                return false;

            return true;
        }

        public bool CanBeUsedBy(string userId, decimal orderAmount)
        {
            if (!IsValid())
                return false;

            if (MinOrderAmount.HasValue && orderAmount < MinOrderAmount.Value)
                return false;

            if (IsSingleUsePerUser)
            {
                var userUsageCount = Usages?.Count(u => u.UserId == userId) ?? 0;
                if (userUsageCount > 0)
                    return false;
            }

            return true;
        }

        public decimal CalculateDiscount(decimal orderAmount)
        {
            if (!IsValid())
                return 0;

            if (MinOrderAmount.HasValue && orderAmount < MinOrderAmount.Value)
                return 0;

            decimal discount = 0;

            if (Type == CouponType.Percentage)
            {
                discount = orderAmount * (Value / 100);
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

            // Discount cannot exceed order amount
            if (discount > orderAmount)
            {
                discount = orderAmount;
            }

            return discount;
        }

        public void IncrementUsage()
        {
            if (UsageLimit > 0 && UsedCount >= UsageLimit)
            {
                throw new CouponValidationException("Kupon kullanım limitine ulaşmış.");
            }

            UsedCount++;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public void Activate()
        {
            IsActive = true;
        }

        public void SoftDelete(string deletedByUserId)
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
            DeletedByUserId = deletedByUserId;
        }

        #endregion
    }

    public enum CouponScope
    {
        OrderTotal,     // Sipariş toplamına uygulanır
        Product,        // Belirli ürünlere uygulanır
        Category        // Belirli kategorilere uygulanır
    }

    public enum CouponType
    {
        Percentage,     // Yüzde indirim
        FixedAmount     // Sabit tutar indirim
    }
}
