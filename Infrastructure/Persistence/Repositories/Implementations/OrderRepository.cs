using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Application.Repositories.Interfaces;
using Application.DTOs;
using Application.Queries.RequestParameters;
using Infrastructure.Persistence.Extensions;

namespace Infrastructure.Persistence.Repositories.Implementations
{
    public sealed class OrderRepository : RepositoryBase<Order>, IOrderRepository
    {
        public OrderRepository(RepositoryContext context) : base(context)
        {
        }

        public async Task<(IEnumerable<Order> orders, int count, int processingCount)> GetAllAdminAsync(OrderRequestParametersAdmin p, bool trackChanges, CancellationToken ct = default)
        {
            var ordersQuery = FindAll(trackChanges)
                .FilterBy(p.Status, o => o.OrderStatus, FilterOperator.Equal)
                .FilterBy(p.PaymentStatus, o => o.PaymentStatus, FilterOperator.Equal)
                .FilterBy(p.StartDate, o => o.OrderedAt, FilterOperator.GreaterThanOrEqual)
                .FilterBy(p.EndDate, o => o.OrderedAt, FilterOperator.LessThanOrEqual);

            if (!string.IsNullOrWhiteSpace(p.SearchTerm))
            {
                var lowerTerm = p.SearchTerm.ToLower();
                ordersQuery = ordersQuery.Where(o => 
                    o.OrderNumber.ToLower().Contains(lowerTerm) || 
                    (o.FirstName + " " + o.LastName).ToLower().Contains(lowerTerm));
            }

            var count = await ordersQuery.CountAsync(ct);
            var processingCount = await ordersQuery.CountAsync(o => o.OrderStatus == OrderStatus.Processing, ct);

            ordersQuery = p.SortBy switch
            {
                "date_asc" => ordersQuery.OrderBy(o => o.OrderedAt),
                "date_desc" => ordersQuery.OrderByDescending(o => o.OrderedAt),
                "amount_asc" => ordersQuery.OrderBy(o => o.TotalAmount),
                "amount_desc" => ordersQuery.OrderByDescending(o => o.TotalAmount),
                _ => ordersQuery.OrderByDescending(o => o.OrderedAt)
            };

            var orders = await ordersQuery
                .ToPaginate(p.PageNumber, p.PageSize)
                .ToListAsync(ct);

            return (orders, count, processingCount);
        }

        public async Task<Order?> GetByIdAsync(int orderId, bool trackChanges)
        {
            var order = await FindByCondition(o => o.OrderId == orderId, trackChanges)
                .Include(o => o.Lines)
                .FirstOrDefaultAsync();

            return order;
        }

        public async Task<Order?> GetByNumberAsync(string orderNumber, bool trackChanges)
        {
            var order = await FindByCondition(o => o.OrderNumber == orderNumber, trackChanges)
                .Include(o => o.Lines)
                .FirstOrDefaultAsync();

            return order;
        }

        public async Task<Order?> GetWithDetailsAsync(int orderId, bool trackChanges)
        {
            var order = await FindByCondition(o => o.OrderId == orderId, trackChanges)
                .AsSplitQuery()
                .Include(o => o.Lines)
                .Include(o => o.AppliedCampaigns)
                .Include(o => o.History)
                    .ThenInclude(h => h.CreatedByUser)
                .FirstOrDefaultAsync();

            return order;
        }

        public async Task<IEnumerable<Order>> GetByUserIdAsync(string userId, bool trackChanges)
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

        public async Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status, bool trackChanges)
        {
            var orders = await FindAllByCondition(o => o.OrderStatus == status, trackChanges)
                .Include(o => o.Lines)
                .OrderByDescending(o => o.OrderedAt)
                .ToListAsync();

            return orders;
        }

        public async Task<IEnumerable<Order>> GetByPaymentStatusAsync(PaymentStatus status, bool trackChanges)
        {
            var orders = await FindAllByCondition(o => o.PaymentStatus == status, trackChanges)
                .Include(o => o.Lines)
                .OrderByDescending(o => o.OrderedAt)
                .ToListAsync();

            return orders;
        }

        public async Task<(IEnumerable<Order> orders, int count)> GetPagedAsync(int pageNumber, int pageSize, bool trackChanges)
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

        public async Task<IEnumerable<Order>> GetPaymentPendingAsync(bool trackChanges)
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

        public async Task<IReadOnlyList<Order>> GetPaymentPendingBeforeAsync(DateTime utcBefore, int take, bool trackChanges)
        {
            return await FindAllByCondition(
                    o => o.PaymentStatus == PaymentStatus.Pending &&
                         o.OrderStatus == OrderStatus.Pending &&
                         o.OrderedAt <= utcBefore,
                    trackChanges)
                .Include(o => o.Lines)
                .OrderBy(o => o.OrderedAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> CountAsync()
        {
            var count = await CountAsync(false);

            return count;
        }

        public async Task<int> CountByUserIdAsync(string userId)
        {
            var count = await FindAllByCondition(
                o => o.UserId == userId && o.OrderStatus != OrderStatus.Failed, 
                false)
                .CountAsync();

            return count;
        }

        public async Task<int> CountOfInProcessAsync(CancellationToken ct = default)
        {
            var count = await FindAllByCondition(o => o.OrderStatus == OrderStatus.Processing, false)
                .CountAsync(ct);

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

        public async Task<IEnumerable<ProductSalesDto>> GetTopSellingProductsAsync(int topN, CancellationToken ct = default)
        {
            var orders = await FindAllByCondition(
                o => o.PaymentStatus == PaymentStatus.Completed,
                false)
                .Include(o => o.Lines)
                .ToListAsync(ct);

            var productSales = orders
                .SelectMany(o => o.Lines)
                .GroupBy(l => new { l.ProductId, l.ProductName })
                .Select(g => new ProductSalesDto
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    TotalQuantitySold = g.Sum(l => l.Quantity),
                    TotalRevenue = g.Sum(l => l.Quantity * l.DiscountPrice ?? l.Price)
                })
                .OrderByDescending(ps => ps.TotalQuantitySold)
                .Take(topN)
                .ToList();

            return productSales;
        }

        public void Create(Order order) => CreateEntity(order);

        public void Update(Order order) => UpdateEntity(order);

        public void Delete(Order order) => RemoveEntity(order);
    }
}
