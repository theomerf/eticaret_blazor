using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Application.Common.Validation.Attributes
{
    public class TurkishPhoneAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return ValidationResult.Success;

            var phone = value.ToString()!.Trim();

            // Türkiye telefon formatları:
            // 05xxxxxxxxx
            // +905xxxxxxxxx
            // 905xxxxxxxxx
            var pattern = @"^05\d{9}$";

            if (!Regex.IsMatch(phone, pattern))
                return new ValidationResult(ErrorMessage ?? "Geçerli bir Türkiye telefon numarası giriniz (örn: 05xxxxxxxxx).");

            return ValidationResult.Success;
        }
    }
}