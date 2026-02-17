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

        public async Task<Address?> GetDefaultByUserIdAsync(string userId, bool trackChanges)
        {
            return await FindByCondition(a => a.UserId == userId && a.IsDefault, trackChanges)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Address>> GetByUserIdAsync(string userId, bool trackChanges)
        {
            var addresses = await FindByCondition(a => a.UserId == userId, trackChanges)
                .ToListAsync();

            return addresses;
        }

        public async Task UnsetDefaultForUserAsync(string userId, CancellationToken ct = default)
        {
            await _context.Set<Address>()
                .Where(a => a.UserId == userId && a.IsDefault && !a.IsDeleted)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(a => a.IsDefault, false)
                    .SetProperty(a => a.UpdatedAt, DateTime.UtcNow), ct);
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
