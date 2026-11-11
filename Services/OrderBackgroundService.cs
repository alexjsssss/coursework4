
using CarWashSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace CarWashSystem.Services
{
    public class OrderBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OrderBackgroundService> _logger;

        public OrderBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<OrderBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(
                TimeSpan.FromSeconds(15));

            await CheckOrdersAsync(stoppingToken);

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await CheckOrdersAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                // Застосунок зупинено.
            }
        }

        private async Task CheckOrdersAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var db = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                await CancelExpiredOrdersAsync(
                    db, cancellationToken);

                await CompleteFinishedOrdersAsync(
                    db, cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Помилка автоматичної перевірки замовлень.");
            }
        }

        // СКАСУВАННЯ НЕОПЛАЧЕНИХ ЗАМОВЛЕНЬ
        private async Task CancelExpiredOrdersAsync(
            AppDbContext db,
            CancellationToken token)
        {
            var now = DateTime.UtcNow;

            var expiredOrders = await db.Orders
                .AsNoTracking()
                .Where(o =>
                    o.Status == "pending" &&
                    o.CreatedAt != null &&
                    o.CreatedAt <= now.AddMinutes(-5))
                .ToListAsync(token);

            foreach (var order in expiredOrders)
            {
                await using var transaction =
                    await db.Database.BeginTransactionAsync(token);

                var updated = await db.Orders
                    .Where(o =>
                        o.Id == order.Id &&
                        o.Status == "pending" &&
                        o.CreatedAt != null &&
                        o.CreatedAt <= DateTime.UtcNow.AddMinutes(-5))
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(
                            o => o.Status, "cancelled"),
                        token);

                if (updated == 1)
                {
                    var otherActiveOrder = await db.Orders
                        .AnyAsync(o =>
                            o.WashingBayId == order.WashingBayId &&
                            o.Id != order.Id &&
                            (o.Status == "pending" ||
                             o.Status == "paid" ||
                             o.Status == "in_progress"),
                            token);

                    if (!otherActiveOrder)
                    {
                        await db.WashingBays
                            .Where(b =>
                                b.Id == order.WashingBayId &&
                                b.Status == "occupied")
                            .ExecuteUpdateAsync(
                                setters => setters.SetProperty(
                                    b => b.Status, "available"),
                                token);
                    }
                }

                await transaction.CommitAsync(token);
            }
        }

        // ЗАВЕРШЕННЯ ОПЛАЧЕНОГО МИТТЯ
        private async Task CompleteFinishedOrdersAsync(
            AppDbContext db,
            CancellationToken token)
        {
            var now = DateTime.UtcNow;

            var paidOrders = await db.Orders
                .AsNoTracking()
                .Where(o => o.Status == "paid")
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Service)
                .Include(o => o.Payment)
                .ToListAsync(token);

            foreach (var order in paidOrders)
            {
                if (order.Payment?.PaidAt == null)
                    continue;

                int duration = order.OrderItems.Sum(i =>
                    i.Service.DurationMinutes * i.Quantity);

                var finishTime = order.Payment.PaidAt.Value
                    .AddMinutes(duration);

                if (finishTime > now)
                    continue;

                await using var transaction =
                    await db.Database.BeginTransactionAsync(token);

                var updated = await db.Orders
                    .Where(o =>
                        o.Id == order.Id &&
                        o.Status == "paid")
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(
                            o => o.Status, "completed"),
                        token);

                if (updated == 1)
                {
                    var otherActiveOrder = await db.Orders
                        .AnyAsync(o =>
                            o.WashingBayId == order.WashingBayId &&
                            o.Id != order.Id &&
                            (o.Status == "pending" ||
                             o.Status == "paid" ||
                             o.Status == "in_progress"),
                            token);

                    if (!otherActiveOrder)
                    {
                        await db.WashingBays
                            .Where(b =>
                                b.Id == order.WashingBayId &&
                                b.Status == "occupied")
                            .ExecuteUpdateAsync(
                                setters => setters.SetProperty(
                                    b => b.Status, "available"),
                                token);
                    }
                }

                await transaction.CommitAsync(token);
            }
        }
    }
}
