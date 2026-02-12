using Domain.Entities;

namespace Application.Repositories.Interfaces
{
    public interface ISecurityLogRepository : IRepositoryBase<SecurityLog>
    {
        Task<IEnumerable<SecurityLog>> GetByUserIdAsync(string userId, int pageNumber = 1, int pageSize = 50);
        Task<IEnumerable<SecurityLog>> GetFailedLoginsAsync(DateTime since);
        Task<IEnumerable<SecurityLog>> GetRecentAsync(int count = 100);
        Task<int> CountOfFailedLoginsAsync(string ipAddress, DateTime since);
        Task<int> CountOfPaymentAttemptsAsync(string ipAddress, DateTime since);
        void Create(SecurityLog securityLog);
    }
}
