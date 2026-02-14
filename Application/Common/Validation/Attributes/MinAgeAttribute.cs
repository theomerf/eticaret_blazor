using System.ComponentModel.DataAnnotations;

namespace Application.Common.Validation.Attributes
{
    public class MinAgeAttribute : ValidationAttribute
    {
        private readonly int _minAge;

        public MinAgeAttribute(int minAge)
        {    
            _minAge = minAge;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            DateTime birthDate;
            if (value is DateTime dt)
            {
                birthDate = dt;
            }
            else if (value is DateOnly d)
            {
                birthDate = d.ToDateTime(TimeOnly.MinValue);
            }
            else
            {
                return new ValidationResult("Geçersiz tarih formatı.");
            }

            var today = DateTime.Today;
            var age = today.Year - birthDate.Year;

            if (birthDate > today.AddYears(-age))
                age--;

            return age >= _minAge
                ? ValidationResult.Success
                : new ValidationResult(ErrorMessage ?? $"{_minAge} yaşından büyük olmalısınız.");
        }
    }
}
