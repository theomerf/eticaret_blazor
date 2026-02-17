using System.Data;

namespace Application.Repositories.Interfaces
{
    public interface IRepositoryManager{
        IProductRepository Product {get; }
        IProductVariantRepository ProductVariant {get; }
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
        IActivityRepository Activity { get; }
        ICategoryVariantAttributeRepository CategoryVariantAttribute { get; }
        IUserRepository User { get; }

        void Save();
        Task SaveAsync();
        void ClearTracker();
        Task CanConnectAsync();

        Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> action,
            IsolationLevel? isolationLevel = null,
            CancellationToken ct = default);

        Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action,
            IsolationLevel? isolationLevel = null,
            CancellationToken ct = default);
    }
}