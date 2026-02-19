using Application.DTOs;
using Application.Services.Interfaces;
using System.Security.Claims;

namespace ETicaret.Services
{
    public interface IAccountStateService
    {
        UserDto? Profile { get; }
        IReadOnlyList<OrderDto> Orders { get; }
        IReadOnlyDictionary<int, OrderWithDetailsDto> OrderDetailsCache { get; }
        IReadOnlyList<AddressDto> Addresses { get; }
        IReadOnlyList<UserReviewDto> Reviews { get; }

        bool IsProfileLoaded { get; }
        bool IsOrdersLoaded { get; }
        bool IsAddressesLoaded { get; }
        bool IsReviewsLoaded { get; }

        bool IsProfileLoading { get; }
        bool IsOrdersLoading { get; }
        bool IsAddressesLoading { get; }
        bool IsReviewsLoading { get; }

        event Action? OnChange;

        Task LoadProfileAsync(bool forceRefresh = false);
        Task LoadOrdersAsync(bool forceRefresh = false, bool preloadOrderDetails = false);
        Task<OrderWithDetailsDto?> GetOrderDetailsAsync(int orderId, bool forceRefresh = false);
        Task LoadAddressesAsync(bool forceRefresh = false);
        Task LoadReviewsAsync(bool forceRefresh = false);

        void InvalidateProfile();
        void InvalidateOrders();
        void InvalidateOrderDetails(int? orderId = null);
        void InvalidateAddresses();
        void InvalidateReviews();
        void InvalidateAll();
    }

    public class AccountStateService : IAccountStateService
    {
        private readonly IUserService _userService;
        private readonly IOrderService _orderService;
        private readonly IAddressService _addressService;
        private readonly IUserReviewService _userReviewService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private List<OrderDto> _orders = new();
        private readonly Dictionary<int, OrderWithDetailsDto> _orderDetailsCache = new();
        private List<AddressDto> _addresses = new();
        private List<UserReviewDto> _reviews = new();

        public UserDto? Profile { get; private set; }
        public IReadOnlyList<OrderDto> Orders => _orders;
        public IReadOnlyDictionary<int, OrderWithDetailsDto> OrderDetailsCache => _orderDetailsCache;
        public IReadOnlyList<AddressDto> Addresses => _addresses;
        public IReadOnlyList<UserReviewDto> Reviews => _reviews;

        public bool IsProfileLoaded { get; private set; }
        public bool IsOrdersLoaded { get; private set; }
        public bool IsAddressesLoaded { get; private set; }
        public bool IsReviewsLoaded { get; private set; }

        public bool IsProfileLoading { get; private set; }
        public bool IsOrdersLoading { get; private set; }
        public bool IsAddressesLoading { get; private set; }
        public bool IsReviewsLoading { get; private set; }

        public event Action? OnChange;

        public AccountStateService(
            IUserService userService,
            IOrderService orderService,
            IAddressService addressService,
            IUserReviewService userReviewService,
            IHttpContextAccessor httpContextAccessor)
        {
            _userService = userService;
            _orderService = orderService;
            _addressService = addressService;
            _userReviewService = userReviewService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LoadProfileAsync(bool forceRefresh = false)
        {
            if (IsProfileLoaded && !forceRefresh)
            {
                return;
            }

            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                InvalidateAll();
                return;
            }

            IsProfileLoading = true;
            NotifyStateChanged();

            Profile = await _userService.GetOneUserAsync(userId);
            IsProfileLoaded = true;

            IsProfileLoading = false;
            NotifyStateChanged();
        }

        public async Task LoadOrdersAsync(bool forceRefresh = false, bool preloadOrderDetails = false)
        {
            if (IsOrdersLoaded && !forceRefresh)
            {
                return;
            }

            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                InvalidateAll();
                return;
            }

            IsOrdersLoading = true;
            NotifyStateChanged();

            _orders = (await _orderService.GetByUserIdAsync(userId)).ToList();
            IsOrdersLoaded = true;

            if (preloadOrderDetails)
            {
                foreach (var order in _orders)
                {
                    if (!_orderDetailsCache.ContainsKey(order.OrderId))
                    {
                        var details = await _orderService.GetByIdAsync(order.OrderId);
                        if (details != null)
                        {
                            _orderDetailsCache[order.OrderId] = details;
                        }
                    }
                }
            }

            IsOrdersLoading = false;
            NotifyStateChanged();
        }

        public async Task<OrderWithDetailsDto?> GetOrderDetailsAsync(int orderId, bool forceRefresh = false)
        {
            if (!forceRefresh && _orderDetailsCache.TryGetValue(orderId, out var cachedDetails))
            {
                return cachedDetails;
            }

            var details = await _orderService.GetByIdAsync(orderId);
            if (details != null)
            {
                _orderDetailsCache[orderId] = details;
                NotifyStateChanged();
            }

            return details;
        }

        public async Task LoadAddressesAsync(bool forceRefresh = false)
        {
            if (IsAddressesLoaded && !forceRefresh)
            {
                return;
            }

            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                InvalidateAll();
                return;
            }

            IsAddressesLoading = true;
            NotifyStateChanged();

            _addresses = (await _addressService.GetByUserIdAsync(userId)).ToList();
            IsAddressesLoaded = true;

            IsAddressesLoading = false;
            NotifyStateChanged();
        }

        public async Task LoadReviewsAsync(bool forceRefresh = false)
        {
            if (IsReviewsLoaded && !forceRefresh)
            {
                return;
            }

            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                InvalidateAll();
                return;
            }

            IsReviewsLoading = true;
            NotifyStateChanged();

            _reviews = (await _userReviewService.GetByUserIdAsync(userId)).ToList();
            IsReviewsLoaded = true;

            IsReviewsLoading = false;
            NotifyStateChanged();
        }

        public void InvalidateProfile()
        {
            Profile = null;
            IsProfileLoaded = false;
            NotifyStateChanged();
        }

        public void InvalidateOrders()
        {
            _orders = new List<OrderDto>();
            IsOrdersLoaded = false;
            NotifyStateChanged();
        }

        public void InvalidateOrderDetails(int? orderId = null)
        {
            if (orderId.HasValue)
            {
                _orderDetailsCache.Remove(orderId.Value);
            }
            else
            {
                _orderDetailsCache.Clear();
            }

            NotifyStateChanged();
        }

        public void InvalidateAddresses()
        {
            _addresses = new List<AddressDto>();
            IsAddressesLoaded = false;
            NotifyStateChanged();
        }

        public void InvalidateReviews()
        {
            _reviews = new List<UserReviewDto>();
            IsReviewsLoaded = false;
            NotifyStateChanged();
        }

        public void InvalidateAll()
        {
            Profile = null;
            _orders = new List<OrderDto>();
            _orderDetailsCache.Clear();
            _addresses = new List<AddressDto>();
            _reviews = new List<UserReviewDto>();

            IsProfileLoaded = false;
            IsOrdersLoaded = false;
            IsAddressesLoaded = false;
            IsReviewsLoaded = false;

            IsProfileLoading = false;
            IsOrdersLoading = false;
            IsAddressesLoading = false;
            IsReviewsLoading = false;

            NotifyStateChanged();
        }

        private string? GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
