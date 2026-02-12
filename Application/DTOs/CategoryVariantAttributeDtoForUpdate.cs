using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class CategoryVariantAttributeDtoForUpdate : CategoryVariantAttributeDtoForCreation
    {

        [Required(ErrorMessage = "Varyant özelliði ID gereklidir.")]
        public int VariantAttributeId { get; set; }
    }
}
