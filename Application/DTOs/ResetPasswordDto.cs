using Application.Common.Validation.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public record ResetPasswordDto
    {
        [Required(ErrorMessage = "Yeni şifre gereklidir.")]
        [MaxLength(100, ErrorMessage = "Şifre en fazla 100 karakter olabilir.")]
        [MinLength(8, ErrorMessage = "Şifre en az 8 karakter olabilir.")]
        [DataType(DataType.Password)]
        [StrongPassword]
        public string NewPassword { get; init; } = null!;

        [DataType(DataType.Password)]
        [Required(ErrorMessage = "Şifre doğrulama gereklidir.")]
        [Compare("Password", ErrorMessage = "Şifreler eşleşmiyor.")]
        public string ConfirmPassword { get; init; } = null!;
        [Required(ErrorMessage = "Token gereklidir.")]
        public string Token { get; init; } = null!;
    }
}
