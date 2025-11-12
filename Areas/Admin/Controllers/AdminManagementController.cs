using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerfumeStore.Models;
using PerfumeStore.Areas.Admin.Filters;

namespace PerfumeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class AdminManagementController : Controller
    {
        private readonly PerfumeStoreContext _db;
        private readonly ILogger<AdminManagementController> _logger;

        public AdminManagementController(PerfumeStoreContext db, ILogger<AdminManagementController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [RequirePermission("View Admins")]
        public async Task<IActionResult> Index()
        {
            var admins = await _db.Admins
                .Include(a => a.Role)
                .OrderByDescending(a => a.AdminId)
                .ToListAsync();

            // Load roles for dropdown
            ViewBag.Roles = await _db.Roles
                .OrderBy(r => r.RoleName)
                .ToListAsync();

            return View(admins);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Approve Admin")]
        public async Task<IActionResult> Approve(int id)
        {
            var admin = await _db.Admins.FindAsync(id);
            if (admin == null)
            {
                TempData["Error"] = "Không tìm thấy admin.";
                return RedirectToAction(nameof(Index));
            }

            admin.IsApproved = true;
            await _db.SaveChangesAsync();

            _logger.LogInformation($"Admin {admin.UserName} đã được phê duyệt");
            TempData["Success"] = $"Đã phê duyệt tài khoản {admin.UserName}";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Block Admin")]
        public async Task<IActionResult> Block(int id)
        {
            var admin = await _db.Admins.FindAsync(id);
            if (admin == null)
            {
                TempData["Error"] = "Không tìm thấy admin.";
                return RedirectToAction(nameof(Index));
            }

            admin.IsBlocked = !admin.IsBlocked;
            await _db.SaveChangesAsync();

            var status = admin.IsBlocked ? "khóa" : "mở khóa";
            _logger.LogInformation($"Admin {admin.UserName} đã được {status}");
            TempData["Success"] = $"Đã {status} tài khoản {admin.UserName}";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Delete Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var admin = await _db.Admins.FindAsync(id);
            if (admin == null)
            {
                TempData["Error"] = "Không tìm thấy admin.";
                return RedirectToAction(nameof(Index));
            }

            // Không cho xóa chính mình
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == admin.AdminId.ToString())
            {
                TempData["Error"] = "Không thể xóa tài khoản của chính mình.";
                return RedirectToAction(nameof(Index));
            }

            _db.Admins.Remove(admin);
            await _db.SaveChangesAsync();

            _logger.LogInformation($"Admin {admin.UserName} đã bị xóa");
            TempData["Success"] = $"Đã xóa tài khoản {admin.UserName}";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Update Admin Role")]
        public async Task<IActionResult> UpdateRole(int id, int? roleId)
        {
            var admin = await _db.Admins.FindAsync(id);
            if (admin == null)
            {
                TempData["Error"] = "Không tìm thấy admin.";
                return RedirectToAction(nameof(Index));
            }

            admin.RoleId = roleId;
            await _db.SaveChangesAsync();

            _logger.LogInformation($"Đã cập nhật role cho admin {admin.UserName}");
            TempData["Success"] = $"Đã cập nhật quyền cho {admin.UserName}";

            return RedirectToAction(nameof(Index));
        }
    }
}
