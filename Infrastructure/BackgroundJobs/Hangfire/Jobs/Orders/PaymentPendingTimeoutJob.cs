using Application.Repositories.Interfaces;
using Domain.Entities;
using Hangfire;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Infrastructure.BackgroundJobs.Hangfire.Jobs.Orders
{
    public class PaymentPendingTimeoutJob
    {
        private readonly IRepositoryManager _manager;
        private readonly ILogger<PaymentPendingTimeoutJob> _logger;

        public PaymentPendingTimeoutJob(IRepositoryManager manager, ILogger<PaymentPendingTimeoutJob> logger)
        {
            _manager = manager;
            _logger = logger;
        }

        [Queue(Queues.Orders)]
        public async Task ExecuteAsync(int olderThanMinutes = 20, int batchSize = 200, CancellationToken ct = default)
        {
            var threshold = DateTime.UtcNow.AddMinutes(-Math.Abs(olderThanMinutes));
            var expired = await _manager.Order.GetPaymentPendingBeforeAsync(threshold, batchSize, trackChanges: true);
            if (expired.Count == 0)
                return;

            await _manager.ExecuteInTransactionAsync(async token =>
            {
                foreach (var order in expired)
                {
                    if (order.PaymentStatus != PaymentStatus.Pending || order.OrderStatus != OrderStatus.Pending)
                        continue;

                    order.Cancel("Ödeme süresi doldu. Stok otomatik iade edildi.");

                    var historyEntry = OrderHistory.CreateEvent(
                        orderId: order.OrderId,
                        eventType: OrderEventType.Cancelled,
                        description: "Ödeme zaman aşımı nedeniyle sipariş otomatik iptal edildi.",
                        userId: null,
                        isSystemEvent: true
                    );
                    _manager.OrderHistory.Create(historyEntry);

                    foreach (var line in order.Lines)
                    {
                        var variant = await _manager.ProductVariant.GetByIdAsync(line.ProductVariantId, includeImages: false, trackChanges: true);
                        if (variant == null)
                            continue;

                        variant.Stock += line.Quantity;
                        _manager.ProductVariant.Update(variant);
                    }
                }

                _logger.LogInformation("Expired pending orders processed. Count: {Count}, Threshold: {Threshold}", expired.Count, threshold);
            }, IsolationLevel.ReadCommitted, ct);
        }
    }
}
