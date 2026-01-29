using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Application.Common.Validation.Attributes
{
    public class NoSqlInjectionAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;
            var input = value.ToString()!;

            var sqlPatterns = new[]
            {
                @"(\b(SELECT|INSERT|UPDATE|DELETE|DROP|CREATE|ALTER|EXEC|EXECUTE)\b)",
                @"(--|;|\/\*|\*\/)",
                @"(\bOR\b.*=.*)",
                @"(\bAND\b.*=.*)",
                @"('|(--)|;|\/\*|\*\/|xp_|sp_)"
            };
            foreach (var pattern in sqlPatterns)
            {
                if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
                    return new ValidationResult("Geçersiz karakter veya ifade tespit edildi.");
            }
            return ValidationResult.Success;
        }
    }
}
