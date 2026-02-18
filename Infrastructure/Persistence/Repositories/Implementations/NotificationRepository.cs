using Application.DTOs;
using Application.Queries.RequestParameters;
using Application.Repositories.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Implementations
{
    public class NotificationRepository : RepositoryBase<Notification>, INotificationRepository
    {
        public NotificationRepository(RepositoryContext context) : base(context)
        {
        }

        public async Task<(IReadOnlyList<NotificationAdminGroupDto> notifications, int count, int sentCount, int pendingCount)> GetAllAdminAsync(NotificationRequestParametersAdmin p, bool trackChanges, CancellationToken ct = default)
        {
            var grouped = BuildAdminGroupedQuery(p, trackChanges);

            var count = await grouped.CountAsync(ct);
            var sentCount = await grouped.CountAsync(x => x.IsSent, ct);
            var pendingCount = await grouped.CountAsync(x => !x.IsSent, ct);

            grouped = p.SortBy switch
            {
                "date_asc" => grouped.OrderBy(x => x.CreatedAt),
                "date_desc" => grouped.OrderByDescending(x => x.CreatedAt),
                "schedule_asc" => grouped.OrderBy(x => x.ScheduledFor ?? DateTime.MaxValue),
                "schedule_desc" => grouped.OrderByDescending(x => x.ScheduledFor ?? DateTime.MinValue),
                "status_desc" => grouped.OrderByDescending(x => x.IsSent).ThenByDescending(x => x.CreatedAt),
                _ => grouped.OrderByDescending(x => x.CreatedAt)
            };

            var pageItems = await grouped
                .ToPaginate(p.PageNumber, p.PageSize)
                .ToListAsync(ct);

            if (pageItems.Count == 0)
                return (pageItems, count, sentCount, pendingCount);

            var groupIds = pageItems.Select(x => x.GroupId).ToArray();

            var previewRows = await GetAdminBaseQuery(trackChanges)
                .Include(x => x.User)
                .Where(x => groupIds.Contains(x.NotificationGroupId ?? ("single-" + x.NotificationId)))
                .OrderBy(x => x.CreatedAt)
                .Select(x => new
                {
                    GroupId = x.NotificationGroupId ?? ("single-" + x.NotificationId),
                    Email = x.User != null ? x.User.Email : null
                })
                .ToListAsync(ct);

            var previewMap = previewRows
                .Where(x => !string.IsNullOrWhiteSpace(x.Email))
                .GroupBy(x => x.GroupId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.Email!).Distinct().Take(3).ToList());

            foreach (var item in pageItems)
            {
                if (item.SentToAllActiveUsers)
                {
                    item.RecipientPreviewEmails = [];
                    continue;
                }

                if (previewMap.TryGetValue(item.GroupId, out var emails))
                    item.RecipientPreviewEmails = emails;
            }

            return (pageItems, count, sentCount, pendingCount);
        }

        public async Task<IReadOnlyList<NotificationRecipientDto>> GetGroupRecipientsAsync(string groupId, int take, bool trackChanges, CancellationToken ct = default)
        {
            var limit = Math.Clamp(take, 1, 1000);

            return await GetAdminBaseQuery(trackChanges)
                .Include(n => n.User)
                .Where(n => (n.NotificationGroupId ?? ("single-" + n.NotificationId)) == groupId)
                .OrderBy(n => n.CreatedAt)
                .Select(n => new NotificationRecipientDto
                {
                    UserId = n.UserId,
                    Email = n.User != null ? n.User.Email : null,
                    IsSent = n.IsSent,
                    IsRead = n.IsRead
                })
                .Take(limit)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<Notification>> GetAllAsync(string userId, bool trackChanges)
        {
            var now = DateTime.UtcNow;
            return await FindAllByCondition(
                    n => n.UserId == userId &&
                         (n.IsSent || !n.ScheduledFor.HasValue || n.ScheduledFor <= now),
                    trackChanges)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<Notification?> GetByIdAsync(int notificationId, bool trackChanges)
        {
            return await FindByCondition(n => n.NotificationId == notificationId, trackChanges)
                .FirstOrDefaultAsync();
        }

        public async Task<IReadOnlyList<Notification>> GetPendingScheduledAsync(DateTime utcNow, int take, bool trackChanges)
        {
            return await FindByCondition(n =>
                    !n.IsSent &&
                    n.ScheduledFor.HasValue &&
                    n.ScheduledFor.Value <= utcNow,
                    trackChanges)
                .OrderBy(n => n.ScheduledFor)
                .Take(take)
                .ToListAsync();
        }

        public void RemoveMultiple(IEnumerable<Notification> notifications)
        {
            RemoveRange(notifications);
        }

        public void UpdateMultiple(IEnumerable<Notification> notifications)
        {
            UpdateRange(notifications);
        }

        public void Create(Notification notification)
        {
            CreateEntity(notification);
        }

        public void Remove(Notification notification)
        {
            RemoveEntity(notification);
        }

        public void Update(Notification notification)
        {
            UpdateEntity(notification);
        }

        private IQueryable<NotificationAdminGroupDto> BuildAdminGroupedQuery(NotificationRequestParametersAdmin p, bool trackChanges)
        {
            var query = GetAdminBaseQuery(trackChanges)
                .Where(n => !n.IsSystemGenerated)
                .FilterBy(p.NotificationType, n => n.NotificationType, FilterOperator.Equal)
                .FilterBy(p.IsSent, n => n.IsSent, FilterOperator.Equal);

            if (!string.IsNullOrWhiteSpace(p.SearchTerm))
            {
                var search = p.SearchTerm.Trim().ToLower();
                query = query.Where(n =>
                    n.Title.ToLower().Contains(search) ||
                    n.Description.ToLower().Contains(search) ||
                    (n.User != null && n.User.Email != null && n.User.Email.ToLower().Contains(search)));
            }

            return query
                .GroupBy(n => n.NotificationGroupId ?? ("single-" + n.NotificationId))
                .Select(g => new NotificationAdminGroupDto
                {
                    GroupId = g.Key,
                    NotificationType = g.Max(x => x.NotificationType),
                    Title = g.Max(x => x.Title) ?? string.Empty,
                    Description = g.Max(x => x.Description) ?? string.Empty,
                    IsSent = !g.Any(x => !x.IsSent),
                    IsRead = !g.Any(x => !x.IsRead),
                    ScheduledFor = g.Max(x => x.ScheduledFor),
                    CreatedAt = g.Max(x => x.CreatedAt),
                    SentToAllActiveUsers = g.Any(x => x.SentToAllActiveUsers),
                    RecipientCount = g.Select(x => x.UserId).Distinct().Count()
                });
        }

        private IQueryable<Notification> GetAdminBaseQuery(bool trackChanges)
        {
            var query = _context.Set<Notification>().IgnoreQueryFilters();
            return trackChanges ? query : query.AsNoTracking();
        }
    }
}
