using Application.Common.Validation.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public record ChangePasswordDto
    {
        [DataType(DataType.Password)]
        [Required(ErrorMessage = "Mevcut şifre gereklidir.")]
        public string Password { get; set; } = null!;

        [DataType(DataType.Password)]
        [Required(ErrorMessage = "Şifre doğrulama gereklidir.")]
        [Compare("NewPassword", ErrorMessage = "Şifreler eşleşmiyor.")]
        public string ConfirmPassword { get; set; } = null!;

        [Required(ErrorMessage = "Yeni şifre gereklidir.")]
        [MaxLength(100, ErrorMessage = "Şifre en fazla 100 karakter olabilir.")]
        [MinLength(8, ErrorMessage = "Şifre en az 8 karakter olabilir.")]
        [DataType(DataType.Password)]
        [StrongPassword]
        public string NewPassword { get; set; } = null!;
    }
}
