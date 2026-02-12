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

        public async Task<IEnumerable<Address>> GetAllAsync(bool trackChanges)
        {
            var addresses = await FindAll(trackChanges)
                .ToListAsync();

            return addresses;
        }

        public async Task<Address?> GetByIdAsync(int addressId, bool trackChanges)
        {
            var address = await FindByCondition(a => a.AddressId == addressId, trackChanges)
                .FirstOrDefaultAsync();

            return address; 
        }

        public async Task<IEnumerable<Address>> GetByUserIdAsync(string userId, bool trackChanges)
        {
            var addresses = await FindAll(trackChanges)
                .Where(a => a.UserId == userId)
                .ToListAsync();

            return addresses;
        }

        public void Create(Address address)
        {
            CreateEntity(address);
        }

        public void Update(Address address)
        {
            UpdateEntity(address);
        }

        public void Delete(Address address)
        {
            RemoveEntity(address);
        }
    }
}
