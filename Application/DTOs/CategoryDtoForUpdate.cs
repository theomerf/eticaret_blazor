using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public record CategoryDtoForUpdate : CategoryDtoForCreation
    {
        [Required(ErrorMessage = "Kategori ID gereklidir.")]
        public int CategoryId { get; set; }

        public List<CategoryVariantAttributeDtoForUpdate> Attributes { get; set; } = new();
    }
}
