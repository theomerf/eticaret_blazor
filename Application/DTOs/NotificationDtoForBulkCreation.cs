using Application.Common.Validation.Attributes;
using Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public record NotificationDtoForBulkCreation : IValidatableObject
    {
        [Required(ErrorMessage = "Bildirim tipi gereklidir.")]
        public NotificationType NotificationType { get; set; }

        [Required(ErrorMessage = "Bildirim başlığı gereklidir.")]
        [MinLength(3, ErrorMessage = "Bildirim başlığı en az 3 karakter olmalıdır.")]
        [MaxLength(200, ErrorMessage = "Bildirim başlığı en fazla 200 karakter olabilir.")]
        [NoXss]
        public string Title { get; set; } = null!;

        [Required(ErrorMessage = "Bildirim açıklaması gereklidir.")]
        [MinLength(5, ErrorMessage = "Bildirim açıklaması en az 5 karakter olmalıdır.")]
        [MaxLength(500, ErrorMessage = "Bildirim açıklaması en fazla 500 karakter olabilir.")]
        [NoXss]
        public string Description { get; set; } = null!;

        public List<string> UserIds { get; set; } = new();

        public bool SendToAllUsers { get; set; } = false;

        [Range(100, 5000, ErrorMessage = "Batch boyutu 100-5000 aralığında olmalıdır.")]
        public int BatchSize { get; set; } = 1000;

        public DateTime? ScheduledFor { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!SendToAllUsers && (UserIds == null || UserIds.Count == 0))
            {
                yield return new ValidationResult(
                    "En az bir kullanıcı seçilmelidir.",
                    [nameof(UserIds)]);
            }
        }
    }
}
