using Domain.Entities;

namespace Application.DTOs
{
    public class CategoryVariantAttributeDto
    {
        public int VariantAttributeId { get; set; }
        public int CategoryId { get; set; }
        public string Key { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public VariantAttributeType Type { get; set; }
        public bool IsVariantDefiner { get; set; }
        public bool IsTechnicalSpec { get; set; }
        public int SortOrder { get; set; }
        public bool IsRequired { get; set; }
    }
}
