using Domain.Exceptions;

namespace Application.Common.Exceptions
{
    public class ProductVariantNotFoundException : NotFoundException
    {
        public ProductVariantNotFoundException(int productVariantId) : base($"{productVariantId} id'sine sahip ürün varyantı bulunamadı.")
        {
        }
    }
}
