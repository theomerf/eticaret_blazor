using Application.Common.Exceptions;
using Application.DTOs;
using Application.Repositories.Interfaces;
using Application.Services.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Application.Services.Implementations
{
    public class NotificationManager : INotificationService
    {
        private readonly IRepositoryManager _manager;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISecurityLogService _securityLogService;
        private readonly ILogger<NotificationManager> _logger;

        public NotificationManager(
            IRepositoryManager manager,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            ISecurityLogService securityLogService,
            ILogger<NotificationManager> logger)
        {
            _manager = manager;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _securityLogService = securityLogService;
            _logger = logger;
        }

        public async Task<IEnumerable<NotificationDto>> GetByUserIdAsync(string userId)
        {
            var notifications = await _manager.Notification.GetAllAsync(userId, false);
            var notificationsDto = _mapper.Map<IEnumerable<NotificationDto>>(notifications);

            return notificationsDto;
        }

        private async Task<Notification> GetOneNotificationForServiceAsync(int notificationId, bool trackChanges)
        {
            var notification = await _manager.Notification.GetByIdAsync(notificationId, trackChanges);
            if (notification == null)
            {
                throw new NotificationNotFoundException(notificationId);
            }

            return notification;
        }

        public async Task<OperationResult<NotificationDto>> MarkAsReadAsync(int notificationId)
        {
            var notification = await GetOneNotificationForServiceAsync(notificationId, true);

            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

            if (notification.UserId != userId)
            {
                await _securityLogService.LogUnauthorizedAccessAsync(
                    userId: userId,
                    requestPath: _httpContextAccessor.HttpContext?.Request?.Path.ToString() ?? ""
                );
                throw new UnauthorizedAccessException("Bunun için yetkiniz yok.");
            }

            notification.MarkAsRead();

            await _manager.SaveAsync();

            _logger.LogInformation(
                "Notification marked as read. NotificationId: {NotificationId}, UserId: {UserId}",
                notificationId, userId);

            return OperationResult<NotificationDto>.Success("Bildirim başarıyla okundu olarak işaretlendi.");
        }

        public async Task<OperationResult<NotificationDto>> RemoveAllAsync()
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var notifications = await _manager.Notification.GetAllAsync(userId, false);

            if (notifications == null || !notifications.Any())
            {
                _logger.LogWarning("No notifications found to delete for user {UserId}", userId);
                return OperationResult<NotificationDto>.Failure("Silinecek bildirim bulunamadı.", ResultType.NotFound);
            }

            foreach (var notification in notifications)
            {
                notification.SoftDelete(userId);
            }

            _manager.Notification.UpdateMultiple(notifications);

            await _manager.SaveAsync();

            _logger.LogInformation(
                "All notifications deleted for user {UserId}. Count: {Count}",
                userId, notifications.Count());

            return OperationResult<NotificationDto>.Success("Tüm bildirimler başarıyla silindi.");
        }

        public async Task<OperationResult<NotificationDto>> MarkAllAsReadAsync()
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var notifications = await _manager.Notification.GetAllAsync(userId, false);

            if (notifications == null || !notifications.Any())
            {
                _logger.LogWarning("No notifications found to mark as read for user {UserId}", userId);
                return OperationResult<NotificationDto>.Failure("Okundu olarak işaretlenecek bildirim bulunamadı.", ResultType.NotFound);
            }

            foreach (var notification in notifications)
            {
                notification.MarkAsRead();
            }

            _manager.Notification.UpdateMultiple(notifications);

            await _manager.SaveAsync();

            _logger.LogInformation(
                "All notifications marked as read for user {UserId}. Count: {Count}",
                userId, notifications.Count());

            return OperationResult<NotificationDto>.Success("Tüm bildirimler başarıyla okundu olarak işaretlendi.");
        }

        public async Task<OperationResult<NotificationDto>> CreateAsync(NotificationDtoForCreation notificationDto)
        {
            try
            {
                var notification = _mapper.Map<Notification>(notificationDto);

                notification.ValidateForCreation();

                notification.IsSystemGenerated = true;
                notification.IsSent = true;

                _manager.Notification.Create(notification);

                await _manager.SaveAsync();

                _logger.LogInformation(
                    "System notification created. NotificationId: {NotificationId}, UserId: {UserId}, Type: {Type}",
                    notification.NotificationId, notification.UserId, notification.NotificationType);

                return OperationResult<NotificationDto>.Success("Bildirim başarıyla oluşturuldu.");
            }
            catch (NotificationValidationException ex)
            {
                _logger.LogWarning(ex, "Notification validation failed. UserId: {UserId}", notificationDto.UserId);
                return OperationResult<NotificationDto>.Failure(ex.Message, ResultType.ValidationError);
            }
        }

        public async Task<OperationResult<NotificationDto>> CreateBulkNotificationAsync(NotificationDtoForBulkCreation notificationDto)
        {
            try
            {
                var adminId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
                var notifications = new List<Notification>();

                foreach (var userId in notificationDto.UserIds)
                {
                    var notification = new Notification
                    {
                        NotificationType = notificationDto.NotificationType,
                        Title = notificationDto.Title,
                        Description = notificationDto.Description,
                        UserId = userId,
                        IsSystemGenerated = false, 
                        CreatedByUserId = adminId,
                        UpdatedByUserId = adminId,
                        ScheduledFor = notificationDto.ScheduledFor,
                        IsSent = notificationDto.ScheduledFor == null // Zamanlanmışsa henüz gönderilmedi
                    };

                    notification.ValidateForCreation();

                    notifications.Add(notification);
                }

                foreach (var notification in notifications)
                {
                    _manager.Notification.Create(notification);
                }

                await _manager.SaveAsync();

                _logger.LogInformation(
                    "Bulk notifications created by admin. AdminId: {AdminId}, Count: {Count}, Scheduled: {IsScheduled}",
                    adminId, notifications.Count, notificationDto.ScheduledFor != null);

                return OperationResult<NotificationDto>.Success($"{notifications.Count} bildirim başarıyla oluşturuldu.");
            }
            catch (NotificationValidationException ex)
            {
                _logger.LogWarning(ex, "Bulk notification validation failed");
                return OperationResult<NotificationDto>.Failure(ex.Message, ResultType.ValidationError);
            }
        }

        public async Task<OperationResult<NotificationDto>> RemoveAsync(int notificationId)
        {
            var notification = await GetOneNotificationForServiceAsync(notificationId, true);
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

            if (notification.UserId != userId)
            {
                await _securityLogService.LogUnauthorizedAccessAsync(
                    userId: userId,
                    requestPath: _httpContextAccessor.HttpContext?.Request?.Path.ToString() ?? ""
                );
                throw new UnauthorizedAccessException("Bunun için yetkiniz yok.");
            }

            notification.SoftDelete(userId);

            await _manager.SaveAsync();

            _logger.LogInformation(
                "Notification deleted. NotificationId: {NotificationId}, UserId: {UserId}",
                notificationId, userId);

            return OperationResult<NotificationDto>.Success("Bildirim başarıyla silindi.");
        }
    }
}
