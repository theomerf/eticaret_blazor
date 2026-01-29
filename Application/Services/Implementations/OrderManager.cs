using Application.DTOs;
using Application.Repositories.Interfaces;
using Application.Services.Interfaces;
using AutoMapper;
using Domain.Entities;
using Microsoft.Extensions.Caching.Memory;

namespace Application.Services.Implementations
{
    public class OrderManager : IOrderService
    {
        private readonly IRepositoryManager _manager;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _cache;

        public OrderManager(IRepositoryManager manager, IMapper mapper, IMemoryCache cache)
        {
            _manager = manager;
            _mapper = mapper;
            _cache = cache;
        }

        public async Task<IEnumerable<OrderDto>> GetAllOrdersAsync() => _mapper.Map<IEnumerable<OrderDto>>(await _manager.Order.GetAllOrdersAsync());

        public async Task<int> GetNumberOfInProcessAsync()
        {
            string cacheKey = "ordersInProgressCount";

            if (_cache.TryGetValue(cacheKey, out int cachedCount))
            {
                return cachedCount;
            }

            var count = await _manager.Order.GetNumberOfInProcessAsync();

            _cache.Set(cacheKey, count,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                    SlidingExpiration = TimeSpan.FromMinutes(2)
                });

            return count;
        }
        
        public async Task CompleteAsync(int id)
        {
            await _manager.Order.CompleteAsync(id);
            await _manager.SaveAsync();
        }

        public async Task<OrderDto?> GetOneOrderAsync(int id)
        {
            var orders = await _manager.Order.GetOneOrderAsync(id);
            return _mapper.Map<OrderDto>(orders);
        }

        public async Task SaveOrderAsync(OrderDto orderDto)
        {
            var order = _mapper.Map<Order>(orderDto);
            await _manager.Order.SaveOrderAsync(order);
        }

        public async Task<IEnumerable<OrderDto>> GetOrdersOfUserAsync(string? userId)
        {
            var orders = await _manager.Order.GetOrdersOfUserAsync(userId);
            return _mapper.Map<IEnumerable<OrderDto>>(orders);
        }

        public async Task<IEnumerable<DailySalesDto>> GetLast30DaysSalesDataAsync()
        {
            var salesData = await _manager.Order.GetLast30DaysSalesDataAsync();
            return salesData;
        }
    }
}
