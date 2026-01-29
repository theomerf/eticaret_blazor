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

        void Save();
        Task SaveAsync();
        void ClearTracker();
    }
}