using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerfumeStore.Models;
using PerfumeStore.Areas.Admin.Filters;

namespace PerfumeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class WarrantyController : Controller
    {
        private readonly PerfumeStoreContext _context;

        public WarrantyController(PerfumeStoreContext context)
        {
            _context = context;
        }

        // GET: Admin/Warranty
        // [RequirePermission("View Warranties")] // Tạm thời bỏ để test
        public async Task<IActionResult> Index(string? status, string? search, DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                // Test connection
                var warrantyCount = await _context.Warranties.CountAsync();
                ViewBag.TestMessage = $"Kết nối thành công! Có {warrantyCount} bảo hành trong database.";
                
                var query = _context.Warranties.AsQueryable();

            // Filter by status
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(w => w.Status == status);
            }

            // Filter by warranty code or customer info
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(w => w.WarrantyCode.Contains(search) || 
                                        w.Notes.Contains(search));
            }

            // Filter by date range
            if (fromDate.HasValue)
            {
                query = query.Where(w => w.StartDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(w => w.StartDate <= toDate.Value);
            }

            var warranties = await query
                .Include(w => w.WarrantyClaims)
                .OrderByDescending(w => w.CreatedDate)
                .ToListAsync();

            // Statistics for dashboard
            ViewBag.TotalWarranties = await _context.Warranties.CountAsync();
            ViewBag.ActiveWarranties = await _context.Warranties.CountAsync(w => w.Status == "Active");
            ViewBag.ExpiredWarranties = await _context.Warranties.CountAsync(w => w.Status == "Expired");
            ViewBag.ClaimedWarranties = await _context.Warranties.CountAsync(w => w.WarrantyClaims.Any());

            // Filter values for view
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentFromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.CurrentToDate = toDate?.ToString("yyyy-MM-dd");

            return View(warranties);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Lỗi: {ex.Message}";
                ViewBag.TestMessage = "Có lỗi xảy ra khi truy cập database.";
                return View(new List<Warranty>());
            }
        }

        // GET: Admin/Warranty/Details/5
        // [RequirePermission("View Warranties")] // Tạm thời bỏ để test
        public async Task<IActionResult> Details(int? id)
        {
            try
            {
                if (id == null)
                {
                    ViewBag.ErrorMessage = "ID không hợp lệ";
                    return View();
                }

                ViewBag.TestMessage = $"Đang tìm bảo hành với ID: {id}";

                var warranty = await _context.Warranties
                    .Include(w => w.WarrantyClaims)
                    .FirstOrDefaultAsync(m => m.WarrantyId == id);

                if (warranty == null)
                {
                    ViewBag.ErrorMessage = $"Không tìm thấy bảo hành với ID: {id}";
                    return View();
                }

                return View(warranty);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Lỗi khi tải chi tiết bảo hành: {ex.Message}";
                ViewBag.TestMessage = $"Exception: {ex.GetType().Name}";
                return View();
            }
        }

        // Bảo hành được tạo tự động khi khách hàng mua sản phẩm
        // Không có chức năng tạo thủ công

        // GET: Admin/Warranty/Edit/5
        // [RequirePermission("Edit Warranty")] // Tạm thời bỏ để test
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var warranty = await _context.Warranties.FindAsync(id);
            if (warranty == null)
            {
                return NotFound();
            }
            return View(warranty);
        }

        // POST: Admin/Warranty/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        // [RequirePermission("Edit Warranty")] // Tạm thời bỏ để test
        public async Task<IActionResult> Edit(int id, [Bind("WarrantyId,OrderDetailId,CustomerId,WarrantyCode,StartDate,EndDate,WarrantyPeriodMonths,Status,Notes,CreatedDate")] Warranty warranty)
        {
            if (id != warranty.WarrantyId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    warranty.UpdatedDate = DateTime.Now;
                    _context.Update(warranty);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật bảo hành thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!WarrantyExists(warranty.WarrantyId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(warranty);
        }

        // POST: Admin/Warranty/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        // [RequirePermission("Edit Warranty")] // Tạm thời bỏ để test
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var warranty = await _context.Warranties.FindAsync(id);
            if (warranty == null)
            {
                return NotFound();
            }

            warranty.Status = status;
            warranty.UpdatedDate = DateTime.Now;
            
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"Đã cập nhật trạng thái bảo hành thành '{status}'.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Admin/Warranty/Claims
        // [RequirePermission("View Warranties")] // Tạm thời bỏ để test
        public async Task<IActionResult> Claims(string? status, string? search)
        {
            var query = _context.WarrantyClaims
                .Include(wc => wc.Warranty)
                .AsQueryable();

            // Filter by status
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(wc => wc.Status == status);
            }

            // Filter by claim code or issue description
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(wc => wc.ClaimCode.Contains(search) || 
                                         wc.IssueDescription.Contains(search));
            }

            var claims = await query
                .OrderByDescending(wc => wc.SubmittedDate)
                .ToListAsync();

            // Statistics
            ViewBag.TotalClaims = await _context.WarrantyClaims.CountAsync();
            ViewBag.PendingClaims = await _context.WarrantyClaims.CountAsync(wc => wc.Status == "Pending");
            ViewBag.ProcessingClaims = await _context.WarrantyClaims.CountAsync(wc => wc.Status == "Processing");
            ViewBag.CompletedClaims = await _context.WarrantyClaims.CountAsync(wc => wc.Status == "Completed");

            ViewBag.CurrentStatus = status;
            ViewBag.CurrentSearch = search;

            return View(claims);
        }

        // GET: Admin/Warranty/ClaimDetails/5
        // [RequirePermission("View Warranties")] // Tạm thời bỏ để test
        public async Task<IActionResult> ClaimDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var claim = await _context.WarrantyClaims
                .Include(wc => wc.Warranty)
                .FirstOrDefaultAsync(m => m.WarrantyClaimId == id);

            if (claim == null)
            {
                return NotFound();
            }

            return View(claim);
        }

        // POST: Admin/Warranty/ProcessClaim/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        // [RequirePermission("Edit Warranty")] // Tạm thời bỏ để test
        public async Task<IActionResult> ProcessClaim(int id, string status, string? resolution, string? resolutionType, string? adminNotes)
        {
            var claim = await _context.WarrantyClaims.FindAsync(id);
            if (claim == null)
            {
                return NotFound();
            }

            claim.Status = status;
            claim.Resolution = resolution;
            claim.ResolutionType = resolutionType;
            claim.AdminNotes = adminNotes;
            claim.ProcessedByAdmin = User.Identity?.Name;

            if (status == "Processing" && claim.ProcessedDate == null)
            {
                claim.ProcessedDate = DateTime.Now;
            }
            else if (status == "Completed")
            {
                claim.CompletedDate = DateTime.Now;
                if (claim.ProcessedDate == null)
                {
                    claim.ProcessedDate = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"Đã cập nhật yêu cầu bảo hành thành '{status}'.";
            return RedirectToAction(nameof(ClaimDetails), new { id });
        }

        // AJAX: Get warranty statistics
        [HttpGet]
        // [RequirePermission("View Warranties")] // Tạm thời bỏ để test
        public async Task<IActionResult> GetWarrantyStats()
        {
            var stats = new
            {
                TotalWarranties = await _context.Warranties.CountAsync(),
                ActiveWarranties = await _context.Warranties.CountAsync(w => w.Status == "Active"),
                ExpiredWarranties = await _context.Warranties.CountAsync(w => w.Status == "Expired"),
                ClaimedWarranties = await _context.Warranties.CountAsync(w => w.WarrantyClaims.Any()),
                TotalClaims = await _context.WarrantyClaims.CountAsync(),
                PendingClaims = await _context.WarrantyClaims.CountAsync(wc => wc.Status == "Pending"),
                ProcessingClaims = await _context.WarrantyClaims.CountAsync(wc => wc.Status == "Processing"),
                CompletedClaims = await _context.WarrantyClaims.CountAsync(wc => wc.Status == "Completed")
            };

            return Json(stats);
        }

        private bool WarrantyExists(int id)
        {
            return _context.Warranties.Any(e => e.WarrantyId == id);
        }

        // Method để tạo bảo hành tự động khi đơn hàng được xác nhận
        public async Task<bool> CreateWarrantyForOrderAsync(int orderDetailId, int customerId, int warrantyPeriodMonths)
        {
            try
            {
                // Kiểm tra xem đã có bảo hành cho OrderDetail này chưa
                var existingWarranty = await _context.Warranties
                    .FirstOrDefaultAsync(w => w.OrderDetailId == orderDetailId);

                if (existingWarranty != null)
                {
                    return false; // Đã có bảo hành rồi
                }

                var warranty = new Warranty
                {
                    OrderDetailId = orderDetailId,
                    CustomerId = customerId,
                    WarrantyCode = GenerateWarrantyCode(),
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddMonths(warrantyPeriodMonths),
                    WarrantyPeriodMonths = warrantyPeriodMonths,
                    Status = "Active",
                    Notes = "Bảo hành được tạo tự động khi xác nhận đơn hàng",
                    CreatedDate = DateTime.Now
                };

                _context.Warranties.Add(warranty);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Method để tạo bảo hành cho tất cả sản phẩm trong đơn hàng
        public async Task<int> CreateWarrantiesForOrderAsync(int orderId)
        {
            try
            {
                var orderDetails = await _context.OrderDetails
                    .Include(od => od.Product)
                    .Include(od => od.Order)
                    .Where(od => od.OrderId == orderId)
                    .ToListAsync();

                int createdCount = 0;

                foreach (var orderDetail in orderDetails)
                {
                    // Chỉ tạo bảo hành cho sản phẩm có thời gian bảo hành > 0
                    if (orderDetail.Product.WarrantyPeriodMonths > 0)
                    {
                        var success = await CreateWarrantyForOrderAsync(
                            orderDetail.OrderDetailId,
                            orderDetail.Order.CustomerId,
                            orderDetail.Product.WarrantyPeriodMonths
                        );

                        if (success)
                        {
                            createdCount++;
                        }
                    }
                }

                return createdCount;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        // GET: Admin/Warranty/Test - Action test đơn giản
        public IActionResult Test()
        {
            ViewBag.Message = "WarrantyController hoạt động bình thường!";
            ViewBag.DateTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            return View("TestView");
        }

        private string GenerateWarrantyCode()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(1000, 9999);
            return $"WR{timestamp}{random}";
        }
    }
}