namespace Infrastructure.BackgroundJobs.Hangfire
{
    public sealed class SoftDeleteCleanupOptions
    {
        public bool Enabled { get; set; } = true;
        public int BatchSize { get; set; } = 500;
        public int MaxBatchesPerEntity { get; set; } = 20;

        // Soft-delete to hard-delete windows (days)
        public int NotificationRetentionDays { get; set; } = 90;
        public int AddressRetentionDays { get; set; } = 365;
        public int UserReviewRetentionDays { get; set; } = 730;
        public int CampaignRetentionDays { get; set; } = 730;
        public int CouponRetentionDays { get; set; } = 730;
        public int CategoryVariantAttributeRetentionDays { get; set; } = 365;

        // Non-soft-delete data retention (optional)
        public int ActivityRetentionDays { get; set; } = 180;
        public int AuditLogRetentionDays { get; set; } = 3650;
        public int SecurityLogRetentionDays { get; set; } = 1825;
    }
}
