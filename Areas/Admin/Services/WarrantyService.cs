using Microsoft.EntityFrameworkCore;
using PerfumeStore.Models;

namespace PerfumeStore.Areas.Admin.Services
{
    public interface IWarrantyService
    {
        Task<bool> CreateWarrantyForOrderDetailAsync(int orderDetailId, int customerId, int warrantyPeriodMonths);
        Task<int> CreateWarrantiesForOrderAsync(int orderId);
        Task<int> DeleteWarrantiesForOrderAsync(int orderId);
        Task<bool> UpdateWarrantyStatusAsync(int warrantyId, string status);
        Task<List<Warranty>> GetExpiringWarrantiesAsync(int daysBeforeExpiry = 30);
    }

    public class WarrantyService : IWarrantyService
    {
        private readonly PerfumeStoreContext _context;

        public WarrantyService(PerfumeStoreContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Tạo bảo hành cho một OrderDetail cụ thể
        /// </summary>
        public async Task<bool> CreateWarrantyForOrderDetailAsync(int orderDetailId, int customerId, int warrantyPeriodMonths)
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

        /// <summary>
        /// Tạo bảo hành cho tất cả sản phẩm trong đơn hàng
        /// Được gọi khi admin set đơn hàng ở trạng thái "Đã giao hàng"
        /// </summary>
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
                        var success = await CreateWarrantyForOrderDetailAsync(
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

        /// <summary>
        /// Xóa tất cả bảo hành của đơn hàng
        /// Được gọi khi admin set đơn hàng ở trạng thái khác "Đã giao hàng"
        /// </summary>
        public async Task<int> DeleteWarrantiesForOrderAsync(int orderId)
        {
            try
            {
                // Lấy tất cả OrderDetailIds của đơn hàng
                var orderDetailIds = await _context.OrderDetails
                    .Where(od => od.OrderId == orderId)
                    .Select(od => od.OrderDetailId)
                    .ToListAsync();

                if (!orderDetailIds.Any())
                {
                    return 0;
                }

                // Tìm tất cả bảo hành liên quan đến các OrderDetail này
                var warranties = await _context.Warranties
                    .Include(w => w.WarrantyClaims)
                    .Where(w => orderDetailIds.Contains(w.OrderDetailId))
                    .ToListAsync();

                int deletedCount = warranties.Count;

                // Xóa tất cả WarrantyClaims trước
                foreach (var warranty in warranties)
                {
                    if (warranty.WarrantyClaims.Any())
                    {
                        _context.WarrantyClaims.RemoveRange(warranty.WarrantyClaims);
                    }
                }

                // Sau đó xóa các Warranty
                _context.Warranties.RemoveRange(warranties);
                await _context.SaveChangesAsync();

                return deletedCount;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>
        /// Cập nhật trạng thái bảo hành
        /// </summary>
        public async Task<bool> UpdateWarrantyStatusAsync(int warrantyId, string status)
        {
            try
            {
                var warranty = await _context.Warranties.FindAsync(warrantyId);
                if (warranty == null)
                    return false;

                warranty.Status = status;
                warranty.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Lấy danh sách bảo hành sắp hết hạn
        /// Dùng để gửi thông báo cho khách hàng
        /// </summary>
        public async Task<List<Warranty>> GetExpiringWarrantiesAsync(int daysBeforeExpiry = 30)
        {
            var expiryDate = DateTime.Now.AddDays(daysBeforeExpiry);
            
            return await _context.Warranties
                .Where(w => w.Status == "Active" && 
                           w.EndDate <= expiryDate && 
                           w.EndDate > DateTime.Now)
                .OrderBy(w => w.EndDate)
                .ToListAsync();
        }

        /// <summary>
        /// Tự động cập nhật trạng thái bảo hành hết hạn
        /// Nên chạy định kỳ (daily job)
        /// </summary>
        public async Task<int> UpdateExpiredWarrantiesAsync()
        {
            try
            {
                var expiredWarranties = await _context.Warranties
                    .Where(w => w.Status == "Active" && w.EndDate < DateTime.Now)
                    .ToListAsync();

                foreach (var warranty in expiredWarranties)
                {
                    warranty.Status = "Expired";
                    warranty.UpdatedDate = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                return expiredWarranties.Count;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private string GenerateWarrantyCode()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(1000, 9999);
            return $"WR{timestamp}{random}";
        }
    }
}