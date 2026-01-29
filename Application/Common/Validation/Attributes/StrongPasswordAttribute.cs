using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Application.Common.Validation.Attributes
{
    public class StrongPasswordAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return ValidationResult.Success;
            var password = value.ToString()!;
            if (password.Length < 8)
                return new ValidationResult("Şifre en az 8 karakter olmalıdır.");
            if (!Regex.IsMatch(password, @"[A-Z]"))
                return new ValidationResult("Şifre en az 1 büyük harf içermelidir.");
            if (!Regex.IsMatch(password, @"[a-z]"))
                return new ValidationResult("Şifre en az 1 küçük harf içermelidir.");
            if (!Regex.IsMatch(password, @"\d"))
                return new ValidationResult("Şifre en az 1 rakam içermelidir.");
            if (!Regex.IsMatch(password, @"[@$!%*?&\-_\.]"))
                return new ValidationResult("Şifre en az 1 özel karakter (@$!%*?&-_. ) içermelidir.");
            var commonPasswords = new[] { "12345678", "password", "qwerty123", "admin123" };
            if (commonPasswords.Contains(password.ToLower()))
                return new ValidationResult("Bu şifre çok yaygın kullanılıyor. Daha güçlü bir şifre seçin.");
            if (HasSequentialCharacters(password))
                return new ValidationResult("Şifre ardışık karakterler içeremez (örn: 123, abc).");
            return ValidationResult.Success;
        }

        private bool HasSequentialCharacters(string password)
        {
            for (int i = 0; i < password.Length - 2; i++)
            {
                if (password[i] + 1 == password[i + 1] && password[i + 1] + 1 == password[i + 2])
                    return true;
            }
            return false;
        }
    }
}