using Domain.Entities;

namespace Application.Repositories.Interfaces
{
    public interface ISecurityLogRepository : IRepositoryBase<SecurityLog>
    {
        void Add(SecurityLog securityLog);
        Task<IEnumerable<SecurityLog>> GetByUserIdAsync(string userId, int pageNumber = 1, int pageSize = 50);
        Task<IEnumerable<SecurityLog>> GetFailedLoginsAsync(DateTime since);
        Task<int> GetFailedLoginCountAsync(string ipAddress, DateTime since);
        Task<IEnumerable<SecurityLog>> GetRecentAsync(int count = 100);
        Task<int> GetPaymentAttemptsCountAsync(string ipAddress, DateTime since);
    }
}
