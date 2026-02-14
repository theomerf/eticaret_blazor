using Application.Common.Validation.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public record UserDtoForUpdate
    {
        [Required(ErrorMessage = "E-posta gereklidir.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [StringLength(256, ErrorMessage = "E-posta en fazla 256 karakter olabilir.")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "İsim gereklidir.")]
        [MinLength(2, ErrorMessage = "İsim en az 2 karakter olabilir.")]
        [MaxLength(50, ErrorMessage = "İsim en fazla 50 karakter olabilir.")]
        [RegularExpression(
            @"^[a-zA-ZçÇğĞıİöÖşŞüÜ\s-]+$",
            ErrorMessage = "İsim sadece harf içerebilir."
        )]
        [NoXss]
        public string FirstName { get; set; } = null!;

        [Required(ErrorMessage = "Soyisim gereklidir.")]
        [MaxLength(50, ErrorMessage = "Soyisim en fazla 50 karakter olabilir.")]
        [MinLength(2, ErrorMessage = "Soyisim en az 2 karakter olabilir.")]
        [RegularExpression(
            @"^[a-zA-ZçÇğĞıİöÖşŞüÜ\s-]+$",
            ErrorMessage = "Soyisim sadece harf içerebilir."
        )]
        [NoXss]
        public string LastName { get; set; } = null!;

        [Required(ErrorMessage = "Telefon numarası gereklidir.")]
        [TurkishPhone]
        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
        public string PhoneNumber { get; set; } = null!;

        [Required(ErrorMessage = "Doğum tarihi gereklidir.")]
        [DataType(DataType.Date, ErrorMessage = "Geçerli bir tarih giriniz.")]
        [MinAge(18)]
        [MaxAge(120)]
        public DateOnly BirthDate { get; set; }
    }
}