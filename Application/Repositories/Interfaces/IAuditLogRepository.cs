using Domain.Entities;

namespace Application.Repositories.Interfaces
{
    public interface IAuditLogRepository : IRepositoryBase<AuditLog>
    {
        Task<IEnumerable<AuditLog>> GetByUserIdAsync(string userId, int pageNumber, int pageSize);
        Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityName, string entityId);
        Task<IEnumerable<AuditLog>> GetRecentAsync(int count = 100);
        void Create(AuditLog auditLog);
    }
}
