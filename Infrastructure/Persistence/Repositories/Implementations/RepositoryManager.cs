using Application.Repositories.Interfaces;

namespace Infrastructure.Persistence.Repositories.Implementations
{
    public class RepositoryManager : IRepositoryManager
    {
        private readonly RepositoryContext _context;
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IUserReviewRepository _userReviewRepository;
        private readonly ICartRepository _cartRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly ISecurityLogRepository _securityLogRepository;
        private readonly IAddressRepository _addressRepository;
        private readonly ICouponRepository _couponRepository;
        private readonly ICampaignRepository _campaignRepository;
        private readonly IOrderHistoryRepository _orderHistoryRepository;
        private readonly ICouponUsageRepository _couponUsageRepository;
        private readonly IOrderLinePaymentTransactionRepository _orderLinePaymentTransactionRepository;
        private readonly IActivityRepository _activityRepository;

        public RepositoryManager(IProductRepository productRepository, RepositoryContext context, ICategoryRepository categoryRepository, IOrderRepository orderRepository, IUserReviewRepository userReviewRepository, ICartRepository cartRepository, INotificationRepository notificationRepository, IAuditLogRepository auditLogRepository, ISecurityLogRepository securityLogRepository, IAddressRepository addressRepository, ICouponRepository couponRepository, ICampaignRepository campaignRepository, IOrderHistoryRepository orderHistoryRepository, ICouponUsageRepository couponUsageRepository, IOrderLinePaymentTransactionRepository orderLinePaymentTransactionRepository, IActivityRepository activityRepository)
        {
            _context = context;
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _orderRepository = orderRepository;
            _userReviewRepository = userReviewRepository;
            _cartRepository = cartRepository;
            _notificationRepository = notificationRepository;
            _auditLogRepository = auditLogRepository;
            _securityLogRepository = securityLogRepository;
            _addressRepository = addressRepository;
            _couponRepository = couponRepository;
            _campaignRepository = campaignRepository;
            _orderHistoryRepository = orderHistoryRepository;
            _couponUsageRepository = couponUsageRepository;
            _orderLinePaymentTransactionRepository = orderLinePaymentTransactionRepository;
            _activityRepository = activityRepository;
        }

        public IProductRepository Product => _productRepository;
        public ICategoryRepository Category => _categoryRepository;
        public IOrderRepository Order => _orderRepository;
        public IUserReviewRepository UserReview => _userReviewRepository;
        public ICartRepository Cart => _cartRepository;
        public INotificationRepository Notification => _notificationRepository;
        public IAuditLogRepository AuditLog => _auditLogRepository;
        public ISecurityLogRepository SecurityLog => _securityLogRepository;
        public IAddressRepository Address => _addressRepository;
        public ICouponRepository Coupon => _couponRepository;
        public ICampaignRepository Campaign => _campaignRepository;
        public IOrderHistoryRepository OrderHistory => _orderHistoryRepository;
        public ICouponUsageRepository CouponUsage => _couponUsageRepository;
        public IOrderLinePaymentTransactionRepository OrderLinePaymentTransaction => _orderLinePaymentTransactionRepository;
        public IActivityRepository Activity => _activityRepository;

        public void Save()
        {
            _context.SaveChanges();
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }

        public void ClearTracker()
        {
            _context.ChangeTracker.Clear();
        }

        public async Task CanConnectAsync()
        {
            await _context.Database.CanConnectAsync();
        }
    }
}