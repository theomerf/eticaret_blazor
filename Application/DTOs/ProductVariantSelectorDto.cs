using Domain.Entities;

namespace Application.DTOs
{
    public record ProductVariantSelectorDto
    {
        public string Key { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public VariantAttributeType Type { get; set; }
    }
}
