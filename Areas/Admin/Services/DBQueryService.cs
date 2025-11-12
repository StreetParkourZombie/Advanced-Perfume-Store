using Microsoft.EntityFrameworkCore;
using PerfumeStore.Areas.Admin.Models;

namespace PerfumeStore.Areas.Admin.Services
{
    public class DBQueryService
    {
        public interface IDbQueryService
        {
            /*
                Giải thích:
                1. Tên interface: IDbQueryService
                    + Quy ước đặt tên: tiền tố "I" cho interface, tên rõ ràng, ngắn gọn, thể hiện đúng mục đích.
                    + Mục đích: cung cấp các phương thức truy vấn dữ liệu từ DB (chỉ đọc, không ghi).
                    + Lợi ích: tách biệt rõ ràng giữa các dịch vụ chỉ đọc và dịch vụ ghi dữ liệu, giúp dễ bảo trì, mở rộng.

                2. ct (CancellationToken)
                    + Mục đích: cho phép hủy sớm tác vụ async (truy vấn DB, gọi API, v.v.) khi yêu cầu HTTP bị hủy (client đóng tab, timeout) hoặc ứng dụng đang tắt.
                    + Lợi ích: giải phóng tài nguyên sớm, tránh làm việc thừa, tăng khả năng chịu tải.
             */

            // Categories
            Task<List<Category>> GetAllCategoriesAsync(CancellationToken ct = default);
            Task<List<Category>> GetCategoriesOrderedByNameAsync(CancellationToken ct = default);
            
            // Brands
            Task<List<Brand>> GetAllBrandsAsync(CancellationToken ct = default);
            Task<List<Brand>> GetBrandsOrderedByNameAsync(CancellationToken ct = default);
            Task<bool> BrandExistsAsync(int brandId, CancellationToken ct = default);
            
            // Products
            Task<List<Product>> GetAllProductsAsync(CancellationToken ct = default);
            Task<List<Product>> GetProductsByCategory(int? categoryId, string? searchName = null, CancellationToken ct = default);
            Task<List<Product>> GetProductsWithIncludesAsync(CancellationToken ct = default);
            Task<Product?> GetProductByIdAsync(int productId, CancellationToken ct = default);
            Task<Product?> GetProductWithCategoriesAsync(int productId, CancellationToken ct = default);
            Task<bool> ProductHasOrdersAsync(int productId, CancellationToken ct = default);
            
            // Discount Programs
            Task<List<DiscountProgram>> GetAllDiscountProgramsAsync(CancellationToken ct = default);
            Task<List<DiscountProgram>> GetDiscountProgramsOrderedByNameAsync(CancellationToken ct = default);
            
            // Orders
            Task<List<Order>> GetAllOrdersAsync(CancellationToken ct = default);
            Task<Order?> GetOrderByIdAsync(int orderId, CancellationToken ct = default);
            Task<List<Order>> GetOrdersWithIncludesAsync(CancellationToken ct = default);
            
            // Customers
            Task<List<Customer>> GetAllCustomersAsync(CancellationToken ct = default);
            Task<Customer?> GetCustomerByIdAsync(int customerId, CancellationToken ct = default);
            Task<List<Customer>> GetCustomersWithIncludesAsync(CancellationToken ct = default);
        }


        public class DbQueryService : IDbQueryService
        {
            private readonly PerfumeStoreContext _context;
            public DbQueryService(PerfumeStoreContext context) { _context = context; }

            // ==================================================== CATEGORIES
            public Task<List<Category>> GetAllCategoriesAsync(CancellationToken ct = default)
            {
                return _context.Categories.ToListAsync(ct);
            }

            public Task<List<Category>> GetCategoriesOrderedByNameAsync(CancellationToken ct = default)
            {
                return _context.Categories.OrderBy(c => c.CategoryName).ToListAsync(ct);
            }

            // ==================================================== BRANDS
            public Task<List<Brand>> GetAllBrandsAsync(CancellationToken ct = default)
            {
                return _context.Brands.ToListAsync(ct);
            }

