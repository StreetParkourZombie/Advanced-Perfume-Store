using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerfumeStore.Areas.Admin.Models;
using PerfumeStore.Areas.Admin.Filters;

namespace PerfumeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class FeesController : Controller
    {
        private readonly PerfumeStoreContext _db;

        public FeesController(PerfumeStoreContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var fees = await _db.Fees.OrderBy(f => f.FeeId).ToListAsync();
            return View(fees);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var fee = await _db.Fees.FindAsync(id);
            if (fee == null)
            {
                return NotFound();
            }

            return View(fee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Fee fee)
        {
            if (id != fee.FeeId)
            {
                return NotFound();
            }

            var existingFee = await _db.Fees.FindAsync(id);
            if (existingFee == null)
            {
                return NotFound();
            }

            // Validate VAT (0-100%)
            if (existingFee.Name == "VAT" && (fee.Value < 0 || fee.Value > 100))
            {
                ModelState.AddModelError("Value", "VAT phải từ 0 đến 100 phần trăm");
                return View(fee);
            }

            // Validate Shipping fee (>= 0)
            if (existingFee.Name == "Shipping" && fee.Value < 0)
            {
                ModelState.AddModelError("Value", "Phí vận chuyển phải >= 0");
                return View(fee);
            }

            existingFee.Value = fee.Value;
            existingFee.Description = fee.Description;
            existingFee.Threshold = fee.Threshold; // Lưu Threshold cho Shipping fee

            try
            {
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật khoản phí thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Không thể cập nhật khoản phí. Vui lòng thử lại.");
            }

            return View(fee);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Fee fee)
        {
            // Validate VAT (0-100%)
            if (fee.Name == "VAT" && (fee.Value < 0 || fee.Value > 100))
            {
                ModelState.AddModelError("Value", "VAT phải từ 0 đến 100 phần trăm");
                return View(fee);
            }

            // Validate Shipping fee (>= 0)
            if (fee.Name == "Shipping" && fee.Value < 0)
            {
                ModelState.AddModelError("Value", "Phí vận chuyển phải >= 0");
                return View(fee);
            }

            // Check if fee name already exists
            var existingFee = await _db.Fees.FirstOrDefaultAsync(f => f.Name == fee.Name);
            if (existingFee != null)
            {
                ModelState.AddModelError("Name", "Tên khoản phí đã tồn tại");
                return View(fee);
            }

            // Tự động gán FeeId (lấy FeeId lớn nhất + 1)
            var maxFeeId = await _db.Fees.MaxAsync(f => (int?)f.FeeId) ?? 0;
            fee.FeeId = maxFeeId + 1;

            try
            {
                _db.Fees.Add(fee);
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Tạo khoản phí thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", "Không thể tạo khoản phí. Vui lòng thử lại. Lỗi: " + ex.Message);
            }

            return View(fee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateThreshold(int feeId, decimal? threshold)
        {
            var fee = await _db.Fees.FindAsync(feeId);
            if (fee == null)
            {
                return Json(new { success = false, message = "Không tìm thấy khoản phí" });
            }

            // Chỉ cho phép cập nhật threshold cho Shipping fee
            if (fee.Name != "Shipping")
            {
                return Json(new { success = false, message = "Chỉ có thể cập nhật ngưỡng cho phí vận chuyển" });
            }

            fee.Threshold = threshold;

            try
            {
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật ngưỡng thành công!";
                return Json(new { success = true, message = "Đã cập nhật ngưỡng thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var fee = await _db.Fees.FindAsync(id);
            if (fee == null)
            {
                return Json(new { success = false, message = "Không tìm thấy khoản phí" });
            }

            // Không cho phép xóa VAT và Shipping (các fee mặc định)
            if (fee.Name == "VAT" || fee.Name == "Shipping")
            {
                return Json(new { success = false, message = "Không thể xóa khoản phí mặc định (VAT, Shipping)" });
            }

            try
            {
                _db.Fees.Remove(fee);
                await _db.SaveChangesAsync();
                return Json(new { success = true, message = "Đã xóa khoản phí thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
    }
}


