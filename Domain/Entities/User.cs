using Domain.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace Domain.Entities
{
    public class User : IdentityUser
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string IdentityNumber { get; set; } = "11111111111";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? BirthDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public ICollection<Address>? Addresses { get; set; }
        public ICollection<UserReview>? UserReviews { get; set; }
        public ICollection<int> FavouriteProductsId { get; set; } = new List<int>();

        public string? LastLoginIpAddress { get; set; }
        public string? RegistrationIpAddress { get; set; }
        public string? TwoFactorSecretKey { get; set; }
        public DateTime? LastFailedLoginDate { get; set; }
        public DateTime? LastPasswordChangeDate { get; set; }
        public DateTime? PhoneVerifiedDate { get; set; }

        public bool AcceptedTerms { get; set; } = false;
        public DateTime? TermsAcceptedDate { get; set; }
        public bool AcceptedMarketing { get; set; } = false;
        public DateTime? MarketingAcceptedDate { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedByUserId { get; set; }

        #region Validation Methods
        public void ValidateForCreation()
        {
            if (string.IsNullOrWhiteSpace(FirstName))
            {
                throw new UserValidationException("İsim boş olamaz.");
            }

            if (FirstName.Length < 2 || FirstName.Length > 50)
            {
                throw new UserValidationException("İsim 2-50 karakter arasında olmalıdır.");
            }

            if (string.IsNullOrWhiteSpace(LastName))
            {
                throw new UserValidationException("Soyisim boş olamaz.");
            }

            if (LastName.Length < 2 || LastName.Length > 50)
            {
                throw new UserValidationException("Soyisim 2-50 karakter arasında olmalıdır.");
            }

            if (BirthDate.HasValue)
            {
                var age = DateTime.UtcNow.Year - BirthDate.Value.Year;
                if (BirthDate.Value > DateTime.UtcNow.AddYears(-age)) age--;

                if (age < 18)
                {
                    throw new UserValidationException("Kullanıcı en az 18 yaşında olmalıdır.");
                }

                if (age > 120)
                {
                    throw new UserValidationException("Geçersiz doğum tarihi.");
                }
            }

            if (string.IsNullOrWhiteSpace(Email))
            {
                throw new UserValidationException("E-posta boş olamaz.");
            }

            if (Email.Length > 256)
            {
                throw new UserValidationException("E-posta en fazla 256 karakter olabilir.");
            }
        }

        public void ValidateForUpdate()
        {
            ValidateForCreation();
        }

        #endregion

        #region Business Logic Methods

        public void UpdateLastLogin(string ipAddress)
        {
            LastLoginDate = DateTime.UtcNow;
            LastLoginIpAddress = ipAddress;
        }

        public void AcceptTerms()
        {
            if (!AcceptedTerms)
            {
                AcceptedTerms = true;
                TermsAcceptedDate = DateTime.UtcNow;
            }
        }

        public void AcceptMarketing()
        {
            if (!AcceptedMarketing)
            {
                AcceptedMarketing = true;
                MarketingAcceptedDate = DateTime.UtcNow;
            }
        }

        public void SoftDelete(string deletedByUserId)
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
            DeletedByUserId = deletedByUserId;
        }

        #endregion
    }
}
