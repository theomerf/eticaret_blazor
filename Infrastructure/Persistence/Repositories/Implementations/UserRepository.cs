using Application.DTOs;
using Application.Queries.RequestParameters;
using Application.Repositories.Interfaces;
using AutoMapper;
using Domain.Entities;
using Infrastructure.Persistence.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Implementations
{
    public class UserRepository : RepositoryBase<User>, IUserRepository
    {
        public UserRepository(RepositoryContext context) : base(context)
        {
        }

        public async Task<(IEnumerable<User> users, int count)> GetAllAdminAsync(UserRequestParametersAdmin p, bool trackChanges, CancellationToken ct = default)
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

            return (users, count);
        }

        public async Task<User?> GetByIdAsync(string userId, bool trackChanges, CancellationToken ct = default)
        {
            var user = await FindByCondition(u => u.Id == userId, trackChanges)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(ct);

            return user;
        }
    }
}
