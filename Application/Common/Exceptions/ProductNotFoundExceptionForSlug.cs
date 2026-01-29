using Domain.Exceptions;

namespace Application.Common.Exceptions
{
    public class ProductNotFoundExceptionForSlug : NotFoundException
    {
        public ProductNotFoundExceptionForSlug(string slug)
        : base($"{slug} slug'ına sahip ürün bulunamadı: .")
        {
        }
    }
}
