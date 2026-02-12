using Domain.Entities;

namespace Application.Repositories.Interfaces
{
    public interface IAddressRepository : IRepositoryBase<Address>
    {
        Task<IEnumerable<Address>> GetAllAsync(bool trackChanges);
        Task<Address?> GetByIdAsync(int addressId, bool trackChanges);
        Task<IEnumerable<Address>> GetByUserIdAsync(string userId, bool trackChanges);
        void Create(Address address);
        void Update(Address address);
        void Delete(Address address);
    }
}
