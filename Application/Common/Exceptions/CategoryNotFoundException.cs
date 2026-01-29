namespace Domain.Exceptions
{
    public sealed class CategoryNotFoundException : NotFoundException
    {
        public CategoryNotFoundException(int id) : base($"{id} id'sine sahip kategori bulunamadı.")
        {
        }
    }
}
