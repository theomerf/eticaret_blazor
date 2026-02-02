namespace Application.Repositories.Interfaces
{
    public interface IRepositoryManager{
        IProductRepository Product {get; }
        ICategoryRepository Category {get; }
        IOrderRepository Order { get; }
        IUserReviewRepository UserReview { get; }
        ICartRepository Cart { get; }
        INotificationRepository Notification { get; }
        IAuditLogRepository AuditLog { get; }
        ISecurityLogRepository SecurityLog { get; }
        IAddressRepository Address { get; }
        ICouponRepository Coupon { get; }
        ICampaignRepository Campaign { get; }
        IOrderHistoryRepository OrderHistory { get; }
        ICouponUsageRepository CouponUsage { get; }
        IOrderLinePaymentTransactionRepository OrderLinePaymentTransaction { get; }

        void Save();
        Task SaveAsync();
        void ClearTracker();
    }
}