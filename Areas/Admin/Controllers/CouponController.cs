using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerfumeStore.Areas.Admin.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PerfumeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CouponController : Controller
    {
        private readonly PerfumeStoreContext _context;

        public CouponController(PerfumeStoreContext context)
        {
            _context = context;
        }

        // GET: Admin/Coupon
        public async Task<IActionResult> Index()
        {
            var coupons = await _context.Coupons
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();
            return View("Index", coupons); // View riêng Index.cshtml
        }

        // GET: Admin/Coupon/Create
        public IActionResult Create()
        {
            return View("Create"); // View riêng Create.cshtml
        }

        // POST: Admin/Coupon/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Coupon coupon)
        {
            if (!ModelState.IsValid)
                return View("Create", coupon);

            // Chuẩn hoá dữ liệu
            coupon.Code = (coupon.Code ?? string.Empty).Trim().ToUpperInvariant();

            // Validate: không trùng Code
            var isDuplicate = await _context.Coupons
                .AnyAsync(c => c.Code == coupon.Code);
            if (isDuplicate)
            {
                ModelState.AddModelError(nameof(coupon.Code), "Code đã tồn tại.");
                return View("Create", coupon);
            }

            SetDefaultValues(coupon);

            _context.Coupons.Add(coupon);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Tạo coupon thành công!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Coupon/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null) return NotFound();

            return View("Edit", coupon); // View riêng Edit.cshtml
        }

        // POST: Admin/Coupon/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Coupon coupon)
        {
            if (id != coupon.CouponId) return NotFound();

            if (!ModelState.IsValid)
                return View("Edit", coupon);

            coupon.Code = (coupon.Code ?? string.Empty).Trim().ToUpperInvariant();

            // Validate: không trùng Code với coupon khác
            var isDuplicate = await _context.Coupons
                .AnyAsync(c => c.CouponId != coupon.CouponId && c.Code == coupon.Code);
            if (isDuplicate)
            {
                ModelState.AddModelError(nameof(coupon.Code), "Code đã tồn tại.");
                return View("Edit", coupon);
            }

            try
            {
                _context.Update(coupon);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật coupon thành công!";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CouponExists(coupon.CouponId)) return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Coupon/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null) return NotFound();

            return View("Delete", coupon); // View riêng Delete.cshtml
        }

        // POST: Admin/Coupon/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var coupon = await _context.Coupons
                .Include(c => c.Orders)
                .FirstOrDefaultAsync(c => c.CouponId == id);
            if (coupon == null) return RedirectToAction(nameof(Index));

            // Chặn xoá nếu đã dùng hoặc đã gắn với đơn hàng
            if ((coupon.IsUsed ?? false) || (coupon.Orders?.Any() ?? false))
            {
                TempData["SuccessMessage"] = "Không thể xóa coupon đã sử dụng hoặc đã gắn với đơn hàng.";
                return RedirectToAction(nameof(Index));
            }

            _context.Coupons.Remove(coupon);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Xóa coupon thành công!";

            return RedirectToAction(nameof(Index));
        }

        // --- Private helper methods ---
        private bool CouponExists(int id) => _context.Coupons.Any(c => c.CouponId == id);

        private void SetDefaultValues(Coupon coupon)
        {
            if (coupon.CreatedDate == null)
                coupon.CreatedDate = DateTime.Now;
            if (coupon.IsUsed == null)
                coupon.IsUsed = false;
        }
    }
}
