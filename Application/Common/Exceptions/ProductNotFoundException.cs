namespace Domain.Exceptions
{
    public sealed class ProductNotFoundException : NotFoundException
    {
        public ProductNotFoundException(int id)
        : base($"{id} id'sine sahip ürün bulunamadı: .")
        {
        }
    }
}