            public Task<List<Brand>> GetBrandsOrderedByNameAsync(CancellationToken ct = default)
            {
                return _context.Brands.OrderBy(b => b.BrandName).ToListAsync(ct);
            }

            public Task<bool> BrandExistsAsync(int brandId, CancellationToken ct = default)
            {
                return _context.Brands.AnyAsync(b => b.BrandId == brandId, ct);
            }

            // ==================================================== PRODUCTS
            public Task<List<Product>> GetAllProductsAsync(CancellationToken ct = default)
            {
                return _context.Products.ToListAsync(ct);
            }

            public async Task<List<Product>> GetProductsByCategory(int? categoryId, string? searchName = null, CancellationToken ct = default)
            {
                var query = _context.Products
                    .Include(p => p.Brand)
                    .Include(p => p.Categories)
                    .Include(p => p.ProductImages)
                    .AsQueryable();

                if (categoryId.HasValue)
                {
                    query = query.Where(p => p.Categories.Any(c => c.CategoryId == categoryId));
                }

                if (!string.IsNullOrWhiteSpace(searchName))
                {
                    query = query.Where(p => p.ProductName.Contains(searchName));
                }

                return await query.OrderByDescending(p => p.ProductId).ToListAsync(ct);
            }

            public Task<List<Product>> GetProductsWithIncludesAsync(CancellationToken ct = default)
            {
                return _context.Products
                    .Include(p => p.Brand)
                    .Include(p => p.Categories)
                    .Include(p => p.ProductImages)
                    .ToListAsync(ct);
            }

            public Task<Product?> GetProductByIdAsync(int productId, CancellationToken ct = default)
            {
                return _context.Products.FindAsync(productId).AsTask();
            }

            public Task<Product?> GetProductWithCategoriesAsync(int productId, CancellationToken ct = default)
            {
                return _context.Products
                    .Include(p => p.Categories)
                    .FirstOrDefaultAsync(p => p.ProductId == productId, ct);
            }

            public Task<bool> ProductHasOrdersAsync(int productId, CancellationToken ct = default)
            {
                return _context.OrderDetails.AnyAsync(od => od.ProductId == productId, ct);
            }

            // ==================================================== DISCOUNT PROGRAMS
            public Task<List<DiscountProgram>> GetAllDiscountProgramsAsync(CancellationToken ct = default)
            {
                return _context.DiscountPrograms.ToListAsync(ct);
            }

            public Task<List<DiscountProgram>> GetDiscountProgramsOrderedByNameAsync(CancellationToken ct = default)
            {
                return _context.DiscountPrograms.OrderBy(d => d.DiscountName).ToListAsync(ct);
            }

            // ==================================================== ORDERS
            public Task<List<Order>> GetAllOrdersAsync(CancellationToken ct = default)
            {
                return _context.Orders.ToListAsync(ct);
            }

            public Task<Order?> GetOrderByIdAsync(int orderId, CancellationToken ct = default)
            {
                return _context.Orders.FindAsync(orderId).AsTask();
            }

            public Task<List<Order>> GetOrdersWithIncludesAsync(CancellationToken ct = default)
            {
                return _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.Address)
                    .Include(o => o.Coupon)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                            .ThenInclude(p => p.Brand)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                            .ThenInclude(p => p.ProductImages)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync(ct);
            }

            // ==================================================== CUSTOMERS
            public Task<List<Customer>> GetAllCustomersAsync(CancellationToken ct = default)
            {
                return _context.Customers.ToListAsync(ct);
            }

            public Task<Customer?> GetCustomerByIdAsync(int customerId, CancellationToken ct = default)
            {
                return _context.Customers.FindAsync(customerId).AsTask();
            }

            public Task<List<Customer>> GetCustomersWithIncludesAsync(CancellationToken ct = default)
            {
                return _context.Customers
                    .Include(c => c.Membership)
                    .Include(c => c.Orders)
                    .OrderByDescending(c => c.CreatedDate)
                    .ToListAsync(ct);
            }
        }
    }
}
