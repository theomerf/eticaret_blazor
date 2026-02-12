using Application.Common.Validation.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public record CategoryDtoForCreation
    {
        [Required(ErrorMessage = "Kategori adı gereklidir.")]
        [MaxLength(200, ErrorMessage = "Kategori adı en fazla 200 karakter olabilir.")]
        [MinLength(2, ErrorMessage = "Kategori adı en az 2 karakter olabilir.")]
        [NoXss]
        public string CategoryName { get; set; } = null!;
        [MaxLength(250, ErrorMessage = "Slug en fazla 250 karakter olabilir.")]
        [NoXss]
        public string? Slug { get; set; }
        [MaxLength(60, ErrorMessage = "Meta başlık en fazla 60 karakter olabilir.")]
        [NoXss]
        public string? MetaTitle { get; set; }
        [MaxLength(160, ErrorMessage = "Meta açıklama en fazla 160 karakter olabilir.")]
        [NoXss]
        public string? MetaDescription { get; set; }
        [MaxLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir.")]
        [NoXss]
        public string? Description { get; set; }
        [MaxLength(500, ErrorMessage = "Simge URL'si en fazla 500 karakter olabilir.")]
        [Url(ErrorMessage = "Geçerli bir URL giriniz.")]
        public string? IconUrl { get; set; }
        public int? ParentId { get; set; }
        [Range(0, 999, ErrorMessage = "Sıralama 0-999 arasında olmalıdır.")]
        public int DisplayOrder { get; set; }
        public bool IsVisible { get; set; } = true;
        public bool IsFeatured { get; set; }

        public List<CategoryVariantAttributeDtoForCreation> NewAttributes { get; set; } = new();
    }
}
