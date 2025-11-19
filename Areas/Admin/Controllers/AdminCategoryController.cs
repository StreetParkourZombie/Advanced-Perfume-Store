using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerfumeStore.Models;
using PerfumeStore.Areas.Admin.Filters;
using PerfumeStore.Areas.Admin.Services;
using System;

namespace PerfumeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize] // Kiểm tra đăng nhập cơ bản giống ProductsController
    public class AdminCategoryController : Controller
    {
        private readonly PerfumeStoreContext _context;
        private readonly IPaginationService _paginationService;

        public AdminCategoryController(PerfumeStoreContext context, IPaginationService paginationService)
        {
            _context = context;
            _paginationService = paginationService;
        }

        // GET: Admin/Category
        [RequirePermission("View Categories")]
        public async Task<IActionResult> Index(int page = 1)
        {
            var categoriesQuery = _context.Categories
                .Include(c => c.Products)
                .OrderBy(c => c.CategoryName)
                .AsQueryable();
            
            var pagedResult = await _paginationService.PaginateAsync(categoriesQuery, page, 10);

            var totalCategories = await _context.Categories.CountAsync();
            var totalProducts = await _context.Products.CountAsync();
            var topCategoryStats = await _context.Categories
                .OrderByDescending(c => c.Products.Count)
                .Select(c => new { c.CategoryName, ProductCount = c.Products.Count })
                .FirstOrDefaultAsync();

            ViewBag.TotalCategories = totalCategories;
            ViewBag.TotalCategoryProducts = totalProducts;
            ViewBag.AverageProductsPerCategory = totalCategories > 0
                ? Math.Round((double)totalProducts / Math.Max(totalCategories, 1), 1)
                : 0;
            ViewBag.TopCategoryName = topCategoryStats?.CategoryName;
            ViewBag.TopCategoryProductCount = topCategoryStats?.ProductCount ?? 0;

            return View(pagedResult);
        }

        // GET: Admin/Category/Details/5
        [RequirePermission("View Categories")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .Include(c => c.Products)
                    .ThenInclude(p => p.ProductImages)
                .FirstOrDefaultAsync(m => m.CategoryId == id);
            
            if (category == null)
            {
                return NotFound();
            }

            // Tính toán thống kê
            var totalProducts = await _context.Products.CountAsync();
            var totalCategories = await _context.Categories.CountAsync();
            
            ViewBag.TotalProducts = totalProducts;
            ViewBag.TotalCategories = totalCategories;

            return View(category);
        }

        // GET: Admin/Category/Create
        [RequirePermission("Create Category")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Category/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Create Category")]
        public async Task<IActionResult> Create([Bind("CategoryName")] Category category)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra trùng tên danh mục
                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.CategoryName.ToLower() == category.CategoryName.ToLower());
                
                if (existingCategory != null)
                {
                    ModelState.AddModelError("CategoryName", "Tên danh mục đã tồn tại.");
                    return View(category);
                }

                _context.Add(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm danh mục thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // GET: Admin/Category/Edit/5
        [RequirePermission("Edit Category")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // POST: Admin/Category/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Edit Category")]
        public async Task<IActionResult> Edit(int id, [Bind("CategoryId,CategoryName")] Category category)
        {
            if (id != category.CategoryId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Kiểm tra trùng tên danh mục (trừ chính nó)
                    var existingCategory = await _context.Categories
                        .FirstOrDefaultAsync(c => c.CategoryName.ToLower() == category.CategoryName.ToLower() 
                                                && c.CategoryId != category.CategoryId);
                    
                    if (existingCategory != null)
                    {
                        ModelState.AddModelError("CategoryName", "Tên danh mục đã tồn tại.");
                        return View(category);
                    }

                    _context.Update(category);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật danh mục thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.CategoryId))
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
            return View(category);
        }

        // GET: Admin/Category/Delete/5
        [RequirePermission("Delete Category")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(m => m.CategoryId == id);
            
            if (category == null)
            {
                return NotFound();
            }

            // Double check số lượng sản phẩm từ database
            var actualProductCount = await _context.Products
                .Where(p => p.Categories.Any(c => c.CategoryId == id))
                .CountAsync();

            // Cập nhật ViewBag để hiển thị số lượng chính xác
            ViewBag.ActualProductCount = actualProductCount;

            return View(category);
        }

        // POST: Admin/Category/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [RequirePermission("Delete Category")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.CategoryId == id);
            
            if (category == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy danh mục cần xóa.";
                return RedirectToAction(nameof(Index));
            }

            // Kiểm tra xem danh mục có sản phẩm không (double check)
            var productCount = await _context.Products
                .Where(p => p.Categories.Any(c => c.CategoryId == id))
                .CountAsync();

            if (productCount > 0)
            {
                TempData["ErrorMessage"] = $"Không thể xóa danh mục '{category.CategoryName}' vì còn {productCount} sản phẩm thuộc danh mục này. Vui lòng di chuyển hoặc xóa các sản phẩm trước.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã xóa danh mục '{category.CategoryName}' thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Có lỗi xảy ra khi xóa danh mục: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.CategoryId == id);
        }

        // AJAX endpoint để lấy thống kê nhanh
        [HttpGet]
        [RequirePermission("View Categories")]
        public async Task<IActionResult> GetCategoryStats()
        {
            var stats = await _context.Categories
                .Select(c => new
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    ProductCount = c.Products.Count()
                })
                .OrderByDescending(c => c.ProductCount)
                .ToListAsync();

            return Json(stats);
        }

        // AJAX endpoint để kiểm tra số lượng sản phẩm trong danh mục
        [HttpGet]
        [RequirePermission("View Categories")]
        public async Task<IActionResult> CheckCategoryProducts(int id)
        {
            var productCount = await _context.Products
                .Where(p => p.Categories.Any(c => c.CategoryId == id))
                .CountAsync();

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            return Json(new
            {
                categoryId = id,
                categoryName = category?.CategoryName ?? "Unknown",
                productCount = productCount,
                canDelete = productCount == 0
            });
        }

        // GET: Admin/Category/CategoryDetails/5
        [RequirePermission("View Category Details")]
        public async Task<IActionResult> CategoryDetails(int? id, int page = 1, int pageSize = 12, string? searchTerm = null, string? sortBy = "name", string? sortOrder = "asc")
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound();
            }

            // Query products in this category
            var productsQuery = _context.Products
                .Where(p => p.Categories.Any(c => c.CategoryId == id))
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                productsQuery = productsQuery.Where(p => p.ProductName.Contains(searchTerm));
            }

            // Get total count for pagination
            var totalProducts = await productsQuery.CountAsync();

            // Apply sorting
            productsQuery = sortBy?.ToLower() switch
            {
                "price" => sortOrder?.ToLower() == "desc" 
                    ? productsQuery.OrderByDescending(p => p.Price)
                    : productsQuery.OrderBy(p => p.Price),
                "date" => sortOrder?.ToLower() == "desc"
                    ? productsQuery.OrderByDescending(p => p.ProductId)
                    : productsQuery.OrderBy(p => p.ProductId),
                _ => sortOrder?.ToLower() == "desc"
                    ? productsQuery.OrderByDescending(p => p.ProductName)
                    : productsQuery.OrderBy(p => p.ProductName)
            };

            // Apply pagination
            var products = await productsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

            // Convert to Admin models
            var adminCategory = new PerfumeStore.Areas.Admin.Models.Category
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName
            };

            var adminProducts = products.Select(p => new PerfumeStore.Areas.Admin.Models.Product
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                Price = p.Price,
                Brand = p.Brand != null ? new PerfumeStore.Areas.Admin.Models.Brand
                {
                    BrandId = p.Brand.BrandId,
                    BrandName = p.Brand.BrandName
                } : null,
                ProductImages = p.ProductImages.Select(pi => new PerfumeStore.Areas.Admin.Models.ProductImage
                {
                    ImageId = pi.ImageId,
                    ImageData = pi.ImageData,
                    ImageMimeType = pi.ImageMimeType,
                    ProductId = pi.ProductId
                }).ToList()
            }).ToList();

            var viewModel = new PerfumeStore.Areas.Admin.Models.ViewModels.CategoryDetailsViewModel
            {
                Category = adminCategory,
                Products = adminProducts,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalProducts = totalProducts,
                SearchTerm = searchTerm,
                SortBy = sortBy,
                SortOrder = sortOrder
            };

            return View(viewModel);
        }
    }
}