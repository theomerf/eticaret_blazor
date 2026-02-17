using Application.DTOs;
using Application.Queries.RequestParameters;
using Domain.Entities;

namespace Application.Repositories.Interfaces
{
    public interface IUserRepository : IRepositoryBase<User>
    {
        Task<(IEnumerable<User> users, int count, int activeCount)> GetAllAdminAsync(UserRequestParametersAdmin p, bool trackChanges, CancellationToken ct = default);
        Task<User?> GetByIdAsync(string userId, bool trackChanges, CancellationToken ct = default);
    }
}
