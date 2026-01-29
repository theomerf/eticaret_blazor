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

        public RepositoryManager(IProductRepository productRepository, RepositoryContext context, ICategoryRepository categoryRepository, IOrderRepository orderRepository, IUserReviewRepository userReviewRepository, ICartRepository cartRepository, INotificationRepository notificationRepository, IAuditLogRepository auditLogRepository, ISecurityLogRepository securityLogRepository, IAddressRepository addressRepository)
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
    }
}