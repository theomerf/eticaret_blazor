using Domain.Exceptions;

namespace Domain.Entities
{
    public class ProductImage : AuditableEntity
    {
        public int ProductImageId { get; set; }
        public Product? Product { get; set; }
        public int ProductId { get; set; }
        public string ImageUrl { get; set; } = null!;
        public bool IsPrimary { get; set; } = true;
        public string? Caption { get; set; }
        public int DisplayOrder { get; set; } = 0;

        public void ValidateForCreation()
        {
            if (string.IsNullOrWhiteSpace(ImageUrl))
            {
                throw new ProductValidationException("Resim URL'si boş olamaz.");
            }

            if (ImageUrl.Length > 2048)
            {
                throw new ProductValidationException("Resim URL'si en fazla 2048 karakter olabilir.");
            }

            /*if (!Uri.TryCreate(ImageUrl, UriKind.Absolute, out var uri))
            {
                throw new ProductValidationException("Geçerli bir URL giriniz.");
            }*/

            if (DisplayOrder < 0 || DisplayOrder > 999)
            {
                throw new ProductValidationException("Sıralama 0-999 arasında olmalıdır.");
            }

            if (Caption != null && Caption.Length > 512)
            {
                throw new ProductValidationException("Başlık en fazla 512 karakter olabilir.");
            }
        }
    }
}
