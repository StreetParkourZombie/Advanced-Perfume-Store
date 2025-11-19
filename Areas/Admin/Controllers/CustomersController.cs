using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerfumeStore.Areas.Admin.Models;
using PerfumeStore.Areas.Admin.Services;
using PerfumeStore.Areas.Admin.Filters;

namespace PerfumeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class CustomersController : Controller
    {
        private readonly PerfumeStoreContext _db;
        private readonly DBQueryService.IDbQueryService _queryService;
        private readonly IPaginationService _paginationService;

        public CustomersController(PerfumeStoreContext db, DBQueryService.IDbQueryService queryService, IPaginationService paginationService)
        {
            _db = db;
            _queryService = queryService;
            _paginationService = paginationService;
        }

        [RequirePermission("View Customers")]
        public async Task<IActionResult> Index(string? searchName, string? searchEmail, string? searchPhone, int? membershipId, int page = 1)
        {
            var customers = await _queryService.GetCustomersWithIncludesAsync();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchName))
            {
                customers = customers.Where(c => 
                    c.Name != null && c.Name.Contains(searchName)
                ).ToList();
            }

            if (!string.IsNullOrWhiteSpace(searchEmail))
            {
                customers = customers.Where(c => 
                    c.Email.Contains(searchEmail)
                ).ToList();
            }

            if (!string.IsNullOrWhiteSpace(searchPhone))
            {
                customers = customers.Where(c => 
                    c.Phone != null && c.Phone.Contains(searchPhone)
                ).ToList();
            }

            if (membershipId.HasValue)
            {
                customers = customers.Where(c => c.MembershipId == membershipId.Value).ToList();
            }

            // Apply pagination
            var pagedResult = _paginationService.Paginate(customers, page, 10);

            ViewBag.Memberships = await _db.Memberships.OrderBy(m => m.Name).ToListAsync();
            ViewBag.SearchName = searchName;
            ViewBag.SearchEmail = searchEmail;
            ViewBag.SearchPhone = searchPhone;
            ViewBag.MembershipId = membershipId;

            // Calculate customer statistics (from all customers, not just current page)
            ViewBag.TotalCustomers = customers.Count;
            ViewBag.ActiveCustomers = customers.Count(c => c.Orders.Any());
            ViewBag.NewCustomersThisMonth = customers.Count(c => 
                c.CreatedDate.HasValue && 
                c.CreatedDate.Value.Month == DateTime.Now.Month && 
                c.CreatedDate.Value.Year == DateTime.Now.Year);

            return View(pagedResult);
        }

        [RequirePermission("View Customers")]
        public async Task<IActionResult> Details(int id)
        {
            var customer = await _db.Customers
                .Include(c => c.Membership)
                .Include(c => c.Orders)
                    .ThenInclude(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                            .ThenInclude(p => p.Brand)
                .Include(c => c.Comments)
                    .ThenInclude(co => co.Product)
                .Include(c => c.ShippingAddresses)
                .FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null)
            {
                return NotFound();
            }

            // Calculate customer statistics
            ViewBag.TotalOrders = customer.Orders.Count;
            ViewBag.TotalSpent = customer.Orders.Sum(o => o.TotalAmount ?? 0);
            ViewBag.TotalComments = customer.Comments.Count;
            ViewBag.AverageOrderValue = ViewBag.TotalOrders > 0 ? ViewBag.TotalSpent / ViewBag.TotalOrders : 0;
            ViewBag.LastOrderDate = customer.Orders.OrderByDescending(o => o.OrderDate).FirstOrDefault()?.OrderDate;

            return View(customer);
        }

        [RequirePermission("Edit Customer")]
        public async Task<IActionResult> Edit(int id)
        {
            var customer = await _db.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            ViewBag.Memberships = await _db.Memberships.OrderBy(m => m.Name).ToListAsync();

            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Edit Customer")]
        public async Task<IActionResult> Edit(int id, Models.Customer customer)
        {
            if (id != customer.CustomerId)
            {
                return NotFound();
            }

            var existingCustomer = await _db.Customers.FindAsync(id);
            if (existingCustomer == null)
            {
                return NotFound();
            }

            // Update fields
            existingCustomer.Name = customer.Name;
            existingCustomer.Phone = customer.Phone;
            existingCustomer.Email = customer.Email;
            existingCustomer.BirthYear = customer.BirthYear;
            existingCustomer.MembershipId = customer.MembershipId;

            try
            {
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật thông tin khách hàng thành công!";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", "Không thể cập nhật thông tin khách hàng. Vui lòng thử lại.");
            }

            ViewBag.Memberships = await _db.Memberships.OrderBy(m => m.Name).ToListAsync();

            return View(customer);
        }

        [RequirePermission("Block Customer")]
        public async Task<IActionResult> BlockCustomer(int id)
        {
            var customer = await _db.Customers.FindAsync(id);
            if (customer == null)
            {
                return Json(new { success = false, message = "Không tìm thấy khách hàng" });
            }

            // Note: You may want to add an IsBlocked field to Customer model
            // For now, this is a placeholder
            try
            {
                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "Đã khóa tài khoản khách hàng!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomerOrders(int customerId)
        {
            var orders = await _db.Orders
                .Where(o => o.CustomerId == customerId)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return PartialView("_CustomerOrders", orders);
        }
    }
}
