using Application.Common.Validation.Attributes;
using Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public record NotificationDtoForCreation
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

        [Required(ErrorMessage = "Kullanıcı ID gereklidir.")]
        public string UserId { get; set; } = null!;

        public DateTime? ScheduledFor { get; set; }
    }
}
