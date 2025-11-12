using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerfumeStore.Models;
using PerfumeStore.Areas.Admin.Filters;

namespace PerfumeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class RoleManagementController : Controller
    {
        private readonly PerfumeStoreContext _db;
        private readonly ILogger<RoleManagementController> _logger;

        public RoleManagementController(PerfumeStoreContext db, ILogger<RoleManagementController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // Danh sách roles
        [RequirePermission("View Roles")]
        public async Task<IActionResult> Index()
        {
            var roles = await _db.Roles
                .Include(r => r.Permissions)
                .Include(r => r.Admins)
                .OrderBy(r => r.RoleName)
                .ToListAsync();

            return View(roles);
        }

        // Tạo role mới
        [HttpGet]
        [RequirePermission("Create Role")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Permissions = await _db.Permissions.OrderBy(p => p.Name).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Create Role")]
        public async Task<IActionResult> Create(string roleName, string description, List<int> permissionIds)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                TempData["Error"] = "Tên role không được để trống.";
                ViewBag.Permissions = await _db.Permissions.OrderBy(p => p.Name).ToListAsync();
                return View();
            }

            // Kiểm tra trùng tên
            if (await _db.Roles.AnyAsync(r => r.RoleName == roleName))
            {
                TempData["Error"] = "Tên role đã tồn tại.";
                ViewBag.Permissions = await _db.Permissions.OrderBy(p => p.Name).ToListAsync();
                return View();
            }

            var role = new Role
            {
                RoleName = roleName,
                Description = description,
                IsSystem = false
            };

            // Thêm permissions
            if (permissionIds != null && permissionIds.Any())
            {
                var permissions = await _db.Permissions
                    .Where(p => permissionIds.Contains(p.PermissionId))
                    .ToListAsync();
                
                foreach (var permission in permissions)
                {
                    role.Permissions.Add(permission);
                }
            }

            _db.Roles.Add(role);
            await _db.SaveChangesAsync();

            _logger.LogInformation($"Đã tạo role mới: {roleName}");
            TempData["Success"] = $"Đã tạo role '{roleName}' thành công!";

            return RedirectToAction(nameof(Index));
        }

        // Sửa role
        [HttpGet]
        [RequirePermission("Edit Role")]
        public async Task<IActionResult> Edit(int id)
        {
            var role = await _db.Roles
                .Include(r => r.Permissions)
                .FirstOrDefaultAsync(r => r.RoleId == id);

            if (role == null)
            {
                TempData["Error"] = "Không tìm thấy role.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Permissions = await _db.Permissions.OrderBy(p => p.Name).ToListAsync();
            ViewBag.SelectedPermissionIds = role.Permissions.Select(p => p.PermissionId).ToList();

            return View(role);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Edit Role")]
        public async Task<IActionResult> Edit(int id, string roleName, string description, List<int> permissionIds)
        {
            var role = await _db.Roles
                .Include(r => r.Permissions)
                .FirstOrDefaultAsync(r => r.RoleId == id);

            if (role == null)
            {
                TempData["Error"] = "Không tìm thấy role.";
                return RedirectToAction(nameof(Index));
            }

            if (role.IsSystem)
            {
                TempData["Error"] = "Không thể sửa role hệ thống.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(roleName))
            {
                TempData["Error"] = "Tên role không được để trống.";
                ViewBag.Permissions = await _db.Permissions.OrderBy(p => p.Name).ToListAsync();
                return View(role);
            }

            // Kiểm tra trùng tên (trừ chính nó)
            if (await _db.Roles.AnyAsync(r => r.RoleName == roleName && r.RoleId != id))
            {
                TempData["Error"] = "Tên role đã tồn tại.";
                ViewBag.Permissions = await _db.Permissions.OrderBy(p => p.Name).ToListAsync();
                return View(role);
            }

            role.RoleName = roleName;
            role.Description = description;

            // Cập nhật permissions
            role.Permissions.Clear();
            if (permissionIds != null && permissionIds.Any())
            {
                var permissions = await _db.Permissions
                    .Where(p => permissionIds.Contains(p.PermissionId))
                    .ToListAsync();
                
                foreach (var permission in permissions)
                {
                    role.Permissions.Add(permission);
                }
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation($"Đã cập nhật role: {roleName}");
            TempData["Success"] = $"Đã cập nhật role '{roleName}' thành công!";

            return RedirectToAction(nameof(Index));
        }

        // Xóa role
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Delete Role")]
        public async Task<IActionResult> Delete(int id)
        {
            var role = await _db.Roles
                .Include(r => r.Admins)
                .FirstOrDefaultAsync(r => r.RoleId == id);

            if (role == null)
            {
                TempData["Error"] = "Không tìm thấy role.";
                return RedirectToAction(nameof(Index));
            }

            if (role.IsSystem)
            {
                TempData["Error"] = "Không thể xóa role hệ thống.";
                return RedirectToAction(nameof(Index));
            }

            if (role.Admins.Any())
            {
                TempData["Error"] = $"Không thể xóa role '{role.RoleName}' vì đang có {role.Admins.Count} admin sử dụng.";
                return RedirectToAction(nameof(Index));
            }

            _db.Roles.Remove(role);
            await _db.SaveChangesAsync();

            _logger.LogInformation($"Đã xóa role: {role.RoleName}");
            TempData["Success"] = $"Đã xóa role '{role.RoleName}' thành công!";

            return RedirectToAction(nameof(Index));
        }

        // Xem chi tiết permissions của role
        [HttpGet]
        [RequirePermission("View Role Details")]
        public async Task<IActionResult> Details(int id)
        {
            var role = await _db.Roles
                .Include(r => r.Permissions)
                .Include(r => r.Admins)
                .FirstOrDefaultAsync(r => r.RoleId == id);

            if (role == null)
            {
                TempData["Error"] = "Không tìm thấy role.";
                return RedirectToAction(nameof(Index));
            }

            return View(role);
        }
    }
}
