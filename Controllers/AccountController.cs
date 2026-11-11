
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

using CarWashSystem.Data;
using CarWashSystem.Models;
using CarWashSystem.ViewModels;

namespace CarWashSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;

        public AccountController(AppDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
        }

        // Сторінка реєстрації
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // Обробка реєстрації
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string email = model.Email.Trim().ToLower();

            bool emailExists = await _context.Users
                .AnyAsync(u => u.Email == email);

            if (emailExists)
            {
                ModelState.AddModelError(
                    "Email",
                    "Користувач із такою електронною поштою вже існує."
                );

                return View(model);
            }

            var clientRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == "Client");

            if (clientRole == null)
            {
                ModelState.AddModelError(
                    "",
                    "Роль Client не знайдена в базі даних."
                );

                return View(model);
            }

            var user = new User
            {
                FullName = $"{model.FirstName.Trim()} {model.LastName.Trim()}",
                Email = email,
                Phone = model.Phone,
                RoleId = clientRole.Id,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            user.PasswordHash = _passwordHasher.HashPassword(
                user,
                model.Password
            );

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Реєстрація успішна! Тепер увійдіть у систему.";

            return RedirectToAction("Login");
        }

        // Сторінка входу
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // Обробка входу
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model,
            string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            string email = model.Email.Trim().ToLower();

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || !user.IsActive)
            {
                ModelState.AddModelError(
                    "",
                    "Неправильна електронна пошта або пароль."
                );

                return View(model);
            }

            var result = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                model.Password
            );

            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(
                    "",
                    "Неправильна електронна пошта або пароль."
                );

                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.Name)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            var principal = new ClaimsPrincipal(identity);

            var properties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                properties
            );


            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            if (user.Role.Name == "Administrator")
            {
                return RedirectToAction("Index", "Admin");
            }

            if (user.Role.Name == "Client")
            {
                return RedirectToAction("Index", "Client");
            }

            return RedirectToAction("Login");

        }

        // Вихід із системи
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            return RedirectToAction("Login");
        }

        // Сторінка відмови в доступі
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}

