using Domain.Entities;
using Hangfire;
using Infrastructure.Persistence;
using Application.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.BackgroundJobs.Hangfire.Jobs.Maintenance
{
    public sealed class SoftDeleteCleanupJob
    {
        private readonly RepositoryContext _context;
        private readonly ICacheService _cache;
        private readonly SoftDeleteCleanupOptions _options;
        private readonly ILogger<SoftDeleteCleanupJob> _logger;

        public SoftDeleteCleanupJob(
            RepositoryContext context,
            ICacheService cache,
            IOptions<SoftDeleteCleanupOptions> options,
            ILogger<SoftDeleteCleanupJob> logger)
        {
            _context = context;
            _cache = cache;
            _options = options.Value;
            _logger = logger;
        }

        [Queue(Queues.Maintenance)]
        public async Task ExecuteAsync(CancellationToken ct = default)
        {
            if (!_options.Enabled)
            {
                _logger.LogInformation("Soft-delete cleanup disabled by configuration.");
                return;
            }

            var now = DateTime.UtcNow;
            var summary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["notifications"] = await PurgeSoftDeletedNotificationsAsync(now, ct),
                ["addresses"] = await PurgeSoftDeletedAddressesAsync(now, ct),
                ["userReviews"] = await PurgeSoftDeletedUserReviewsAsync(now, ct),
                ["campaigns"] = await PurgeSoftDeletedCampaignsAsync(now, ct),
                ["coupons"] = await PurgeSoftDeletedCouponsAsync(now, ct),
                ["categoryVariantAttributes"] = await PurgeSoftDeletedCategoryVariantAttributesAsync(now, ct),
                ["activities"] = await PurgeActivitiesAsync(now, ct),
                ["auditLogs"] = await PurgeAuditLogsAsync(now, ct),
                ["securityLogs"] = await PurgeSecurityLogsAsync(now, ct)
            };

            if (summary["notifications"] > 0)
                await _cache.RemoveByPrefixAsync("notifications:", ct);

            if (summary["userReviews"] > 0)
                await _cache.RemoveByPrefixAsync("userReviews:", ct);

            if (summary["campaigns"] > 0)
                await _cache.RemoveByPrefixAsync("campaigns:", ct);

            if (summary["coupons"] > 0)
                await _cache.RemoveByPrefixAsync("coupons:", ct);

            if (summary["activities"] > 0)
                await _cache.RemoveByPrefixAsync("activities:", ct);

            _logger.LogInformation(
                "Soft-delete cleanup completed. Notifications={Notifications}, Addresses={Addresses}, UserReviews={UserReviews}, Campaigns={Campaigns}, Coupons={Coupons}, CategoryVariantAttributes={CategoryVariantAttributes}, Activities={Activities}, AuditLogs={AuditLogs}, SecurityLogs={SecurityLogs}",
                summary["notifications"],
                summary["addresses"],
                summary["userReviews"],
                summary["campaigns"],
                summary["coupons"],
                summary["categoryVariantAttributes"],
                summary["activities"],
                summary["auditLogs"],
                summary["securityLogs"]);
        }

        private Task<int> PurgeSoftDeletedNotificationsAsync(DateTime now, CancellationToken ct)
            => PurgeBatchAsync(
                queryFactory: () => _context.Notifications
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(x => x.IsDeleted && x.DeletedAt.HasValue && x.DeletedAt.Value <= now.AddDays(-RetentionDays(_options.NotificationRetentionDays)))
                    .OrderBy(x => x.NotificationId),
                entityName: "Notification",
                ct: ct);

        private Task<int> PurgeSoftDeletedAddressesAsync(DateTime now, CancellationToken ct)
            => PurgeBatchAsync(
                queryFactory: () => _context.Addresses
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(x => x.IsDeleted && x.DeletedAt.HasValue && x.DeletedAt.Value <= now.AddDays(-RetentionDays(_options.AddressRetentionDays)))
                    .OrderBy(x => x.AddressId),
                entityName: "Address",
                ct: ct);

        private Task<int> PurgeSoftDeletedUserReviewsAsync(DateTime now, CancellationToken ct)
            => PurgeBatchAsync(
                queryFactory: () => _context.UserReviews
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(x => x.IsDeleted && x.DeletedAt.HasValue && x.DeletedAt.Value <= now.AddDays(-RetentionDays(_options.UserReviewRetentionDays)))
                    .OrderBy(x => x.UserReviewId),
                entityName: "UserReview",
                ct: ct);

        private Task<int> PurgeSoftDeletedCampaignsAsync(DateTime now, CancellationToken ct)
            => PurgeBatchAsync(
                queryFactory: () => _context.Campaigns
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(x => x.IsDeleted && x.DeletedAt.HasValue && x.DeletedAt.Value <= now.AddDays(-RetentionDays(_options.CampaignRetentionDays)))
                    .OrderBy(x => x.CampaignId),
                entityName: "Campaign",
                ct: ct);

        private Task<int> PurgeSoftDeletedCouponsAsync(DateTime now, CancellationToken ct)
            => PurgeBatchAsync(
                queryFactory: () => _context.Coupons
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(x => x.IsDeleted && x.DeletedAt.HasValue && x.DeletedAt.Value <= now.AddDays(-RetentionDays(_options.CouponRetentionDays)))
                    .OrderBy(x => x.CouponId),
                entityName: "Coupon",
                ct: ct);

        private Task<int> PurgeSoftDeletedCategoryVariantAttributesAsync(DateTime now, CancellationToken ct)
            => PurgeBatchAsync(
                queryFactory: () => _context.CategoryVariantAttributes
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(x => x.IsDeleted && x.DeletedAt.HasValue && x.DeletedAt.Value <= now.AddDays(-RetentionDays(_options.CategoryVariantAttributeRetentionDays)))
                    .OrderBy(x => x.VariantAttributeId),
                entityName: "CategoryVariantAttribute",
                ct: ct);

        private Task<int> PurgeActivitiesAsync(DateTime now, CancellationToken ct)
            => PurgeBatchAsync(
                queryFactory: () => _context.Activities
                    .AsNoTracking()
                    .Where(x => x.CreatedAt <= now.AddDays(-RetentionDays(_options.ActivityRetentionDays)))
                    .OrderBy(x => x.ActivityId),
                entityName: "Activity",
                ct: ct);

        private Task<int> PurgeAuditLogsAsync(DateTime now, CancellationToken ct)
            => PurgeBatchAsync(
                queryFactory: () => _context.AuditLogs
                    .AsNoTracking()
                    .Where(x => x.Timestamp <= now.AddDays(-RetentionDays(_options.AuditLogRetentionDays)))
                    .OrderBy(x => x.AuditLogId),
                entityName: "AuditLog",
                ct: ct);

        private Task<int> PurgeSecurityLogsAsync(DateTime now, CancellationToken ct)
            => PurgeBatchAsync(
                queryFactory: () => _context.SecurityLogs
                    .AsNoTracking()
                    .Where(x => x.Timestamp <= now.AddDays(-RetentionDays(_options.SecurityLogRetentionDays)))
                    .OrderBy(x => x.SecurityLogId),
                entityName: "SecurityLog",
                ct: ct);

        private async Task<int> PurgeBatchAsync<TEntity>(
            Func<IQueryable<TEntity>> queryFactory,
            string entityName,
            CancellationToken ct)
            where TEntity : class
        {
            var totalDeleted = 0;
            var batchSize = Math.Max(1, _options.BatchSize);
            var maxBatches = Math.Max(1, _options.MaxBatchesPerEntity);

            for (var batchIndex = 0; batchIndex < maxBatches; batchIndex++)
            {
                var batch = await queryFactory()
                    .Take(batchSize)
                    .ToListAsync(ct);

                if (batch.Count == 0)
                    break;

                _context.Set<TEntity>().RemoveRange(batch);
                await _context.SaveChangesAsync(ct);
                totalDeleted += batch.Count;
                _context.ChangeTracker.Clear();
            }

            if (totalDeleted > 0)
            {
                _logger.LogInformation("Hard-delete cleanup removed {Count} rows from {Entity}.", totalDeleted, entityName);
            }

            return totalDeleted;
        }

        private static int RetentionDays(int days) => Math.Max(0, days);
    }
}
