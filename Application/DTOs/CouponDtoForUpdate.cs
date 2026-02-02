using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public record CouponDtoForUpdate : CouponDtoForCreation
    {
        [Required(ErrorMessage = "Kupon ID gereklidir.")]
        public int CouponId { get; set; }
    }
}
