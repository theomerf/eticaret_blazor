using Application.Repositories.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Implementations
{
    public class AuditLogRepository : RepositoryBase<AuditLog>, IAuditLogRepository
    {
        public AuditLogRepository(RepositoryContext context) : base(context)
        {
        }

        public async Task<IEnumerable<AuditLog>> GetByUserIdAsync(string userId, int pageNumber, int pageSize)
        {
            var logs = await FindAll(false)
                .Where(al => al.UserId == userId)
                .OrderByDescending(al => al.Timestamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return logs;    
        }

        public async Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityName, string entityId)
        {
            var logs = await FindAll(false)
                .Where(al => al.EntityName == entityName && al.EntityId == entityId)
                .OrderByDescending(al => al.Timestamp)
                .ToListAsync();

            return logs;
        }

        public async Task<IEnumerable<AuditLog>> GetRecentAsync(int count = 100)
        {
            var logs = await FindAll(false)
                .OrderByDescending(a => a.Timestamp)
                .Take(count)
                .ToListAsync();

            return logs;
        }

        public void Create(AuditLog auditLog)
        {
            CreateEntity(auditLog);
        }
    }
}
