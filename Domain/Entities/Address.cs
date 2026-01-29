using Domain.Exceptions;

namespace Domain.Entities
{
    public class Address : SoftDeletableEntity
    {
        public int AddressId { get; set; }

        public string UserId { get; set; } = null!;
        public User? User { get; set; }

        public string Title { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Phone { get; set; } = null!;

        public string City { get; set; } = null!;
        public string District { get; set; } = null!;
        public string AddressLine { get; set; } = null!;
        public string? PostalCode { get; set; }

        public bool IsDefault { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public void SoftDelete(string deletedByUserId)
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
            DeletedByUserId = deletedByUserId;
        }

        public void ValidateForCreation()
        {
            if (string.IsNullOrWhiteSpace(Title))
            {
                throw new AddressValidationException("Adres başlığı boş olamaz.");
            }

            if (Title.Length < 2 || Title.Length > 50)
            {
                throw new AddressValidationException("Adres başlığı 2-50 karakter arasında olmalıdır.");
            }

            if (string.IsNullOrWhiteSpace(FirstName))
            {
                throw new AddressValidationException("Ad boş olamaz.");
            }

            if (FirstName.Length < 2 || FirstName.Length > 100)
            {
                throw new AddressValidationException("Ad 2-100 karakter arasında olmalıdır.");
            }

            if (string.IsNullOrWhiteSpace(LastName))
            {
                throw new AddressValidationException("Soyad boş olamaz.");
            }

            if (LastName.Length < 2 || LastName.Length > 100)
            {
                throw new AddressValidationException("Soyad 2-100 karakter arasında olmalıdır.");
            }

            if (string.IsNullOrWhiteSpace(Phone))
            {
                throw new AddressValidationException("Telefon numarası boş olamaz.");
            }

            if (string.IsNullOrWhiteSpace(City))
            {
                throw new AddressValidationException("Şehir boş olamaz.");
            }
            
            if (City.Length < 2 || City.Length > 100)
            {
                throw new AddressValidationException("Şehir 2-100 karakter arasında olmalıdır.");
            }

            if (string.IsNullOrWhiteSpace(District))
            {
                throw new AddressValidationException("İlçe boş olamaz.");
            }

            if (District.Length < 2 || District.Length > 100)
            {
                throw new AddressValidationException("İlçe 2-100 karakter arasında olmalıdır.");
            }

            if (string.IsNullOrWhiteSpace(AddressLine))
            {
                throw new AddressValidationException("Adres detayı boş olamaz.");
            }

            if (AddressLine.Length < 10 || AddressLine.Length > 500)
            {
                throw new AddressValidationException("Adres detayı 10-500 karakter arasında olmalıdır.");
            }
        }

        public void ValidateForUpdate()
        {
            ValidateForCreation();
        }
    }
}
