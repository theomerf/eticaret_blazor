using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    /// <summary>
    /// DTO for updating coupons
    /// </summary>
    public record CouponDtoForUpdate : CouponDtoForCreation
    {
        [Required(ErrorMessage = "Kupon ID gereklidir.")]
        public int CouponId { get; set; }
    }
}
