using Domain.Exceptions;

namespace Domain.Entities
{
    public class CouponUsage
    {
        public int CouponUsageId { get; set; }
        
        public int CouponId { get; set; }
        public Coupon? Coupon { get; set; }
        
        public string UserId { get; set; } = null!;
        public User? User { get; set; }
        
        public int OrderId { get; set; }
        public Order? Order { get; set; }
        
        public DateTime UsedAt { get; set; } = DateTime.UtcNow;

        #region Validation Methods

        public void ValidateForCreation()
        {
            if (CouponId <= 0)
            {
                throw new CouponValidationException("Geçerli bir kupon seçilmelidir.");
            }

            if (string.IsNullOrWhiteSpace(UserId))
            {
                throw new CouponValidationException("Kullanıcı ID boş olamaz.");
            }

            if (OrderId <= 0)
            {
                throw new CouponValidationException("Geçerli bir sipariş seçilmelidir.");
            }
        }

        #endregion
    }
}
