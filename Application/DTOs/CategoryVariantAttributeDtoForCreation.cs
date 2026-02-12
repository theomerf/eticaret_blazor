using Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class CategoryVariantAttributeDtoForCreation
    {
        [Required]
        public int CategoryId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Key { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string DisplayName { get; set; } = null!;

        public VariantAttributeType Type { get; set; } = VariantAttributeType.Select;

        public bool IsVariantDefiner { get; set; }
        public bool IsTechnicalSpec { get; set; }
        public int SortOrder { get; set; }
        public bool IsRequired { get; set; }
    }
}
