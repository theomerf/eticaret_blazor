namespace Domain.Entities
{
    public class CategoryVariantAttribute : SoftDeletableEntity
    {
        public int VariantAttributeId { get; set; }

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public string Key { get; set; } = null!;
        public string DisplayName { get; set; } = null!;

        public VariantAttributeType Type { get; set; } = VariantAttributeType.Select;

        public bool IsVariantDefiner { get; set; }
        public bool IsTechnicalSpec { get; set; }

        public int SortOrder { get; set; }
        public bool IsRequired { get; set; }
    }

    public enum VariantAttributeType
    {
        String,
        Number,
        Boolean,
        Select,
        Color
    }
}
