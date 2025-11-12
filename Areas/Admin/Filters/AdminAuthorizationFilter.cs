using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using PerfumeStore.Models;

namespace PerfumeStore.Areas.Admin.Filters
{
    // Filter cơ bản: Chỉ kiểm tra đăng nhập và role Admin
    public class AdminAuthorizationFilter : IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            
            // Kiểm tra xem user đã đăng nhập chưa
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new RedirectToActionResult("Login", "AdminAuth", new { area = "Admin" });
                return;
            }

            // Kiểm tra xem user có role Admin không
            var roleClaim = user.FindFirst(ClaimTypes.Role);
            if (roleClaim == null || roleClaim.Value != "Admin")
            {
                context.Result = new RedirectToActionResult("Login", "AdminAuth", new { area = "Admin" });
                return;
            }
        }
    }

    // Filter nâng cao: Kiểm tra permission cụ thể
    public class PermissionAuthorizationFilter : IAsyncAuthorizationFilter
    {
        private readonly string _permissionName;
        private readonly PerfumeStoreContext _db;

        public PermissionAuthorizationFilter(string permissionName, PerfumeStoreContext db)
        {
            _permissionName = permissionName;
            _db = db;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            
            // Kiểm tra đăng nhập
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new RedirectToActionResult("Login", "AdminAuth", new { area = "Admin" });
                return;
            }

            // Kiểm tra role Admin
            var roleClaim = user.FindFirst(ClaimTypes.Role);
            if (roleClaim == null || roleClaim.Value != "Admin")
            {
                context.Result = new RedirectToActionResult("Login", "AdminAuth", new { area = "Admin" });
                return;
            }

            // Lấy AdminId từ claims
            var adminIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (adminIdClaim == null || !int.TryParse(adminIdClaim.Value, out int adminId))
            {
                context.Result = new RedirectToActionResult("Login", "AdminAuth", new { area = "Admin" });
                return;
            }

            // Kiểm tra permission
            var hasPermission = await _db.Admins
                .Where(a => a.AdminId == adminId && a.RoleId != null)
                .SelectMany(a => a.Role.Permissions)
                .AnyAsync(p => p.Name == _permissionName);

            if (!hasPermission)
            {
                // Không có quyền -> Chuyển đến trang Access Denied
                context.Result = new ViewResult
                {
                    ViewName = "~/Areas/Admin/Views/Shared/AccessDenied.cshtml"
                };
                return;
            }
        }
    }

    // Attribute cơ bản: Chỉ kiểm tra Admin
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AdminAuthorizeAttribute : TypeFilterAttribute
    {
        public AdminAuthorizeAttribute() : base(typeof(AdminAuthorizationFilter))
        {
        }
    }

    // Attribute nâng cao: Kiểm tra permission cụ thể
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequirePermissionAttribute : TypeFilterAttribute
    {
        public RequirePermissionAttribute(string permissionName) 
            : base(typeof(PermissionAuthorizationFilter))
        {
            Arguments = new object[] { permissionName };
        }
    }
}
