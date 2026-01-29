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

        public async Task<IEnumerable<Order>> GetAllOrdersAsync()
        {
            var orders = await FindAll(false)
            .Include(o => o.Lines)
            .OrderByDescending(o => o.OrderStatus == OrderStatus.Shipped)
            .ToListAsync();

            return orders;
        }

        public async Task<int> GetNumberOfInProcessAsync() => await FindAllByCondition(o => o.OrderStatus != OrderStatus.Shipped, false).CountAsync();

        public async Task CompleteAsync(int id)
        {
            var order = await FindByCondition(O => O.OrderId.Equals(id), true).SingleOrDefaultAsync();
            if (order is null)
            {
                throw new Exception("Order could not found");
            }
            order.OrderStatus = OrderStatus.Shipped;
        }

        public async Task<Order?> GetOneOrderAsync(int id)
        {
            return await FindByCondition(o => o.OrderId.Equals(id), false).SingleOrDefaultAsync();
        }

        public async Task<IEnumerable<Order>> GetOrdersOfUserAsync(string? userId)
        {
            return await FindAllByCondition(o => o.UserId == userId, false)
                .Include(mc => mc.Lines)
                .ToListAsync();
        }

        public async Task SaveOrderAsync(Order order)
        {
            _context.AttachRange(order.Lines.Select(l => l));
            if (order.OrderId == 0)
            {
                await _context.Orders.AddAsync(order);
            }
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<DailySalesDto>> GetLast30DaysSalesDataAsync()
        {
            var today = DateTime.UtcNow.Date;
            var startDate = today.AddDays(-30);

            var orders = await FindAllByCondition(o => o.OrderedAt >= startDate && o.OrderedAt <= today, false)
                                .Include(o => o.Lines)
                                    .ThenInclude(l => l.Product)
                                .ToListAsync();

            var result = orders
                .GroupBy(o => o.OrderedAt.Date)
                .Select(g => new DailySalesDto
                {
                    Date = g.Key,
                    TotalProductsSold = g.Sum(o => o.Lines.Sum(l => l.Quantity)),

                    // Ürün fiyatlarını Product üzerinden hesaplıyoruz
                    TotalRevenue = g.Sum(o => o.Lines.Sum(l =>
                        ((l.Product?.DiscountPrice ?? l.Product?.ActualPrice) ?? 0) * l.Quantity))
                })
                .OrderBy(x => x.Date)
                .ToList();

            return result;
        }


    }
}
