using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerfumeStore.Models;
using System.Linq;

namespace PerfumeStore.Controllers
{
    public class SearchController : Controller
    {
        private readonly PerfumeStoreContext _context;

        public SearchController(PerfumeStoreContext context)
        {
            _context = context;
        }

        // ✅ Trang kết quả tìm kiếm
        public IActionResult Index(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                ViewBag.Query = "";
                return View(new List<Product>());
            }

            var results = _context.Products
                .Include(p => p.Discount)
                .Include(p => p.ProductImages)
                .Where(p => (p.IsPublished ?? false) &&
                            ((p.ProductName ?? "").Contains(query) ||
                             (p.SuggestionName ?? "").Contains(query)))
                .OrderByDescending(p => p.ProductId)
                .Take(50)
                .ToList();

            ViewBag.Query = query;
            return View(results);
        }

        [HttpGet]
        public JsonResult SearchProducts(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return Json(Enumerable.Empty<object>());

            var data = _context.Products
                .Include(p => p.ProductImages)
                .Where(p => (p.IsPublished ?? false) &&
                            (p.ProductName.Contains(keyword) || p.SuggestionName.Contains(keyword)))
                .Select(p => new
                {
                    id = p.ProductId,
                    name = p.ProductName,
                    price = p.DiscountPrice ?? p.Price,
                    image = p.ProductImages
                        .Select(i => "/Product/GetImage/" + i.ImageId)
                        .FirstOrDefault() ?? "/images/placeholder.jpg"
                })
                .Take(8)
                .ToList();

            return Json(data);
        }
    }
}
