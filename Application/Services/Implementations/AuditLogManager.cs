using Application.Repositories.Interfaces;
using Application.Services.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace Application.Services.Implementations
{
    public class AuditLogManager : IAuditLogService
    {
        private readonly IRepositoryManager _manager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditLogManager(IRepositoryManager manager, IHttpContextAccessor httpContextAccessor)
        {
            _manager = manager;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(string userId, string userName, string action, string entityName,
            string? entityId = null, object? oldValues = null, object? newValues = null)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            var auditLog = new AuditLog
            {
                UserId = userId,
                UserName = userName,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
                Timestamp = DateTime.UtcNow
            };

            _manager.AuditLog.Add(auditLog);
            await _manager.SaveAsync();
        }
        public async Task<IEnumerable<AuditLog>> GetUserActivityAsync(string userId, int pageNumber = 1, int pageSize = 50)
        {
            var logs = await _manager.AuditLog.GetByUserIdAsync(userId, pageNumber, pageSize);

            return logs;
        }
        public async Task<IEnumerable<AuditLog>> GetEntityHistoryAsync(string entityName, string entityId)
        {
            var logs = await _manager.AuditLog.GetByEntityAsync(entityName, entityId);

            return logs;
        }

        public async Task LogPaymentEventAsync(string userId, string userName, string action, string orderNumber,
            string? transactionId, decimal amount, string status, string? provider = null)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            var paymentDetails = new
            {
                OrderNumber = orderNumber,
                TransactionId = transactionId,
                Amount = amount,
                Status = status,
                Provider = provider ?? "Unknown",
                Timestamp = DateTime.UtcNow
            };

            var auditLog = new AuditLog
            {
                UserId = userId,
                UserName = userName,
                Action = action,
                EntityName = "Payment",
                EntityId = transactionId ?? orderNumber,
                NewValues = JsonSerializer.Serialize(paymentDetails),
                IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
                Timestamp = DateTime.UtcNow
            };

            _manager.AuditLog.Add(auditLog);
            await _manager.SaveAsync();
        }
    }
}
