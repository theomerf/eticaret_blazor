using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public record UserDtoForUpdateAdmin : UserDtoForCreation
    {
        [Required(ErrorMessage = "Kullanıcı ID gereklidir.")]
        public string Id { get; set; } = null!;
    }
}
