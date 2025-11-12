namespace PerfumeStore.Services
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.EntityFrameworkCore;
    using PerfumeStore.Models;

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

        Task<List<Category>> GetAllCategoriesAsync(CancellationToken ct = default);
        Task<List<Brand>> GetAllBrandsAsync(CancellationToken ct = default);
        Task<List<Product>> GetAllProductsAsync(CancellationToken ct = default);
        Task<List<Product>> GetProductsByCategory(string? name, CancellationToken ct = default);
        Task<List<Comment>> GetCommentsByProductIdAsync(int productId, CancellationToken ct = default);
    }


    public class DbQueryService : IDbQueryService
    {
        private readonly PerfumeStoreContext _context;
        public DbQueryService(PerfumeStoreContext context) { _context = context; }

        // ==================================================== BRANDS
        // Lấy tất cả thương hiệu
        public Task<List<Brand>> GetAllBrandsAsync(CancellationToken ct = default)
        {
            return _context.Brands.ToListAsync(ct);
        }

        // ==================================================== CATEGORIES
        // Lấy tất cả danh mục
        public Task<List<Category>> GetAllCategoriesAsync(CancellationToken ct = default)
        {
            return _context.Categories.ToListAsync(ct);
        }

        // ==================================================== COMMENTS
        // Lấy tất cả comment của một sản phẩm kèm thông tin khách hàng
        public Task<List<Comment>> GetCommentsByProductIdAsync(int productId, CancellationToken ct = default)
        {
            return _context.Comments
                .Include(c => c.Customer)
                .Where(c => c.ProductId == productId && c.IsPublished == true)
                .OrderByDescending(c => c.CommentDate)
                .ToListAsync(ct);
        }

        // ==================================================== COUPONS


        // ==================================================== CUSTOMERS

        // ==================================================== DISCOUNT PROGRAMS

        // ==================================================== FEES

        // ==================================================== LITERS

        // ==================================================== MEMBERSHIPS

        // ==================================================== ORDERS

        // ==================================================== PRODUCTS
        // Lấy tất cả sản phẩm đã xuất bản kèm hình ảnh và thương hiệu
        public Task<List<Product>> GetAllProductsAsync(CancellationToken ct = default)
        {
            return _context.Products
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .Where(p => p.IsPublished == true)
                .ToListAsync(ct);
        }

        // Lấy sản phẩm publish theo tên loại (ưu tiên khớp Category trước)
        public async Task<List<Product>> GetProductsByCategory(string? name, CancellationToken ct = default)
        {
            var baseQuery = _context.Products
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .Where(p => p.IsPublished == true)
                .AsQueryable();

            if (string.IsNullOrWhiteSpace(name))
            {
                return await baseQuery.ToListAsync(ct);
            }

            var normalized = name.Trim();

            // 1) Ưu tiên tìm theo Category (khớp tuyệt đối trước, sau đó LIKE)
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryName == normalized, ct)
                ?? await _context.Categories
                    .FirstOrDefaultAsync(c => EF.Functions.Like(c.CategoryName, "%" + normalized + "%"), ct);

            if (category != null)
            {
                return await baseQuery
                    .Where(p => p.Categories.Any(c => c.CategoryId == category.CategoryId))
                    .ToListAsync(ct);
            }

            // 2) Không khớp category, trả về tất cả (có thể mở rộng lọc theo Brand/Scent sau)
            return await baseQuery.ToListAsync(ct);
        }

        // ==================================================== PRODUCT IMAGES

        // ==================================================== SHIPPING ADDRESSES

        // ==================================================== WARRANTIES

        // ==================================================== WARRENTY CLAIMS
    }
}
