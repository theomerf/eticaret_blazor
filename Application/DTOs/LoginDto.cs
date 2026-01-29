using System.ComponentModel.DataAnnotations;

namespace ETicaret.Models
{
    public class LoginDto
    {
        [Required(ErrorMessage = "E-Posta gereklidir.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [StringLength(256, ErrorMessage = "E-posta en fazla 256 karakter olabilir.")]
        public string Email { get; set; } = null!;
        [Required(ErrorMessage = "Şifre gereklidir.")]
        [MaxLength(100, ErrorMessage = "Şifre en fazla 100 karakter olabilir.")]
        [MinLength(8, ErrorMessage = "Şifre en az 8 karakter olabilir.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;
        public bool RememberMe { get; set; } = false;
        public string? CaptchaToken { get; set; }
    }
}
