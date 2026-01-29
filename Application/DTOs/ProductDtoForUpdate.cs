using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public record ProductDtoForUpdate : ProductDtoForCreation
    {
        [Required(ErrorMessage = "Ürün ID gereklidir.")]
        public int ProductId { get; set; }
    }
}
