
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CarWashSystem.Data;
using CarWashSystem.Models;
using System.Text.RegularExpressions;

namespace CarWashSystem.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        private static readonly string[] AllowedIcons =
        {
            "prewash", "water", "foam", "osmosis",
            "wax", "sparkles", "brush", "shield",
            "droplet"
        };

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        private static int BayNumber(WashingBay bay)
        {
            var match = Regex.Match(bay.Name ?? "", @"\d+");

            return match.Success &&
                   int.TryParse(match.Value, out var number)
                ? number
                : int.MaxValue;
        }

        private async Task<List<WashingBay>> GetSortedBays()
        {
            var bays = await _context.WashingBays
                .AsNoTracking()
                .ToListAsync();

            return bays
                .OrderBy(BayNumber)
                .ThenBy(b => b.Id)
                .ToList();
        }

        // ГОЛОВНА
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var zone = TimeZoneInfo
                .FindSystemTimeZoneById("Europe/Kyiv");

            var today = TimeZoneInfo
                .ConvertTimeFromUtc(DateTime.UtcNow, zone)
                .Date;

            var start = TimeZoneInfo.ConvertTimeToUtc(today, zone);
            var end = TimeZoneInfo.ConvertTimeToUtc(
                today.AddDays(1), zone);

            ViewBag.TotalRevenue = await _context.Orders
                .Where(o =>
                    o.Status == "paid" ||
                    o.Status == "in_progress" ||
                    o.Status == "completed")
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            ViewBag.TodayOrders = await _context.Orders
                .CountAsync(o =>
                    o.CreatedAt >= start &&
                    o.CreatedAt < end);

            ViewBag.TotalOrders =
                await _context.Orders.CountAsync();

            ViewBag.Bays = await GetSortedBays();

            var orders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .Include(o => o.WashingBay)
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .ToListAsync();

            return View(orders);
        }

        // ЗАМОВЛЕННЯ
        [HttpGet]
        public async Task<IActionResult> Orders()
        {
            var orders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .Include(o => o.WashingBay)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteOrder(int id)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                    return NotFound();

                if (order.Status != "paid" &&
                    order.Status != "in_progress")
                {
                    TempData["AdminError"] =
                        "Це замовлення не можна завершити.";

                    return RedirectToAction(nameof(Orders));
                }

                order.Status = "completed";
                await _context.SaveChangesAsync();

                bool hasActive = await _context.Orders
                    .AnyAsync(o =>
                        o.Id != id &&
                        o.WashingBayId == order.WashingBayId &&
                        (o.Status == "pending" ||
                         o.Status == "paid" ||
                         o.Status == "in_progress"));

                if (!hasActive)
                {
                    var bay = await _context.WashingBays
                        .FirstOrDefaultAsync(b =>
                            b.Id == order.WashingBayId);

                    if (bay != null &&
                        bay.Status == "occupied")
                    {
                        bay.Status = "available";
                        await _context.SaveChangesAsync();
                    }
                }

                await transaction.CommitAsync();

                TempData["AdminSuccess"] =
                    "Замовлення завершено.";
            }
            catch
            {
                await transaction.RollbackAsync();

                TempData["AdminError"] =
                    "Не вдалося завершити замовлення.";
            }

            return RedirectToAction(nameof(Orders));
        }

        // ПОСЛУГИ ТА БОКСИ
        [HttpGet]
        public async Task<IActionResult> Services()
        {
            ViewBag.Bays = await GetSortedBays();

            var services = await _context.Services
                .AsNoTracking()
                .OrderBy(s => s.Id)
                .ToListAsync();

            return View(services);
        }

        // ДОДАТИ ПОСЛУГУ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddService(
            string name,
            string? description,
            decimal price,
            int durationMinutes,
            string icon)
        {
            if (string.IsNullOrWhiteSpace(name) ||
                name.Length > 100 ||
                price < 0 ||
                durationMinutes <= 0 ||
                !AllowedIcons.Contains(icon))
            {
                TempData["AdminError"] =
                    "Перевірте дані нової послуги.";

                return RedirectToAction(nameof(Services));
            }

            var service = new Service
            {
                Name = name.Trim(),
                Description = description?.Trim() ?? "",
                Price = price,
                DurationMinutes = durationMinutes,
                Icon = icon,
                IsActive = true
            };

            _context.Services.Add(service);
            await _context.SaveChangesAsync();

            TempData["AdminSuccess"] =
                "Послугу додано.";

            return RedirectToAction(nameof(Services));
        }

        // РЕДАГУВАТИ ПОСЛУГУ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateService(
            int id,
            string name,
            string? description,
            decimal price,
            int durationMinutes,
            string icon)
        {
            var service = await _context.Services
                .FirstOrDefaultAsync(s => s.Id == id);

            if (service == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(name) ||
                name.Length > 100 ||
                price < 0 ||
                durationMinutes <= 0 ||
                !AllowedIcons.Contains(icon))
            {
                TempData["AdminError"] =
                    "Перевірте введені дані.";

                return RedirectToAction(nameof(Services));
            }

            service.Name = name.Trim();
            service.Description = description?.Trim() ?? "";
            service.Price = price;
            service.DurationMinutes = durationMinutes;
            service.Icon = icon;

            await _context.SaveChangesAsync();

            TempData["AdminSuccess"] =
                "Зміни послуги збережено.";

            return RedirectToAction(nameof(Services));
        }

        // УВІМКНУТИ АБО ВИМКНУТИ ПОСЛУГУ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleService(int id)
        {
            var service = await _context.Services
                .FirstOrDefaultAsync(s => s.Id == id);

            if (service == null)
                return NotFound();

            service.IsActive = !service.IsActive;
            await _context.SaveChangesAsync();

            TempData["AdminSuccess"] =
                "Доступність послуги змінено.";

            return RedirectToAction(nameof(Services));
        }

        // ВИДАЛИТИ ПОСЛУГУ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteService(int id)
        {
            var service = await _context.Services
                .FirstOrDefaultAsync(s => s.Id == id);

            if (service == null)
                return NotFound();

            bool usedInOrders = await _context.Orders
                .SelectMany(o => o.OrderItems)
                .AnyAsync(item => item.ServiceId == id);

            if (usedInOrders)
            {
                TempData["AdminError"] =
                    "Цю послугу вже використовували в замовленнях. " +
                    "Щоб зберегти історію, її можна лише вимкнути.";

                return RedirectToAction(nameof(Services));
            }

            _context.Services.Remove(service);
            await _context.SaveChangesAsync();

            TempData["AdminSuccess"] =
                "Послугу видалено.";

            return RedirectToAction(nameof(Services));
        }

        // ВИМКНУТИ АБО УВІМКНУТИ БОКС
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBay(int id)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            try
            {
                var bay = await _context.WashingBays
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (bay == null)
                    return NotFound();

                if (bay.Status == "occupied")
                {
                    TempData["AdminError"] =
                        "Зайнятий бокс не можна вимкнути.";

                    return RedirectToAction(nameof(Services));
                }

                bool hasActiveOrder = await _context.Orders
                    .AnyAsync(o =>
                        o.WashingBayId == id &&
                        (o.Status == "pending" ||
                         o.Status == "paid" ||
                         o.Status == "in_progress"));

                if (hasActiveOrder)
                {
                    TempData["AdminError"] =
                        "Бокс має активне замовлення.";

                    return RedirectToAction(nameof(Services));
                }

                if (bay.Status == "available")
                    bay.Status = "unavailable";
                else if (bay.Status == "unavailable")
                    bay.Status = "available";
                else
                {
                    TempData["AdminError"] =
                        "Невідомий статус боксу.";

                    return RedirectToAction(nameof(Services));
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["AdminSuccess"] =
                    "Стан боксу змінено.";
            }
            catch
            {
                await transaction.RollbackAsync();

                TempData["AdminError"] =
                    "Не вдалося змінити стан боксу. Спробуйте ще раз.";
            }

            return RedirectToAction(nameof(Services));
        }
    }
}

