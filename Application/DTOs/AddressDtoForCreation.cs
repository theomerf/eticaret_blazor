using Application.Common.Validation.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public record AddressDtoForCreation
    {
        [Required(ErrorMessage = "Adres başlığı gereklidir.")]
        [MinLength(2, ErrorMessage = "Adres başlığı en az 2 karakter olabilir.")]
        [MaxLength(50, ErrorMessage = "Adres başlığı en fazla 50 karakter olabilir.")]
        [NoXss]
        public string Title { get; set; } = null!;

        [Required(ErrorMessage = "Ad gereklidir.")]
        [MinLength(2, ErrorMessage = "Ad en az 2 karakter olabilir.")]
        [MaxLength(100, ErrorMessage = "Ad en fazla 100 karakter olabilir.")]
        [NoXss]
        public string FirstName { get; set; } = null!;

        [Required(ErrorMessage = "Soyad gereklidir.")]
        [MinLength(2, ErrorMessage = "Soyad en az 2 karakter olabilir.")]
        [MaxLength(100, ErrorMessage = "Soyad en fazla 100 karakter olabilir.")]
        [NoXss]
        public string LastName { get; set; } = null!;

        [Required(ErrorMessage = "Telefon numarası gereklidir.")]
        [TurkishPhone]
        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
        [NoXss]
        public string Phone { get; set; } = null!;

        [Required(ErrorMessage = "Şehir gereklidir.")]
        [MinLength(2, ErrorMessage = "Şehir en az 2 karakter olabilir.")]
        [MaxLength(100, ErrorMessage = "Şehir en fazla 100 karakter olabilir.")]
        [NoXss]
        public string City { get; set; } = null!;

        [Required(ErrorMessage = "İlçe gereklidir.")]
        [MinLength(2, ErrorMessage = "İlçe en az 2 karakter olabilir.")]
        [MaxLength(100, ErrorMessage = "İlçe en fazla 100 karakter olabilir.")]
        [NoXss]
        public string District { get; set; } = null!;

        [Required(ErrorMessage = "Adres detayı gereklidir.")]
        [MinLength(10, ErrorMessage = "Adres detayı en az 10 karakter olabilir.")]
        [MaxLength(500, ErrorMessage = "Adres detayı en fazla 500 karakter olabilir.")]
        [NoXss]
        public string AddressLine { get; set; } = null!;

        [NoXss]
        public string? PostalCode { get; set; }

        public bool IsDefault { get; set; }
    }
}
