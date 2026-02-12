using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class ProductSpecificationDto
    {
        [Required(ErrorMessage = "Özellik adı gereklidir.")]
        public string Key { get; set; } = null!;

        [Required(ErrorMessage = "Özellik değeri gereklidir.")]
        public string Value { get; set; } = null!;
    }
}
