using Domain.Entities;

namespace Application.Services.Interfaces
{
    public interface IAuditLogService
    {
        Task LogAsync(string userId, string userName, string action, string entityName,
            string? entityId = null, object? oldValues = null, object? newValues = null);
        Task<IEnumerable<AuditLog>> GetByUserIdAsync(string userId, int pageNumber = 1, int pageSize = 50);
        Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityName, string entityId);
        
        Task LogPaymentEventAsync(string userId, string userName, string action, string orderNumber, 
            string? transactionId, decimal amount, string status, string? provider = null);

        Task LogPaymentTransactionAsync(string userId, string userName, string action, string orderNumber,
            string? transactionId, decimal amount, string status, string? provider = null);
    }
}
