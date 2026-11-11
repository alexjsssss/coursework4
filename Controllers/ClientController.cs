
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

using CarWashSystem.Data;
using CarWashSystem.Models;
using CarWashSystem.ViewModels;

namespace CarWashSystem.Controllers
{
    [Authorize(Roles = "Client")]
    public class ClientController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;

        public ClientController(AppDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
        }

        // ==========================================
        // ОТРИМАННЯ ID КОРИСТУВАЧА
        // ==========================================

        private int? GetUserId()
        {
            return int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out int id)
                ? id
                : null;
        }

        // ==========================================
        // ГОЛОВНА СТОРІНКА
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();

            if (userId == null)
                return RedirectToAction("Login", "Account");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null)
                return RedirectToAction("Login", "Account");

            var dashboard = new ClientDashboardViewModel
            {
                ClientName = user.FullName,
                Email = user.Email,
                Phone = user.Phone ?? "Не вказано"
            };

            dashboard.OrdersCount = await _context.Orders
                .CountAsync(o => o.UserId == userId.Value);

            dashboard.TotalSpent = await _context.Orders
                .Where(o =>
                    o.UserId == userId.Value &&
                    (o.Status == "paid" ||
                     o.Status == "in_progress" ||
                     o.Status == "completed"))
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            dashboard.WashingBays = await _context.WashingBays
                .OrderBy(b => b.Id)
                .ToListAsync();

            dashboard.TotalBays = dashboard.WashingBays.Count;

            dashboard.AvailableBays = dashboard.WashingBays
                .Count(b => b.Status == "available");

            dashboard.occupiedBays =
                dashboard.TotalBays - dashboard.AvailableBays;

            dashboard.RecentOrders = await _context.Orders
                .Where(o => o.UserId == userId.Value)
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .Select(o => new ClientOrderViewModel
                {
                    Id = o.Id,
                    CreatedAt = o.CreatedAt,
                    Status = o.Status,
                    TotalAmount = o.TotalAmount
                })
                .ToListAsync();

            return View(dashboard);
        }

        // ==========================================
        // ІСТОРІЯ ЗАМОВЛЕНЬ
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> History()
        {
            var userId = GetUserId();

            if (userId == null)
                return RedirectToAction("Login", "Account");

            var orders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.WashingBay)
                .Where(o => o.UserId == userId.Value)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        // ==========================================
        // МІЙ ПРОФІЛЬ
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = GetUserId();

            if (userId == null)
                return RedirectToAction("Login", "Account");

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null)
                return NotFound();

            var model = new ClientProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone ?? ""
            };

            return View(model);
        }

        // ==========================================
        // РЕДАГУВАННЯ ПРОФІЛЮ
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(
            UpdateClientProfileViewModel model)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            if (!ModelState.IsValid)
            {
                TempData["ProfileError"] =
                    GetValidationErrors();

                return RedirectToAction(nameof(Profile));
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null)
                return NotFound();

            var fullName = model.FullName.Trim();
            var email = model.Email.Trim().ToLowerInvariant();
            var phone = model.Phone?.Trim();

            // Перевірка унікальності Email
            var emailExists = await _context.Users
                .AnyAsync(u =>
                    u.Email == email &&
                    u.Id != userId.Value);

            if (emailExists)
            {
                TempData["ProfileError"] =
                    "Ця електронна пошта вже використовується.";

                return RedirectToAction(nameof(Profile));
            }

            // Оновлення інформації
            user.FullName = fullName;
            user.Email = email;

            user.Phone = string.IsNullOrWhiteSpace(phone)
                ? null
                : phone;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["ProfileError"] =
                    "Не вдалося зберегти зміни. Перевірте введені дані.";

                return RedirectToAction(nameof(Profile));
            }

            // Оновлення інформації у cookie авторизації
            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.Id.ToString()
                ),

                new Claim(
                    ClaimTypes.Name,
                    user.FullName
                ),

                new Claim(
                    ClaimTypes.Email,
                    user.Email
                ),

                new Claim(
                    ClaimTypes.Role,
                    "Client"
                )
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            var principal = new ClaimsPrincipal(identity);

            // Зберігаємо налаштування поточної сесії
            var authResult = await HttpContext.AuthenticateAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            var properties = authResult.Succeeded
                ? authResult.Properties
                : null;

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                properties ?? new AuthenticationProperties()
            );

            TempData["ProfileSuccess"] =
                "Інформацію профілю успішно оновлено.";

            return RedirectToAction(nameof(Profile));
        }

        // ==========================================
        // ЗМІНА ПАРОЛЯ
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            ChangeClientPasswordViewModel model)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            if (!ModelState.IsValid)
            {
                TempData["PasswordError"] =
                    GetValidationErrors();

                return RedirectToAction(nameof(Profile));
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null)
                return NotFound();

            // Перевірка поточного пароля
            var verification = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                model.CurrentPassword
            );

            if (verification == PasswordVerificationResult.Failed)
            {
                TempData["PasswordError"] =
                    "Поточний пароль введено неправильно.";

                return RedirectToAction(nameof(Profile));
            }

            // Перевірка нового пароля
            if (model.CurrentPassword == model.NewPassword)
            {
                TempData["PasswordError"] =
                    "Новий пароль повинен відрізнятися від поточного.";

                return RedirectToAction(nameof(Profile));
            }

            if (model.NewPassword != model.ConfirmPassword)
            {
                TempData["PasswordError"] =
                    "Новий пароль і підтвердження не збігаються.";

                return RedirectToAction(nameof(Profile));
            }

            // Хешування нового пароля
            user.PasswordHash = _passwordHasher.HashPassword(
                user,
                model.NewPassword
            );

            await _context.SaveChangesAsync();

            TempData["ProfileSuccess"] =
                "Пароль успішно змінено.";

            return RedirectToAction(nameof(Profile));
        }

        // ==========================================
        // ОТРИМАННЯ ПОМИЛОК ВАЛІДАЦІЇ
        // ==========================================

        private string GetValidationErrors()
        {
            return string.Join(
                " ",
                ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(message =>
                        !string.IsNullOrWhiteSpace(message))
            );
        }

        // ==========================================
        // ПОСЛУГИ
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Services()
        {
            var model = new ClientServicesViewModel
            {
                Services = await _context.Services
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.Id)
                    .ToListAsync(),

                AvailableBays = await _context.WashingBays
                    .Where(b => b.Status == "available")
                    .OrderBy(b => b.Id)
                    .ToListAsync()
            };

            return View(model);
        }

        // ==========================================
        // СТВОРЕННЯ ЗАМОВЛЕННЯ
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrder(
            [FromBody] CreateOrderViewModel model)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            if (model == null ||
                !ModelState.IsValid ||
                model.ServiceIds == null ||
                model.ServiceIds.Count == 0)
            {
                return BadRequest(new
                {
                    message = "Оберіть хоча б одну послугу."
                });
            }

            var serviceIds = model.ServiceIds
                .Distinct()
                .ToList();

            var services = await _context.Services
                .Where(s =>
                    serviceIds.Contains(s.Id) &&
                    s.IsActive)
                .ToListAsync();

            if (services.Count != serviceIds.Count)
            {
                return BadRequest(new
                {
                    message = "Деякі послуги недоступні."
                });
            }

            var bayExists = await _context.WashingBays
                .AnyAsync(b => b.Id == model.WashingBayId);

            if (!bayExists)
            {
                return BadRequest(new
                {
                    message = "Мийний бокс не знайдено."
                });
            }

            var order = new Order
            {
                UserId = userId.Value,
                WashingBayId = model.WashingBayId,
                CreatedAt = DateTime.UtcNow,
                Status = "pending",
                TotalAmount = services.Sum(s => s.Price)
            };

            foreach (var service in services)
            {
                order.OrderItems.Add(new OrderItem
                {
                    ServiceId = service.Id,
                    Quantity = 1,
                    UnitPrice = service.Price
                });
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // Резервуємо лише вільний бокс
                var reserved = await _context.WashingBays
                    .Where(b =>
                        b.Id == model.WashingBayId &&
                        b.Status == "available")
                    .ExecuteUpdateAsync(setters =>
                        setters.SetProperty(
                            b => b.Status,
                            "occupied"
                        ));

                if (reserved != 1)
                {
                    await transaction.RollbackAsync();

                    return Conflict(new
                    {
                        message =
                            "Бокс уже зайнятий. Оберіть інший."
                    });
                }

                _context.Orders.Add(order);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();

                return StatusCode(500, new
                {
                    message =
                        "Не вдалося створити замовлення."
                });
            }

            return Ok(new
            {
                orderId = order.Id,

                redirectUrl = Url.Action(
                    "Payment",
                    "Client",
                    new { id = order.Id }
                )
            });
        }

        // ==========================================
        // СТОРІНКА ОПЛАТИ
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Payment(int id)
        {
            var userId = GetUserId();

            if (userId == null)
                return RedirectToAction("Login", "Account");

            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .Include(o => o.WashingBay)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Service)
                .FirstOrDefaultAsync(o =>
                    o.Id == id &&
                    o.UserId == userId.Value);

            if (order == null)
                return NotFound();

            if (order.Status == "paid" ||
                order.Status == "completed")
            {
                return RedirectToAction(
                    "Confirmation",
                    new { id }
                );
            }

            // Неоплачене замовлення дійсне 5 хвилин
            if (order.Status != "pending" ||
                order.CreatedAt == null ||
                order.CreatedAt.Value <=
                    DateTime.UtcNow.AddMinutes(-5))
            {
                return RedirectToAction("Services");
            }

            var model = new PaymentViewModel
            {
                OrderId = order.Id,
                ClientName = order.User.FullName,
                WashingBayName = order.WashingBay.Name,
                TotalAmount = order.TotalAmount,

                TotalDuration = order.OrderItems.Sum(i =>
                    i.Service.DurationMinutes * i.Quantity),

                Status = order.Status,
                CreatedAt = order.CreatedAt,

                Services = order.OrderItems.Select(i =>
                    new PaymentServiceViewModel
                    {
                        Name = i.Service.Name,

                        DurationMinutes =
                            i.Service.DurationMinutes * i.Quantity,

                        Price = i.UnitPrice * i.Quantity
                    }).ToList()
            };

            return View(model);
        }

        // ==========================================
        // ОПЛАТА ЗАМОВЛЕННЯ
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompletePayment(int id)
        {
            var userId = GetUserId();

            if (userId == null)
                return RedirectToAction("Login", "Account");

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var order = await _context.Orders
                    .AsNoTracking()
                    .Include(o => o.Payment)
                    .FirstOrDefaultAsync(o =>
                        o.Id == id &&
                        o.UserId == userId.Value);

                if (order == null)
                {
                    await transaction.RollbackAsync();
                    return NotFound();
                }

                if (order.Status == "paid" ||
                    order.Status == "completed")
                {
                    await transaction.RollbackAsync();

                    return RedirectToAction(
                        "Confirmation",
                        new { id }
                    );
                }

                if (order.Status != "pending" ||
                    order.Payment != null)
                {
                    await transaction.RollbackAsync();

                    return BadRequest(
                        "Це замовлення неможливо оплатити."
                    );
                }

                var now = DateTime.UtcNow;

                // Оплата можлива тільки протягом 5 хвилин
                var updated = await _context.Orders
                    .Where(o =>
                        o.Id == id &&
                        o.UserId == userId.Value &&
                        o.Status == "pending" &&
                        o.CreatedAt != null &&
                        o.CreatedAt > now.AddMinutes(-5))
                    .ExecuteUpdateAsync(setters =>
                        setters.SetProperty(
                            o => o.Status,
                            "paid"
                        ));

                if (updated != 1)
                {
                    await transaction.RollbackAsync();

                    return BadRequest(
                        "Час оплати минув. Створіть нове замовлення."
                    );
                }

                _context.Payments.Add(new Payment
                {
                    OrderId = order.Id,
                    Amount = order.TotalAmount,
                    Method = "simulated",
                    Status = "successful",
                    PaidAt = now
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return RedirectToAction(
                    "Confirmation",
                    new { id }
                );
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();

                return StatusCode(
                    500,
                    "Не вдалося виконати оплату."
                );
            }
        }

        // ==========================================
        // ПІДТВЕРДЖЕННЯ ОПЛАТИ
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Confirmation(int id)
        {
            var userId = GetUserId();

            if (userId == null)
                return RedirectToAction("Login", "Account");

            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .Include(o => o.WashingBay)
                .Include(o => o.Payment)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Service)
                .FirstOrDefaultAsync(o =>
                    o.Id == id &&
                    o.UserId == userId.Value);

            if (order == null)
                return NotFound();

            if ((order.Status != "paid" &&
                 order.Status != "completed") ||
                order.Payment == null ||
                order.Payment.Status != "successful")
            {
                return RedirectToAction("Index");
            }

            var model = new PaymentViewModel
            {
                OrderId = order.Id,
                ClientName = order.User.FullName,
                WashingBayName = order.WashingBay.Name,
                TotalAmount = order.TotalAmount,

                TotalDuration = order.OrderItems.Sum(i =>
                    i.Service.DurationMinutes * i.Quantity),

                Status = order.Status,

                // Фактичний час оплати
                CreatedAt = order.Payment.PaidAt,

                Services = order.OrderItems.Select(i =>
                    new PaymentServiceViewModel
                    {
                        Name = i.Service.Name,

                        DurationMinutes =
                            i.Service.DurationMinutes * i.Quantity,

                        Price = i.UnitPrice * i.Quantity
                    }).ToList()
            };

            return View(model);
        }

        // ==========================================
        // СКАСУВАННЯ ЗАМОВЛЕННЯ
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var userId = GetUserId();

            if (userId == null)
                return RedirectToAction("Login", "Account");

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var order = await _context.Orders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o =>
                        o.Id == id &&
                        o.UserId == userId.Value);

                if (order == null)
                {
                    await transaction.RollbackAsync();
                    return NotFound();
                }

                if (order.Status != "pending")
                {
                    await transaction.RollbackAsync();

                    return RedirectToAction("Services");
                }

                var updated = await _context.Orders
                    .Where(o =>
                        o.Id == id &&
                        o.UserId == userId.Value &&
                        o.Status == "pending")
                    .ExecuteUpdateAsync(setters =>
                        setters.SetProperty(
                            o => o.Status,
                            "cancelled"
                        ));

                if (updated != 1)
                {
                    await transaction.RollbackAsync();

                    return Conflict(
                        "Статус замовлення вже змінився."
                    );
                }

                // Перевіряємо інші активні замовлення боксу
                var otherActiveOrder = await _context.Orders
                    .AnyAsync(o =>
                        o.WashingBayId == order.WashingBayId &&
                        o.Id != order.Id &&
                        (o.Status == "pending" ||
                         o.Status == "paid" ||
                         o.Status == "in_progress"));

                // Звільняємо бокс, якщо немає інших замовлень
                if (!otherActiveOrder)
                {
                    await _context.WashingBays
                        .Where(b =>
                            b.Id == order.WashingBayId &&
                            b.Status == "occupied")
                        .ExecuteUpdateAsync(setters =>
                            setters.SetProperty(
                                b => b.Status,
                                "available"
                            ));
                }

                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();

                return StatusCode(
                    500,
                    "Не вдалося скасувати замовлення."
                );
            }

            return RedirectToAction("Services");
        }
    }
}
