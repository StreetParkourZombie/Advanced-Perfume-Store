using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerfumeStore.Areas.Admin.Models;
using PerfumeStore.Areas.Admin.Models.ViewModels;
using PerfumeStore.Areas.Admin.Filters;
using System.Linq;

namespace PerfumeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class ProfileController : Controller
    {
        private readonly PerfumeStoreContext _db;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(PerfumeStoreContext db, ILogger<ProfileController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [RequirePermission("View Customers")]
        public async Task<IActionResult> Index(string searchName, string searchEmail, int? page)
        {
            ViewData["Title"] = "Quản lý khách hàng";
            
            var query = _db.Customers
                .Include(c => c.Membership)
                .AsQueryable();

            // Tìm kiếm theo tên
            if (!string.IsNullOrWhiteSpace(searchName))
            {
                query = query.Where(c => c.Name != null && c.Name.Contains(searchName));
            }

            // Tìm kiếm theo email
            if (!string.IsNullOrWhiteSpace(searchEmail))
            {
                query = query.Where(c => c.Email.Contains(searchEmail));
            }

            var customers = await query
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();

            ViewBag.SearchName = searchName;
            ViewBag.SearchEmail = searchEmail;

            return View(customers);
        }

        [RequirePermission("View Customers")]
        public async Task<IActionResult> Details(int id)
        {
            var customer = await _db.Customers
                .Include(c => c.Membership)
                .Include(c => c.Orders)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null)
            {
                TempData["Error"] = "Không tìm thấy khách hàng.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["Title"] = "Chi tiết khách hàng";
            return View(customer);
        }

        [RequirePermission("Edit Customers")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var customer = await _db.Customers
                .Include(c => c.Membership)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null)
            {
                TempData["Error"] = "Không tìm thấy khách hàng.";
                return RedirectToAction(nameof(Index));
            }

            var model = new CustomerAccountVM_Admin
            {
                CustomerId = customer.CustomerId,
                Name = customer.Name,
                Email = customer.Email,
                Phone = customer.Phone,
                BirthYear = customer.BirthYear,
                CreatedDate = customer.CreatedDate,
                MembershipId = customer.MembershipId
            };

            ViewBag.Memberships = await _db.Memberships.OrderBy(m => m.Name).ToListAsync();
            ViewData["Title"] = "Chỉnh sửa thông tin khách hàng";

            return View(model);
        }

        [RequirePermission("Edit Customers")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CustomerAccountVM_Admin model)
        {
            ViewData["Title"] = "Chỉnh sửa thông tin khách hàng";

            if (!ModelState.IsValid)
            {
                ViewBag.Memberships = await _db.Memberships.OrderBy(m => m.Name).ToListAsync();
                return View(model);
            }

            var customer = await _db.Customers.FindAsync(id);
            if (customer == null)
            {
                TempData["Error"] = "Không tìm thấy khách hàng.";
                return RedirectToAction(nameof(Index));
            }

            // Kiểm tra email trùng lặp (nếu đổi email)
            if (customer.Email != model.Email)
            {
                var existingCustomer = await _db.Customers
                    .FirstOrDefaultAsync(c => c.Email == model.Email && c.CustomerId != id);
                
                if (existingCustomer != null)
                {
                    ModelState.AddModelError(nameof(model.Email), "Email này đã được sử dụng bởi khách hàng khác.");
                    ViewBag.Memberships = await _db.Memberships.OrderBy(m => m.Name).ToListAsync();
                    return View(model);
                }
            }

            // Xử lý Name: trim và kiểm tra độ dài
            if (!string.IsNullOrWhiteSpace(model.Name))
            {
                var trimmedName = model.Name.Trim();
                if (trimmedName.Length > 100)
                {
                    ModelState.AddModelError(nameof(model.Name), "Họ tên tối đa 100 ký tự");
                    ViewBag.Memberships = await _db.Memberships.OrderBy(m => m.Name).ToListAsync();
                    return View(model);
                }
                customer.Name = trimmedName;
            }
            else
            {
                customer.Name = null;
            }

            // Xử lý Phone: chỉ chứa số, tối đa 13 chữ số
            if (!string.IsNullOrWhiteSpace(model.Phone))
            {
                var trimmedPhone = model.Phone.Trim();
                // Loại bỏ các ký tự không phải số
                trimmedPhone = new string(trimmedPhone.Where(char.IsDigit).ToArray());
                
                if (trimmedPhone.Length > 13)
                {
                    ModelState.AddModelError(nameof(model.Phone), "Số điện thoại tối đa 13 chữ số");
                    ViewBag.Memberships = await _db.Memberships.OrderBy(m => m.Name).ToListAsync();
                    return View(model);
                }
                
                customer.Phone = trimmedPhone.Length > 0 ? trimmedPhone : null;
            }
            else
            {
                customer.Phone = null;
            }

            // Xử lý Email
            customer.Email = model.Email.Trim();

            // Xử lý BirthYear
            if (model.BirthYear.HasValue)
            {
                var currentYear = DateTime.Now.Year;
                if (model.BirthYear.Value < 1900 || model.BirthYear.Value > currentYear)
                {
                    ModelState.AddModelError(nameof(model.BirthYear), $"Năm sinh chỉ trong khoảng 1900 đến {currentYear}");
                    ViewBag.Memberships = await _db.Memberships.OrderBy(m => m.Name).ToListAsync();
                    return View(model);
                }
                customer.BirthYear = model.BirthYear;
            }
            else
            {
                customer.BirthYear = null;
            }

            // Xử lý MembershipId
            customer.MembershipId = model.MembershipId;

            _db.Customers.Update(customer);
            await _db.SaveChangesAsync();

            _logger.LogInformation($"Admin đã cập nhật thông tin khách hàng {customer.CustomerId}");
            TempData["Success"] = $"Đã cập nhật thông tin khách hàng {customer.Email} thành công!";

            return RedirectToAction(nameof(Index));
        }

        [RequirePermission("Delete Customers")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _db.Customers
                .Include(c => c.Orders)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null)
            {
                TempData["Error"] = "Không tìm thấy khách hàng.";
                return RedirectToAction(nameof(Index));
            }

            // Kiểm tra xem khách hàng có đơn hàng không
            if (customer.Orders != null && customer.Orders.Any())
            {
                TempData["Error"] = "Không thể xóa khách hàng vì đã có đơn hàng.";
                return RedirectToAction(nameof(Index));
            }

            _db.Customers.Remove(customer);
            await _db.SaveChangesAsync();

            _logger.LogInformation($"Admin đã xóa khách hàng {customer.CustomerId}");
            TempData["Success"] = $"Đã xóa khách hàng {customer.Email} thành công!";

            return RedirectToAction(nameof(Index));
        }
    }
}

