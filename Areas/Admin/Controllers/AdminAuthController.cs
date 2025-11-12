using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PerfumeStore.Models;

namespace PerfumeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdminAuthController : Controller
    {
        private readonly PerfumeStoreContext _db;
        private readonly ILogger<AdminAuthController> _logger;

        public AdminAuthController(PerfumeStoreContext db, ILogger<AdminAuthController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Tên đăng nhập và mật khẩu là bắt buộc.";
                return View();
            }

            var admin = await _db.Admins
                .Include(a => a.Role)
                .FirstOrDefaultAsync(a => a.UserName == username);

            if (admin == null)
            {
                ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng.";
                return View();
            }

            if (!admin.IsApproved)
            {
                ViewBag.Error = "Tài khoản chưa được phê duyệt.";
                return View();
            }

            if (admin.IsBlocked)
            {
                ViewBag.Error = "Tài khoản đã bị khóa.";
                return View();
            }

            if (string.IsNullOrEmpty(admin.PasswordHash) || !BCrypt.Net.BCrypt.Verify(password, admin.PasswordHash))
            {
                ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng.";
                return View();
            }

            // Tạo claims cho admin
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, admin.AdminId.ToString()),
                new Claim(ClaimTypes.Name, admin.FullName ?? admin.UserName),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("UserName", admin.UserName),
                new Claim("RoleId", admin.RoleId?.ToString() ?? "0")
            };

            if (admin.Role != null)
            {
                claims.Add(new Claim("RoleName", admin.Role.RoleName ?? ""));
            }
            else
            {
                claims.Add(new Claim("RoleName", "Chưa có role"));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties 
            { 
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity), 
                authProperties);

            _logger.LogInformation($"Admin {username} đăng nhập thành công");

            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string fullName, string username, string password, string confirmPassword, DateTime? birthDate, string nationalId)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Tên đăng nhập và mật khẩu là bắt buộc.";
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp.";
                return View();
            }

            if (password.Length < 6)
            {
                ViewBag.Error = "Mật khẩu phải có ít nhất 6 ký tự.";
                return View();
            }

            // Kiểm tra username đã tồn tại
            if (await _db.Admins.AnyAsync(a => a.UserName == username))
            {
                ViewBag.Error = "Tên đăng nhập đã được sử dụng.";
                return View();
            }

            var admin = new PerfumeStore.Models.Admin
            {
                FullName = fullName,
                UserName = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                BirthDate = birthDate,
                NationalId = nationalId,
                IsApproved = false, // Cần admin khác phê duyệt
                IsBlocked = false
            };

            _db.Admins.Add(admin);
            await _db.SaveChangesAsync();

            _logger.LogInformation($"Admin mới đăng ký: {username}");

            ViewBag.Message = "Đăng ký thành công! Vui lòng chờ admin phê duyệt tài khoản.";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
