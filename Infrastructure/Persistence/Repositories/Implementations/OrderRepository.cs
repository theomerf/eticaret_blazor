using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Application.Repositories.Interfaces;
using Application.DTOs;

namespace Infrastructure.Persistence.Repositories.Implementations
{
    public sealed class OrderRepository : RepositoryBase<Order>, IOrderRepository
    {
        public OrderRepository(RepositoryContext context) : base(context)
        {
        }

        public async Task<Order?> GetOrderByIdAsync(int orderId, bool trackChanges)
        {
            var order = await FindByCondition(o => o.OrderId == orderId, trackChanges)
                .Include(o => o.Lines)
                .FirstOrDefaultAsync();

            return order;
        }

        public async Task<Order?> GetOrderByNumberAsync(string orderNumber, bool trackChanges)
        {
            var order = await FindByCondition(o => o.OrderNumber == orderNumber, trackChanges)
                .Include(o => o.Lines)
                .FirstOrDefaultAsync();

            return order;
        }

        public async Task<Order?> GetOrderWithDetailsAsync(int orderId, bool trackChanges)
        {
            var order = await FindByCondition(o => o.OrderId == orderId, trackChanges)
                .Include(o => o.Lines)
                .Include(o => o.AppliedCampaigns)
                .Include(o => o.History)
                    .ThenInclude(h => h.CreatedByUser)
                .FirstOrDefaultAsync();

            return order;
        }

        public async Task<IEnumerable<Order>> GetUserOrdersAsync(string userId, bool trackChanges)
        {
            var orders = await FindAllByCondition(
                o => o.UserId == userId && o.OrderStatus != OrderStatus.Failed, 
                trackChanges)
                .OrderByDescending(o => o.OrderedAt)
                .Select(o => new Order
                {
                    OrderId = o.OrderId,
                    OrderNumber = o.OrderNumber,
                    FirstName = o.FirstName,
                    LastName = o.LastName,
                    OrderStatus = o.OrderStatus,
                    PaymentStatus = o.PaymentStatus,    
                    OrderedAt = o.OrderedAt,
                    TotalAmount = o.TotalAmount,
                    Currency = o.Currency,
                    Lines = o.Lines.Select(l => new OrderLine
                    {
                        ImageUrl = l.ImageUrl,
                    }).ToList()
                })
                .ToListAsync();

            return orders;
        }

        public async Task<IEnumerable<Order>> GetOrdersByStatusAsync(OrderStatus status, bool trackChanges)
        {
            var orders = await FindAllByCondition(o => o.OrderStatus == status, trackChanges)
                .Include(o => o.Lines)
                .OrderByDescending(o => o.OrderedAt)
                .ToListAsync();

            return orders;
        }

        public async Task<IEnumerable<Order>> GetOrdersByPaymentStatusAsync(PaymentStatus status, bool trackChanges)
        {
            var orders = await FindAllByCondition(o => o.PaymentStatus == status, trackChanges)
                .Include(o => o.Lines)
                .OrderByDescending(o => o.OrderedAt)
                .ToListAsync();

            return orders;
        }

        public async Task<(IEnumerable<Order> orders, int count)> GetOrdersPagedAsync(int pageNumber, int pageSize, bool trackChanges)
        {
            var query = FindAll(trackChanges)
                .Include(o => o.Lines);

            var count = await query.CountAsync();

            var orders = await query
                .OrderByDescending(o => o.OrderedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (orders, count);
        }

        public async Task<IEnumerable<Order>> GetPendingPaymentOrdersAsync(bool trackChanges)
        {
            var orders = await FindAllByCondition(
                o => o.PaymentStatus == PaymentStatus.Pending && 
                     o.OrderStatus == OrderStatus.Pending,
                trackChanges)
                .Include(o => o.Lines)
                .OrderBy(o => o.OrderedAt)
                .ToListAsync();

            return orders;
        }

        public async Task<int> GetOrdersCountAsync()
        {
            var count = await CountAsync(false);

            return count;
        }

        public async Task<int> GetUserOrdersCountAsync(string userId)
        {
            var count = await FindAllByCondition(
                o => o.UserId == userId && o.OrderStatus != OrderStatus.Failed, 
                false)
                .CountAsync();

            return count;
        }

        public async Task<decimal> GetTotalRevenueAsync()
        {
            var totalRevenue = await FindAllByCondition(
                o => o.PaymentStatus == PaymentStatus.Completed,
                false)
                .SumAsync(o => o.TotalAmount);

            return totalRevenue;
        }

        public async Task<decimal> GetUserTotalSpentAsync(string userId)
        {
            var totalSpent = await FindAllByCondition(
                o => o.UserId == userId && 
                     o.PaymentStatus == PaymentStatus.Completed && 
                     o.OrderStatus != OrderStatus.Failed,
                false)
                .SumAsync(o => o.TotalAmount);

            return totalSpent;
        }

        public async Task<int> GetOrdersInProcessCountAsync()
        {
            var count = await FindAllByCondition(o => o.OrderStatus == OrderStatus.Processing, false)
                .CountAsync();

            return count;
        }

        public async Task<IEnumerable<DailySalesDto>> GetDailySalesAsync(DateTime startDate, DateTime endDate)
        {
            var orders = await FindAllByCondition(
                o => o.OrderedAt >= startDate && o.OrderedAt <= endDate && o.PaymentStatus == PaymentStatus.Completed,
                false)
                .Include(o => o.Lines)
                .ToListAsync();

            var result = orders
                .GroupBy(o => o.OrderedAt.Date)
                .Select(g => new DailySalesDto
                {
                    Date = g.Key,
                    TotalProductsSold = g.Sum(o => o.Lines.Sum(l => l.Quantity)),
                    TotalRevenue = g.Sum(o => o.TotalAmount)
                })
                .OrderBy(x => x.Date)
                .ToList();

            return result;
        }

        public async Task<IEnumerable<ProductSalesDto>> GetTopSellingProductsAsync(int topN)
        {
            var orders = await FindAllByCondition(
                o => o.PaymentStatus == PaymentStatus.Completed,
                false)
                .Include(o => o.Lines)
                .ToListAsync();

            var productSales = orders
                .SelectMany(o => o.Lines)
                .GroupBy(l => new { l.ProductId, l.ProductName })
                .Select(g => new ProductSalesDto
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    TotalQuantitySold = g.Sum(l => l.Quantity),
                    TotalRevenue = g.Sum(l => l.Quantity * l.DiscountPrice ?? l.ActualPrice)
                })
                .OrderByDescending(ps => ps.TotalQuantitySold)
                .Take(topN)
                .ToList();

            return productSales;
        }

        public void CreateOrder(Order order) => Create(order);

        public void UpdateOrder(Order order) => Update(order);

        public void DeleteOrder(Order order) => Remove(order);
    }
}
