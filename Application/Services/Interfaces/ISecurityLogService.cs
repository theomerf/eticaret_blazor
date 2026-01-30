using Domain.Entities;

namespace Application.Services.Interfaces
{
    public interface ISecurityLogService
    {
        Task LogLoginAsync(string userId, string userName, string email, bool isSuccess, string? failureReason = null);
        Task LogLogoutAsync(string userId, string userName);
        Task LogFailedLoginAsync(string? email, string failureReason);
        Task LogUnauthorizedAccessAsync(string? userId, string requestPath);
        Task<bool> IsIpBlockedAsync(string ipAddress);
        Task<IEnumerable<SecurityLog>> GetUserSecurityLogsAsync(string userId, int pageNumber = 1, int pageSize = 50);
        
        // Payment Security Methods
        Task LogSuspiciousPaymentActivityAsync(string? userId, string orderNumber, decimal amount, string reason);
        Task LogMultipleCardAttemptsAsync(string ipAddress, int attemptCount, TimeSpan timeWindow);
        Task LogRateLimitViolationAsync(string? userId, string ipAddress, string endpoint, int requestCount);
        Task LogPaymentAnomalyAsync(string? userId, string orderNumber, string anomalyType, string details);
        Task<int> GetPaymentAttemptsFromIpAsync(string ipAddress, TimeSpan timeWindow);
    }
}
