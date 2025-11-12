using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerfumeStore.Models;

namespace PerfumeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SetupController : Controller
    {
        private readonly PerfumeStoreContext _db;
        private readonly ILogger<SetupController> _logger;

        public SetupController(PerfumeStoreContext db, ILogger<SetupController> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Tạo admin đầu tiên - chỉ chạy khi chưa có admin nào
        /// URL: /Admin/Setup/CreateFirstAdmin
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CreateFirstAdmin()
        {
            // Kiểm tra xem đã có admin nào chưa
            var hasAdmin = await _db.Admins.AnyAsync();
            
            if (hasAdmin)
            {
                return Content("Hệ thống đã có admin. Không thể tạo admin đầu tiên nữa.");
            }

            // Tạo admin đầu tiên
            var admin = new PerfumeStore.Models.Admin
            {
                FullName = "Super Admin",
                UserName = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"), // Mật khẩu: admin123
                IsApproved = true,
                IsBlocked = false,
                BirthDate = null,
                NationalId = null,
                RoleId = null
            };

            _db.Admins.Add(admin);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Đã tạo admin đầu tiên: admin / admin123");

            return Content(@"
                <h2>✅ Tạo admin đầu tiên thành công!</h2>
                <p><strong>Tên đăng nhập:</strong> admin</p>
                <p><strong>Mật khẩu:</strong> admin123</p>
                <p><a href='/Admin/AdminAuth/Login'>Đăng nhập ngay</a></p>
                <p style='color: red;'><strong>Lưu ý:</strong> Vui lòng đổi mật khẩu sau khi đăng nhập lần đầu!</p>
            ", "text/html");
        }

        /// <summary>
        /// Phê duyệt tất cả admin đang chờ
        /// URL: /Admin/Setup/ApproveAllPending
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ApproveAllPending()
        {
            var pendingAdmins = await _db.Admins
                .Where(a => !a.IsApproved)
                .ToListAsync();

            if (!pendingAdmins.Any())
            {
                return Content("Không có admin nào đang chờ phê duyệt.");
            }

            foreach (var admin in pendingAdmins)
            {
                admin.IsApproved = true;
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation($"Đã phê duyệt {pendingAdmins.Count} admin");

            return Content($"Đã phê duyệt {pendingAdmins.Count} admin: {string.Join(", ", pendingAdmins.Select(a => a.UserName))}");
        }

        /// <summary>
        /// Reset mật khẩu admin
        /// URL: /Admin/Setup/ResetPassword?username=admin&newPassword=123456
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ResetPassword(string username, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(newPassword))
            {
                return Content("Thiếu tham số username hoặc newPassword");
            }

            var admin = await _db.Admins.FirstOrDefaultAsync(a => a.UserName == username);
            if (admin == null)
            {
                return Content($"Không tìm thấy admin với username: {username}");
            }

            admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _db.SaveChangesAsync();

            _logger.LogInformation($"Đã reset mật khẩu cho admin: {username}");

            return Content($"Đã reset mật khẩu cho admin '{username}' thành: {newPassword}");
        }
    }
}
