using System.ComponentModel.DataAnnotations;

namespace Application.Common.Validation.Attributes
{
    public class MaxAgeAttribute : ValidationAttribute
    {
        private readonly int _maxAge;

        public MaxAgeAttribute(int maxAge)
        {
            _maxAge = maxAge;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            if (value is not DateTime birthDate)
                return new ValidationResult("Geçersiz tarih formatı.");

            var today = DateTime.Today;
            var age = today.Year - birthDate.Year;

            if (birthDate > today.AddYears(-age))
                age--;

            return age <= _maxAge
                ? ValidationResult.Success
                : new ValidationResult(ErrorMessage ?? $"{_maxAge} yaşından küçük olmalısınız.");
        }
    }
}
