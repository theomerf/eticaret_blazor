using Application.Queries.RequestParameters;
using Application.Repositories.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Implementations
{
    public class UserRepository : RepositoryBase<User>, IUserRepository
    {
        public UserRepository(RepositoryContext context) : base(context)
        {
        }

        public async Task<(IEnumerable<User> users, int count, int activeCount)> GetAllAdminAsync(UserRequestParametersAdmin p, bool trackChanges, CancellationToken ct = default)
        {
            var query = FindAll(trackChanges)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FilterBy(p.IsActive, u => u.IsActive, FilterOperator.Equal);

            if (!string.IsNullOrWhiteSpace(p.Role))
                query = query.Where(u => u.UserRoles.Any(ur => ur.Role.Name == p.Role));

            if (!string.IsNullOrWhiteSpace(p.SearchTerm))
            {
                var searchLower = p.SearchTerm.ToLower();
                query = query.Where(u =>
                    u.FirstName.ToLower().Contains(searchLower) ||
                    u.LastName.ToLower().Contains(searchLower) ||
                    u.Email!.ToLower().Contains(searchLower) ||
                    u.PhoneNumber!.Contains(searchLower));
            }

            var count = await query.CountAsync(ct);
            var activeCount = await query.CountAsync(u => u.IsActive, ct);

            query = p.SortBy switch
            {
                "name_asc" => query.OrderBy(u => u.FirstName).ThenBy(u => u.LastName),
                "name_desc" => query.OrderByDescending(u => u.FirstName).ThenByDescending(u => u.LastName),
                "date_asc" => query.OrderBy(u => u.CreatedAt),
                "login_desc" => query.OrderByDescending(u => u.LastLoginDate),
                _ => query.OrderByDescending(u => u.CreatedAt)
            };

            var users = await query
                .ToPaginate(p.PageNumber, p.PageSize)
                .ToListAsync(ct);

            return (users, count, activeCount);
        }

        public async Task<User?> GetByIdAsync(string userId, bool trackChanges, CancellationToken ct = default)
        {
            var user = await FindByCondition(u => u.Id == userId, trackChanges)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(ct);

            return user;
        }

        public async Task<IReadOnlyList<string>> GetActiveUserIdsBatchAsync(int pageNumber, int pageSize, bool trackChanges, CancellationToken ct = default)
        {
            if (pageNumber < 1)
                pageNumber = 1;

            if (pageSize < 1)
                pageSize = 1000;

            return await FindAllByCondition(u => u.IsActive, trackChanges)
                .OrderBy(u => u.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(u => u.Id)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<Application.DTOs.UserEmailLookupDto>> SearchEmailLookupAsync(string searchTerm, int take, bool trackChanges, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return [];

            var normalized = searchTerm.Trim().ToLower();
            var limit = Math.Clamp(take, 1, 50);

            return await FindAllByCondition(u => u.IsActive, trackChanges)
                .Where(u =>
                    u.Email != null &&
                    u.Email.ToLower().Contains(normalized))
                .OrderBy(u => u.Email)
                .Select(u => new Application.DTOs.UserEmailLookupDto
                {
                    Id = u.Id,
                    Email = u.Email!
                })
                .Take(limit)
                .ToListAsync(ct);
        }
    }
}
