using Domain.Entities;

namespace Application.Repositories.Interfaces
{
    public interface IAddressRepository : IRepositoryBase<Address>
    {
        Task<IEnumerable<Address>> GetAllAddressesAsync(bool trackChanges);
        Task<Address?> GetOneAddressAsync(int addressId, bool trackChanges);
        Task<IEnumerable<Address>> GetAllUserAddressesOfOneUserAsync(string userId, bool trackChanges);
        void CreateAddress(Address address);
        void UpdateAddress(Address address);
        void DeleteAddress(Address address);
    }
}
