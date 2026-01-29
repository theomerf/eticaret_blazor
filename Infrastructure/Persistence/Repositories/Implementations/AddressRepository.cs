using Application.Repositories.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Implementations
{
    public class AddressRepository : RepositoryBase<Address>, IAddressRepository
    {
        public AddressRepository(RepositoryContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Address>> GetAllAddressesAsync(bool trackChanges)
        {
            var addresses = await FindAll(trackChanges)
                .ToListAsync();

            return addresses;
        }

        public async Task<Address?> GetOneAddressAsync(int addressId, bool trackChanges)
        {
            var address = await FindByCondition(a => a.AddressId == addressId, trackChanges)
                .FirstOrDefaultAsync();

            return address; 
        }

        public async Task<IEnumerable<Address>> GetAllUserAddressesOfOneUserAsync(string userId, bool trackChanges)
        {
            var addresses = await FindAll(trackChanges)
                .Where(a => a.UserId == userId)
                .ToListAsync();

            return addresses;
        }

        public void CreateAddress(Address address)
        {
            Create(address);
        }

        public void UpdateAddress(Address address)
        {
            Update(address);
        }

        public void DeleteAddress(Address address)
        {
            Remove(address);
        }
    }
}
