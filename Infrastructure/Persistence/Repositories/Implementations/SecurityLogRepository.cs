using Application.Repositories.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Implementations
{
    public class SecurityLogRepository : RepositoryBase<SecurityLog>, ISecurityLogRepository
    {
        public SecurityLogRepository(RepositoryContext context) : base(context)
        {
        }

        public async Task<IEnumerable<SecurityLog>> GetByUserIdAsync(string userId, int pageNumber = 1, int pageSize = 50)
        {
            var logs = await FindAll(false)
                .Where(sl => sl.UserId == userId)
                .OrderByDescending(sl => sl.Timestamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return logs;
        }

        public async Task<int> CountOfFailedLoginsAsync(string ipAddress, DateTime since)
        {
            var count = await FindAll(false)
                .Where(sl => sl.IpAddress == ipAddress && sl.EventType == "FailedLogin" && sl.Timestamp >= since)
                .CountAsync();

            return count;
        }

        public async Task<IEnumerable<SecurityLog>> GetFailedLoginsAsync(DateTime since)
        {
            var logs = await FindAll(false)
                .Where(sl => sl.EventType == "FailedLogin" && sl.Timestamp >= since)
                .OrderByDescending(sl => sl.Timestamp)
                .ToListAsync(); 
            
            return logs;
        }

        public async Task<IEnumerable<SecurityLog>> GetRecentAsync(int count = 100)
        {
            var logs = await FindAll(false)
                .OrderByDescending(sl => sl.Timestamp)
                .Take(count)
                .ToListAsync();

            return logs;
        }

        public async Task<int> CountOfPaymentAttemptsAsync(string ipAddress, DateTime since)
        {
            var paymentEventTypes = new[] { "SuspiciousPayment", "MultipleCardAttempts", "PaymentAnomaly" };
            
            var count = await FindAll(false)
                .Where(sl => sl.IpAddress == ipAddress 
                    && paymentEventTypes.Contains(sl.EventType) 
                    && sl.Timestamp >= since)
                .CountAsync();

            return count;
        }

        public void Create(SecurityLog securityLog)
        {
            CreateEntity(securityLog);
        }
    }
}
