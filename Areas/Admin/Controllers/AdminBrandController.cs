using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerfumeStore.Models;
using PerfumeStore.Areas.Admin.Filters;
using PerfumeStore.Areas.Admin.Services;
using System;

namespace PerfumeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class AdminBrandController : Controller
    {
        private readonly PerfumeStoreContext _context;
        private readonly IPaginationService _paginationService;

        public AdminBrandController(PerfumeStoreContext context, IPaginationService paginationService)
        {
            _context = context;
            _paginationService = paginationService;
        }

        // GET: Admin/Brand
        [RequirePermission("View Brands")]
        public async Task<IActionResult> Index(int page = 1)
        {
            var brandsQuery = _context.Brands
                .Include(b => b.Products)
                .OrderBy(b => b.BrandName)
                .AsQueryable();
            
            var pagedResult = await _paginationService.PaginateAsync(brandsQuery, page, 10);

            var totalBrands = await _context.Brands.CountAsync();
            var totalProducts = await _context.Products.CountAsync();
            var topBrandStats = await _context.Brands
                .OrderByDescending(b => b.Products.Count)
                .Select(b => new { b.BrandName, ProductCount = b.Products.Count })
                .FirstOrDefaultAsync();

            ViewBag.TotalBrands = totalBrands;
            ViewBag.TotalBrandProducts = totalProducts;
            ViewBag.AverageProductsPerBrand = totalBrands > 0
                ? Math.Round((double)totalProducts / Math.Max(totalBrands, 1), 1)
                : 0;
            ViewBag.TopBrandName = topBrandStats?.BrandName;
            ViewBag.TopBrandProductCount = topBrandStats?.ProductCount ?? 0;

            return View(pagedResult);
        }

        // GET: Admin/Brand/Details/5
        [RequirePermission("View Brands")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var brand = await _context.Brands
                .Include(b => b.Products)
                    .ThenInclude(p => p.ProductImages)
                .FirstOrDefaultAsync(m => m.BrandId == id);
            
            if (brand == null)
            {
                return NotFound();
            }

            // Tính toán thống kê
            var totalProducts = await _context.Products.CountAsync();
            var totalBrands = await _context.Brands.CountAsync();
            
            ViewBag.TotalProducts = totalProducts;
            ViewBag.TotalBrands = totalBrands;

            return View(brand);
        }

        // GET: Admin/Brand/Create
        [RequirePermission("Create Brand")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Brand/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Create Brand")]
        public async Task<IActionResult> Create([Bind("BrandName")] Brand brand, IFormFile? imageFile)
        {
            // Set giá trị mặc định cho ImageMimeType trước khi validation
            if (string.IsNullOrEmpty(brand.ImageMimeType))
            {
                brand.ImageMimeType = "image/png";
            }
            
            // Loại bỏ lỗi validation của ImageMimeType nếu có (vì đã set giá trị mặc định)
            ModelState.Remove("ImageMimeType");
            
            if (ModelState.IsValid)
            {
                // Kiểm tra trùng tên thương hiệu
                var existingBrand = await _context.Brands
                    .FirstOrDefaultAsync(b => b.BrandName.ToLower() == brand.BrandName.ToLower());
                
                if (existingBrand != null)
                {
                    ModelState.AddModelError("BrandName", "Tên thương hiệu đã tồn tại.");
                    return View(brand);
                }

                // Xử lý upload ảnh
                if (imageFile != null && imageFile.Length > 0)
                {
                    if (imageFile.Length > 5 * 1024 * 1024) // 5MB
                    {
                        ModelState.AddModelError("", "Kích thước ảnh không được vượt quá 5MB.");
                        return View(brand);
                    }

                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    var fileExtension = Path.GetExtension(imageFile.FileName).ToLower();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("", "Chỉ chấp nhận file ảnh: .jpg, .jpeg, .png, .gif, .webp");
                        return View(brand);
                    }

                    using (var memoryStream = new MemoryStream())
                    {
                        await imageFile.CopyToAsync(memoryStream);
                        brand.ImageData = memoryStream.ToArray();
                        brand.ImageMimeType = imageFile.ContentType;
                    }
                }
                else
                {
                    // Không có ảnh, set giá trị mặc định hợp lệ
                    brand.ImageData = null;
                    brand.ImageMimeType = "image/png"; // Giá trị mặc định hợp lệ
                }

                _context.Add(brand);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm thương hiệu thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(brand);
        }

        // GET: Admin/Brand/Edit/5
        [RequirePermission("Edit Brand")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
            {
                return NotFound();
            }
            return View(brand);
        }

        // POST: Admin/Brand/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Edit Brand")]
        public async Task<IActionResult> Edit(int id, [Bind("BrandId,BrandName,ImageData,ImageMimeType")] Brand brand, IFormFile? imageFile)
        {
            if (id != brand.BrandId)
            {
                return NotFound();
            }

            // Lấy thông tin brand hiện tại từ database để giữ nguyên ảnh cũ nếu không upload ảnh mới
            var existingBrand = await _context.Brands.AsNoTracking()
                .FirstOrDefaultAsync(b => b.BrandId == id);
            
            if (existingBrand == null)
            {
                return NotFound();
            }

            // Set giá trị mặc định cho ImageMimeType trước khi validation
            // Nếu không upload ảnh mới, giữ nguyên giá trị cũ
            if (string.IsNullOrEmpty(brand.ImageMimeType))
            {
                brand.ImageMimeType = existingBrand.ImageMimeType ?? "image/png";
            }
            
            // Loại bỏ lỗi validation của ImageMimeType nếu có (vì đã set giá trị mặc định)
            ModelState.Remove("ImageMimeType");

            if (ModelState.IsValid)
            {
                try
                {
                    // Kiểm tra trùng tên thương hiệu (trừ chính nó)
                    var duplicateBrand = await _context.Brands
                        .FirstOrDefaultAsync(b => b.BrandName.ToLower() == brand.BrandName.ToLower() 
                                                && b.BrandId != brand.BrandId);
                    
                    if (duplicateBrand != null)
                    {
                        ModelState.AddModelError("BrandName", "Tên thương hiệu đã tồn tại.");
                        return View(brand);
                    }

                    // Xử lý upload ảnh mới (nếu có)
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        if (imageFile.Length > 5 * 1024 * 1024) // 5MB
                        {
                            ModelState.AddModelError("", "Kích thước ảnh không được vượt quá 5MB.");
                            return View(brand);
                        }

                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    var fileExtension = Path.GetExtension(imageFile.FileName).ToLower();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("", "Chỉ chấp nhận file ảnh: .jpg, .jpeg, .png, .gif, .webp");
                        return View(brand);
                    }

                        using (var memoryStream = new MemoryStream())
                        {
                            await imageFile.CopyToAsync(memoryStream);
                            brand.ImageData = memoryStream.ToArray();
                            brand.ImageMimeType = imageFile.ContentType;
                        }
                    }
                    else
                    {
                        // Giữ nguyên ảnh cũ nếu không upload ảnh mới
                        brand.ImageData = existingBrand.ImageData;
                        brand.ImageMimeType = existingBrand.ImageMimeType ?? "image/png";
                    }

                    _context.Update(brand);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật thương hiệu thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BrandExists(brand.BrandId))
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
            return View(brand);
        }

        // GET: Admin/Brand/Delete/5
        [RequirePermission("Delete Brand")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var brand = await _context.Brands
                .Include(b => b.Products)
                .FirstOrDefaultAsync(m => m.BrandId == id);
            
            if (brand == null)
            {
                return NotFound();
            }

            // Double check số lượng sản phẩm từ database
            var actualProductCount = await _context.Products
                .Where(p => p.BrandId == id)
                .CountAsync();

            // Cập nhật ViewBag để hiển thị số lượng chính xác
            ViewBag.ActualProductCount = actualProductCount;

            return View(brand);
        }

        // POST: Admin/Brand/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [RequirePermission("Delete Brand")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var brand = await _context.Brands
                .Include(b => b.Products)
                .FirstOrDefaultAsync(b => b.BrandId == id);
            
            if (brand == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thương hiệu cần xóa.";
                return RedirectToAction(nameof(Index));
            }

            // Kiểm tra xem thương hiệu có sản phẩm không (double check)
            var productCount = await _context.Products
                .Where(p => p.BrandId == id)
                .CountAsync();

            if (productCount > 0)
            {
                TempData["ErrorMessage"] = $"Không thể xóa thương hiệu '{brand.BrandName}' vì còn {productCount} sản phẩm thuộc thương hiệu này. Vui lòng di chuyển hoặc xóa các sản phẩm trước.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Brands.Remove(brand);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã xóa thương hiệu '{brand.BrandName}' thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Có lỗi xảy ra khi xóa thương hiệu: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool BrandExists(int id)
        {
            return _context.Brands.Any(e => e.BrandId == id);
        }

        // AJAX endpoint để lấy thống kê nhanh
        [HttpGet]
        [RequirePermission("View Brands")]
        public async Task<IActionResult> GetBrandStats()
        {
            var stats = await _context.Brands
                .Select(b => new
                {
                    BrandId = b.BrandId,
                    BrandName = b.BrandName,
                    ProductCount = b.Products.Count()
                })
                .OrderByDescending(b => b.ProductCount)
                .ToListAsync();

            return Json(stats);
        }

        // AJAX endpoint để kiểm tra số lượng sản phẩm trong thương hiệu
        [HttpGet]
        [RequirePermission("View Brands")]
        public async Task<IActionResult> CheckBrandProducts(int id)
        {
            var productCount = await _context.Products
                .Where(p => p.BrandId == id)
                .CountAsync();

            var brand = await _context.Brands
                .FirstOrDefaultAsync(b => b.BrandId == id);

            return Json(new
            {
                brandId = id,
                brandName = brand?.BrandName ?? "Unknown",
                productCount = productCount,
                canDelete = productCount == 0
            });
        }
    }
}

