using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerfumeStore.Models;
using PerfumeStore.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PerfumeStore.Controllers
{
    public class CategoryController : Controller
    {
        private readonly IDbQueryService _dbQueryService;
        private readonly PerfumeStoreContext _context;

        public CategoryController(IDbQueryService dbQueryService, PerfumeStoreContext context)
        {
            _dbQueryService = dbQueryService;
            _context = context;
        }

        public async Task<IActionResult> Index(string? categoryName)
        {
            var ct = HttpContext.RequestAborted;

            if (string.IsNullOrEmpty(categoryName))
                return RedirectToAction("Index", "Home");

            var products = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Discount)
                .Include(p => p.ProductImages)
                .Include(p => p.Categories)
                .Where(p => p.Categories.Any(c => c.CategoryName == categoryName))
                .ToListAsync(ct);

            var allProducts = await _context.Products.ToListAsync(ct);
            var brands = await _context.Brands.ToListAsync(ct);

            var scentChoices = allProducts
                .Where(p => !string.IsNullOrEmpty(p.Scent))
                .SelectMany(p => p.Scent!.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s)
                .ToList();

            ViewBag.Brands = brands;
            ViewBag.ScentChoices = scentChoices;
            ViewBag.CurrentCategory = categoryName;
            ViewData["Title"] = categoryName;

            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> FilterProducts(string categoryName, string? scent, string? brandName, string? priceRange)
        {
            var ct = HttpContext.RequestAborted;

            var products = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Discount)
                .Include(p => p.ProductImages)
                .Include(p => p.Categories)
                .Where(p => p.Categories.Any(c => c.CategoryName == categoryName))
                .ToListAsync(ct);

            // 🔹 Lọc nhóm hương
            if (!string.IsNullOrEmpty(scent))
            {
                products = products
                    .Where(p => !string.IsNullOrEmpty(p.Scent)
                                && p.Scent.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                                    .Any(s => s.Equals(scent, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            if (!string.IsNullOrEmpty(brandName))
            {
                products = products
                    .Where(p => p.Brand != null &&
                                p.Brand.BrandName.Contains(brandName, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrEmpty(priceRange))
            {
                products = products.Where(p =>
                {
                    var discountPercent = p.Discount?.DiscountPercent ?? 0;
                    var effectivePrice = discountPercent > 0
                        ? p.Price * (1 - discountPercent / 100m)
                        : p.Price;

                    return priceRange switch
                    {
                        "under2" => effectivePrice < 2_000_000,
                        "2to4" => effectivePrice >= 2_000_000 && effectivePrice <= 4_000_000,
                        "above4" => effectivePrice > 4_000_000,
                        _ => true
                    };
                }).ToList();
            }

            return PartialView("CategoryProductList", products);
        }
    }
}
