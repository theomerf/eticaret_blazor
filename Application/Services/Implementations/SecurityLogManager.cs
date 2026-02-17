using Application.Repositories.Interfaces;
using Application.Services.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace Application.Services.Implementations
{
    public class SecurityLogManager : ISecurityLogService
    {
        private readonly IRepositoryManager _manager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const int MaxFailedAttemptsBeforeBlock = 5;
        private static readonly TimeSpan BlockDuration = TimeSpan.FromMinutes(15);

        public SecurityLogManager(IRepositoryManager manager, IHttpContextAccessor httpContextAccessor)
        {
            _manager = manager;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogLoginAsync(string userId, string userName, string email, bool isSuccess, string? failureReason = null)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var securityLog = new SecurityLog
            {
                EventType = isSuccess ? "Login" : "FailedLogin",
                UserId = userId,
                UserName = userName,
                Email = email,
                IpAddress = ipAddress,
                UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
                IsSuccess = isSuccess,
                FailureReason = failureReason,
                Timestamp = DateTime.UtcNow
            };

            _manager.SecurityLog.Create(securityLog);
            await _manager.SaveAsync();

            // Serilog'a da yaz
            if (isSuccess)
            {
                Log.Information("User {UserName} logged in successfully from {IpAddress}", userName, ipAddress);
            }
            else
            {
                Log.Warning("Failed login attempt for {Email} from {IpAddress}. Reason: {Reason}",
                    email, ipAddress, failureReason);
            }
        }
        public async Task LogLogoutAsync(string userId, string userName)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var securityLog = new SecurityLog
            {
                EventType = "Logout",
                UserId = userId,
                UserName = userName,
                IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
                IsSuccess = true,
                Timestamp = DateTime.UtcNow
            };
            _manager.SecurityLog.Create(securityLog);
            await _manager.SaveAsync();

            Log.Information("User {UserName} logged out", userName);
        }
        public async Task LogFailedLoginAsync(string? email, string failureReason)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var securityLog = new SecurityLog
            {
                EventType = "FailedLogin",
                Email = email,
                IpAddress = ipAddress,
                UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
                IsSuccess = false,
                FailureReason = failureReason,
                Timestamp = DateTime.UtcNow
            };
            _manager.SecurityLog.Create(securityLog);
            await _manager.SaveAsync();

            Log.Warning("Failed login attempt for {Email} from {IpAddress}", email, ipAddress);
        }
        public async Task LogUnauthorizedAccessAsync(string? userId, string requestPath)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var securityLog = new SecurityLog
            {
                EventType = "Unauthorized",
                UserId = userId,
                IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
                IsSuccess = false,
                FailureReason = $"Unauthorized access attempt to {requestPath}",
                Timestamp = DateTime.UtcNow
            };
            _manager.SecurityLog.Create(securityLog);
            await _manager.SaveAsync();

            Log.Warning("Unauthorized access attempt by user {UserId} to {Path}", userId, requestPath);
        }
        public async Task<bool> IsIpBlockedAsync(string ipAddress)
        {
            var since = DateTime.UtcNow.Subtract(BlockDuration);
            var failedCount = await _manager.SecurityLog.CountOfFailedLoginsAsync(ipAddress, since);

            return failedCount >= MaxFailedAttemptsBeforeBlock;
        }
        public async Task<IEnumerable<SecurityLog>> GetByUserIdAsync(string userId, int pageNumber = 1, int pageSize = 50)
        {
            var logs = await _manager.SecurityLog.GetByUserIdAsync(userId, pageNumber, pageSize);

            return logs;
        }

        public async Task LogSuspiciousPaymentActivityAsync(string? userId, string orderNumber, decimal amount, string reason)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            
            var securityLog = new SecurityLog
            {
                EventType = "SuspiciousPayment",
                UserId = userId,
                IpAddress = ipAddress,
                UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
                IsSuccess = false,
                FailureReason = $"Suspicious payment detected for order {orderNumber}. Amount: {amount:C}. Reason: {reason}",
                Timestamp = DateTime.UtcNow
            };
            
            _manager.SecurityLog.Create(securityLog);
            await _manager.SaveAsync();

            Log.Warning("Suspicious payment activity detected. Order: {OrderNumber}, Amount: {Amount}, Reason: {Reason}, IP: {IpAddress}",
                orderNumber, amount, reason, ipAddress);
        }

        public async Task LogMultipleCardAttemptsAsync(string ipAddress, int attemptCount, TimeSpan timeWindow)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            
            var securityLog = new SecurityLog
            {
                EventType = "MultipleCardAttempts",
                IpAddress = ipAddress,
                UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
                IsSuccess = false,
                FailureReason = $"Multiple card attempts detected: {attemptCount} attempts within {timeWindow.TotalMinutes:F0} minutes from same IP",
                Timestamp = DateTime.UtcNow
            };
            
            _manager.SecurityLog.Create(securityLog);
            await _manager.SaveAsync();

            Log.Warning("Multiple card payment attempts detected. IP: {IpAddress}, Attempts: {AttemptCount}, TimeWindow: {TimeWindow}",
                ipAddress, attemptCount, timeWindow);
        }

        public async Task LogRateLimitViolationAsync(string? userId, string ipAddress, string endpoint, int requestCount)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            
            var securityLog = new SecurityLog
            {
                EventType = "RateLimitViolation",
                UserId = userId,
                IpAddress = ipAddress,
                UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
                IsSuccess = false,
                FailureReason = $"Rate limit exceeded for endpoint {endpoint}. Request count: {requestCount}",
                Timestamp = DateTime.UtcNow
            };
            
            _manager.SecurityLog.Create(securityLog);
            await _manager.SaveAsync();

            Log.Warning("Rate limit violation detected. Endpoint: {Endpoint}, IP: {IpAddress}, RequestCount: {RequestCount}",
                endpoint, ipAddress, requestCount);
        }

        public async Task LogPaymentAnomalyAsync(string? userId, string orderNumber, string anomalyType, string details)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            
            var securityLog = new SecurityLog
            {
                EventType = "PaymentAnomaly",
                UserId = userId,
                IpAddress = ipAddress,
                UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
                IsSuccess = false,
                FailureReason = $"Payment anomaly detected. Order: {orderNumber}, Type: {anomalyType}, Details: {details}",
                Timestamp = DateTime.UtcNow
            };
            
            _manager.SecurityLog.Create(securityLog);
            await _manager.SaveAsync();

            Log.Warning("Payment anomaly detected. Order: {OrderNumber}, Type: {AnomalyType}, Details: {Details}",
                orderNumber, anomalyType, details);
        }

        public async Task<int> CountOfPaymentAttemptsFromIpAsync(string ipAddress, TimeSpan timeWindow)
        {
            var since = DateTime.UtcNow.Subtract(timeWindow);
            var count = await _manager.SecurityLog.CountOfPaymentAttemptsAsync(ipAddress, since);
            return count;
        }

        public async Task LogImpersonationStartAsync(string adminId, string adminUserName, string targetUserId, string targetUserName)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            var securityLog = new SecurityLog
            {
                EventType = "ImpersonationStart",
                UserId = adminId,
                UserName = adminUserName,
                IpAddress = ipAddress,
                UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
                IsSuccess = true,
                AdditionalInfo = $"Admin impersonating user: {targetUserName} (ID: {targetUserId})",
                Timestamp = DateTime.UtcNow
            };

            _manager.SecurityLog.Create(securityLog);
            await _manager.SaveAsync();

            Log.Warning("IMPERSONATION: Admin {AdminUserName} (ID: {AdminId}) started impersonating user {TargetUserName} (ID: {TargetUserId}) from IP {IpAddress}",
                adminUserName, adminId, targetUserName, targetUserId, ipAddress);
        }

        public async Task LogImpersonationStopAsync(string adminId, string adminUserName)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            var securityLog = new SecurityLog
            {
                EventType = "ImpersonationStop",
                UserId = adminId,
                UserName = adminUserName,
                IpAddress = ipAddress,
                UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
                IsSuccess = true,
                AdditionalInfo = "Admin returned to their own session",
                Timestamp = DateTime.UtcNow
            };

            _manager.SecurityLog.Create(securityLog);
            await _manager.SaveAsync();

            Log.Information("IMPERSONATION ENDED: Admin {AdminUserName} (ID: {AdminId}) stopped impersonation from IP {IpAddress}",
                adminUserName, adminId, ipAddress);
        }
    }
}
