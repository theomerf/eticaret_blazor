using Application.Common.Validation.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public record AddressDtoForUpdate : AddressDtoForCreation
    {
        [Required(ErrorMessage = "Adres ID gereklidir.")]
        public int AddressId { get; set; }
    }
}
