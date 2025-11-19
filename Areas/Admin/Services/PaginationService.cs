using Microsoft.EntityFrameworkCore;
using PerfumeStore.Areas.Admin.Models;

namespace PerfumeStore.Areas.Admin.Services
{
    /// <summary>
    /// Interface cho PaginationService
    /// </summary>
    public interface IPaginationService
    {
        /// <summary>
        /// Phân trang cho IQueryable
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu</typeparam>
        /// <param name="query">IQueryable cần phân trang</param>
        /// <param name="page">Trang hiện tại (mặc định 1)</param>
        /// <param name="pageSize">Số items mỗi trang (mặc định 10)</param>
        /// <returns>PagedResult chứa dữ liệu đã phân trang</returns>
        Task<PagedResult<T>> PaginateAsync<T>(IQueryable<T> query, int page = 1, int pageSize = 10);

        /// <summary>
        /// Phân trang cho List (sau khi đã filter)
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu</typeparam>
        /// <param name="items">List cần phân trang</param>
        /// <param name="page">Trang hiện tại (mặc định 1)</param>
        /// <param name="pageSize">Số items mỗi trang (mặc định 10)</param>
        /// <returns>PagedResult chứa dữ liệu đã phân trang</returns>
        PagedResult<T> Paginate<T>(List<T> items, int page = 1, int pageSize = 10);
    }

    /// <summary>
    /// Service xử lý phân trang
    /// </summary>
    public class PaginationService : IPaginationService
    {
        private const int DefaultPageSize = 10;
        private const int MaxPageSize = 100;

        /// <summary>
        /// Phân trang cho IQueryable (tối ưu cho database)
        /// </summary>
        public async Task<PagedResult<T>> PaginateAsync<T>(IQueryable<T> query, int page = 1, int pageSize = 10)
        {
            // Validate và normalize parameters
            page = Math.Max(1, page);
            pageSize = Math.Max(1, Math.Min(pageSize, MaxPageSize));

            // Đếm tổng số items (chỉ đếm, không load toàn bộ)
            var totalItems = await query.CountAsync();

            // Lấy items cho trang hiện tại
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<T>
            {
                Items = items,
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems
            };
        }

        /// <summary>
        /// Phân trang cho List (sau khi đã filter trong memory)
        /// </summary>
        public PagedResult<T> Paginate<T>(List<T> items, int page = 1, int pageSize = 10)
        {
            // Validate và normalize parameters
            page = Math.Max(1, page);
            pageSize = Math.Max(1, Math.Min(pageSize, MaxPageSize));

            var totalItems = items.Count;

            // Lấy items cho trang hiện tại
            var pagedItems = items
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<T>
            {
                Items = pagedItems,
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems
            };
        }
    }
}


