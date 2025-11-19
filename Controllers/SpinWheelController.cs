using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using PerfumeStore.Models;
using PerfumeStore.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace PerfumeStore.Controllers
{
    public class SpinWheelController : Controller
    {
        private readonly PerfumeStoreContext _context;
        private readonly ILogger<SpinWheelController> _logger;

        public SpinWheelController(PerfumeStoreContext context, ILogger<SpinWheelController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Vòng Quay Voucher";

            var customerId = GetCurrentCustomerId();
            var remainingSpins = GetRemainingSpins(customerId);
            var dailySpins = GetDailySpins(customerId);
            var availableVouchers = await GetAvailableCouponsAsync();

            var model = new SpinWheelViewModel
            {
                RemainingSpins = remainingSpins,
                DailySpins = dailySpins,
                IsLoggedIn = customerId.HasValue,
                AvailableVouchers = availableVouchers
            };

            return View(model);
        }

        private int? GetCurrentCustomerId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int customerId))
            {
                return customerId;
            }
            return null;
        }

        private int GetRemainingSpins(int? customerId)
        {
            if (!customerId.HasValue)
            {
                // Guest có 3 lần quay
                var guestSpins = HttpContext.Session.GetInt32("GuestSpins");
                if (guestSpins == null)
                {
                    HttpContext.Session.SetInt32("GuestSpins", 3);
                    return 3;
                }
                return guestSpins.Value;
            }

            var customer = _context.Customers.Find(customerId.Value);
            if (customer == null) return 3;

            // Đảm bảo SpinNumber luôn là 3 nếu null hoặc <= 0
            if (customer.SpinNumber == null || customer.SpinNumber <= 0)
            {
                customer.SpinNumber = 3;
                _context.SaveChanges();
            }

            return customer.SpinNumber.Value;
        }

        private int GetDailySpins(int? customerId)
        {
            return 3; // Mặc định 3 lần/ngày
        }

        [HttpPost]
        public async Task<IActionResult> Spin()
        {
            try
            {
                var customerId = GetCurrentCustomerId();
                var remainingSpins = GetRemainingSpins(customerId);

                // Kiểm tra số lần quay còn lại
                if (remainingSpins <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "🎯 Bạn đã hết lượt quay hôm nay! Hãy quay lại vào ngày mai nhé!",
                        remainingSpins = remainingSpins
                    });
                }

                // Danh sách voucher có sẵn từ database
                var vouchers = await GetAvailableCouponsAsync();
                if (!vouchers.Any())
                {
                    return Json(new
                    {
                        success = false,
                        message = "Hiện chưa có coupon nào trên vòng quay, vui lòng quay lại sau!",
                        remainingSpins
                    });
                }

                VoucherModel? selectedVoucher = null;
                int selectedIndex = -1;

                if (customerId.HasValue)
                {
                    var selectionPool = new List<VoucherModel>(vouchers);
                    while (selectionPool.Any())
                    {
                        var candidateVoucher = SelectVoucherByProbability(selectionPool, out _);
                        var assignmentSucceeded = await AssignCouponToCustomerAsync(candidateVoucher.Id, customerId.Value);

                        if (assignmentSucceeded)
                        {
                            selectedVoucher = candidateVoucher;
                            selectedIndex = Math.Max(0, vouchers.FindIndex(v => v.Id == candidateVoucher.Id));
                            break;
                        }

                        selectionPool.RemoveAll(v => v.Id == candidateVoucher.Id);
                    }

                    if (selectedVoucher == null)
                    {
                        return Json(new
                        {
                            success = false,
                            message = "🎟️ Các coupon vừa được nhận hết, vui lòng thử lại!",
                            remainingSpins
                        });
                    }
                }
                else
                {
                    selectedVoucher = SelectVoucherByProbability(vouchers, out selectedIndex);
                }

                // Giảm số lần quay
                if (selectedVoucher == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Không thể xác định voucher, vui lòng thử lại!",
                        remainingSpins
                    });
                }

                if (customerId.HasValue)
                {
                    // Đã đăng nhập - cập nhật database
                    var customer = await _context.Customers.FindAsync(customerId.Value);
                    if (customer != null)
                    {
                        customer.SpinNumber = Math.Max(0, customer.SpinNumber.Value - 1);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    // Guest - cập nhật session
                    var guestSpins = HttpContext.Session.GetInt32("GuestSpins") ?? 3;
                    HttpContext.Session.SetInt32("GuestSpins", Math.Max(0, guestSpins - 1));
                }

                // Lưu voucher vào session nếu trúng
                if (selectedVoucher.Type != "none")
                {
                    HttpContext.Session.SetString("AppliedVoucher", JsonSerializer.Serialize(selectedVoucher));
                }

                // Tính góc quay với animation mượt mà
                var finalAngle = CalculateSpinAngle(selectedIndex, vouchers.Count);

                var newRemainingSpins = GetRemainingSpins(customerId);
                var updatedVouchers = await GetAvailableCouponsAsync();

                _logger.LogInformation($"Spin completed for customer {customerId}: {selectedVoucher.Name}");

                _logger.LogInformation($"Selected voucher: {selectedVoucher.Name} ({selectedVoucher.Code})");

                return Json(new
                {
                    success = true,
                    voucher = selectedVoucher,
                    angle = finalAngle,
                    remainingSpins = newRemainingSpins,
                    availableVouchers = updatedVouchers,
                    message = GetSpinMessage(selectedVoucher),
                    animation = GetAnimationType(selectedVoucher)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Spin action");
                return Json(new
                {
                    success = false,
                    message = "Có lỗi xảy ra, vui lòng thử lại!"
                });
            }
        }

        private List<VoucherModel> GetVoucherPool()
        {
            return new List<VoucherModel>
            {
                new VoucherModel { Id = 1, Code = "FREESHIP", Name = "Miễn phí ship", Value = 0, Type = "freeship", Color = "#667eea", Probability = 12 },
                new VoucherModel { Id = 2, Code = "NONE", Name = "Chúc may mắn lần sau", Value = 0, Type = "none", Color = "#f093fb", Probability = 10 },
                new VoucherModel { Id = 3, Code = "LUCKY15", Name = "Giảm 15%", Value = 15, Type = "percent", Color = "#4facfe", Probability = 15 },
                new VoucherModel { Id = 4, Code = "LUCKY10", Name = "Giảm 10%", Value = 10, Type = "percent", Color = "#43e97b", Probability = 20 },
                new VoucherModel { Id = 5, Code = "LUCKY20", Name = "Giảm 20%", Value = 20, Type = "percent", Color = "#fa709a", Probability = 18 },
                new VoucherModel { Id = 6, Code = "LUCKY30", Name = "Giảm 30%", Value = 30, Type = "percent", Color = "#a8edea", Probability = 12 },
                new VoucherModel { Id = 7, Code = "CASH50K", Name = "Giảm 50.000đ", Value = 50000, Type = "amount", Color = "#ff9a9e", Probability = 8 },
                new VoucherModel { Id = 8, Code = "CASH100K", Name = "Giảm 100.000đ", Value = 100000, Type = "amount", Color = "#ffecd2", Probability = 5 }
            };
        }

        private VoucherModel SelectVoucherByProbability(List<VoucherModel> vouchers, out int selectedIndex)
        {
            var random = new Random();
            var totalProbability = vouchers.Sum(v => v.Probability);
            var randomNumber = random.Next(1, totalProbability + 1);

            var currentProbability = 0;
            for (var i = 0; i < vouchers.Count; i++)
            {
                currentProbability += vouchers[i].Probability;
                if (randomNumber <= currentProbability)
                {
                    selectedIndex = i;
                    return vouchers[i];
                }
            }

            selectedIndex = vouchers.Count - 1;
            return vouchers.Last(); // Fallback
        }

        private double CalculateSpinAngle(int voucherIndex, int totalSlots)
        {
            var random = new Random();
            var spins = 5 + random.Next(3); // 5-7 vòng quay
            var slotCount = Math.Max(1, totalSlots);
            var sectorAngle = 360.0 / slotCount;
            var targetAngle = voucherIndex * sectorAngle + (sectorAngle / 2); // Giữa sector
            var finalAngle = spins * 360 + targetAngle;

            return finalAngle;
        }

        private string GetSpinMessage(VoucherModel voucher)
        {
            return voucher.Type switch
            {
                "none" => "🎯 Chúc may mắn lần sau! Hãy thử lại nhé!",
                "bonus" => "🎉 Chúc mừng! Bạn đã trúng quà tặng đặc biệt!",
                "freeship" => "🚚 Tuyệt vời! Bạn được miễn phí vận chuyển!",
                "percent" => $"🎊 Xuất sắc! Bạn được giảm {voucher.Value}% cho đơn hàng tiếp theo!",
                "amount" => $"💰 Hoàn hảo! Bạn được giảm {voucher.Value:N0}đ cho đơn hàng tiếp theo!",
                _ => "🎁 Chúc mừng bạn đã trúng thưởng!"
            };
        }

        private string GetAnimationType(VoucherModel voucher)
        {
            return voucher.Type switch
            {
                "none" => "shake",
                "bonus" => "confetti",
                "freeship" => "bounce",
                "percent" => "pulse",
                "amount" => "glow",
                _ => "fadeIn"
            };
        }

        [HttpPost]
        public async Task<IActionResult> ApplyVoucher([FromBody] VoucherRequestModel model)
        {
            _logger.LogInformation($"ApplyVoucher called with code: {model?.Code}");

            if (model == null || string.IsNullOrEmpty(model.Code))
                return Json(new { success = false, message = "❌ Mã voucher không hợp lệ" });

            VoucherModel? voucher = null;

            // Bước 1: Tìm coupon trong database trước
            var now = DateTime.Now;
            var codeLower = model.Code.ToLower();
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c =>
                    c.Code != null &&
                    c.Code.ToLower() == codeLower &&
                    (c.IsUsed == null || c.IsUsed == false) &&
                    (c.ExpiryDate == null || c.ExpiryDate >= now) &&
                    c.DiscountAmount.HasValue &&
                    c.DiscountAmount.Value > 0);

            if (coupon != null)
            {
                // Chuyển đổi coupon từ database thành VoucherModel
                voucher = new VoucherModel
                {
                    Id = coupon.CouponId,
                    Code = coupon.Code!,
                    Name = $"Giảm {coupon.DiscountAmount.Value:N0}đ",
                    Value = coupon.DiscountAmount.Value,
                    Type = "amount",
                    Color = "#4facfe",
                    Description = "Mã giảm giá từ admin",
                    ExpiryDate = coupon.ExpiryDate,
                    IsActive = true
                };
                _logger.LogInformation($"Found coupon in database: {voucher.Name} ({voucher.Code})");
            }
            else
            {
                // Bước 2: Nếu không tìm thấy trong database, tìm trong danh sách voucher mặc định
                var vouchers = GetVoucherPool();
                voucher = vouchers.FirstOrDefault(v => v.Code.Equals(model.Code, StringComparison.OrdinalIgnoreCase));
            }

            if (voucher == null)
            {
                _logger.LogWarning($"Voucher not found: {model.Code}");
                return Json(new { success = false, message = "❌ Mã voucher không tồn tại" });
            }

            // Cộng dồn nếu cùng mã đang tồn tại trong session
            var existingJson = HttpContext.Session.GetString("AppliedVoucher");
            if (!string.IsNullOrEmpty(existingJson))
            {
                try
                {
                    var existing = JsonSerializer.Deserialize<VoucherModel>(existingJson);
                    if (existing != null && existing.Code.Equals(voucher.Code, StringComparison.OrdinalIgnoreCase))
                    {
                        // Cộng dồn theo loại
                        existing.TimesApplied += 1;
                        if (existing.Type == "amount")
                        {
                            existing.AccumulatedValue += voucher.Value;
                        }
                        else if (existing.Type == "percent")
                        {
                            existing.AccumulatedValue += voucher.Value; // tổng % (có thể hạn chế tối đa 100% ở lúc tính tiền)
                        }
                        else if (existing.Type == "freeship")
                        {
                            existing.AccumulatedValue = 1; // flag miễn phí ship
                        }

                        var mergedJson = JsonSerializer.Serialize(existing);
                        HttpContext.Session.SetString("AppliedVoucher", mergedJson);
                        _logger.LogInformation($"Voucher stacked: {existing.Name} x{existing.TimesApplied}, Accum = {existing.AccumulatedValue}");
                        return Json(new { success = true, message = $"✅ Đã cộng dồn {existing.Name} (x{existing.TimesApplied})!", voucher = existing });
                    }
                }
                catch { /* ignore parse errors and overwrite below */ }
            }

            // Nếu không trùng mã, ghi voucher mới
            voucher.TimesApplied = 1;
            voucher.AccumulatedValue = voucher.Value;
            var voucherJson = JsonSerializer.Serialize(voucher);
            HttpContext.Session.SetString("AppliedVoucher", voucherJson);
            _logger.LogInformation($"Voucher applied successfully: {voucher.Name} ({voucher.Code})");
            _logger.LogInformation($"Voucher JSON saved to session: {voucherJson}");

            return Json(new { success = true, message = $"✅ Áp dụng {voucher.Name} thành công!", voucher });
        }

        [HttpGet]
        public IActionResult TestSession()
        {
            var voucherJson = HttpContext.Session.GetString("AppliedVoucher");
            _logger.LogInformation($"TestSession - Voucher JSON: {voucherJson}");

            if (string.IsNullOrEmpty(voucherJson))
            {
                return Json(new { success = false, message = "No voucher in session" });
            }

            try
            {
                var voucher = JsonSerializer.Deserialize<VoucherModel>(voucherJson);
                return Json(new { success = true, voucher = voucher });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deserializing voucher: {ex.Message}");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ResetDailySpins()
        {
            try
            {
                var customers = await _context.Customers.ToListAsync();
                foreach (var customer in customers)
                {
                    customer.SpinNumber = 3;
                }
                await _context.SaveChangesAsync();

                _logger.LogInformation("Daily spins reset for all customers");
                return Json(new { success = true, message = "✅ Đã reset số lần quay cho tất cả khách hàng!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting daily spins");
                return Json(new { success = false, message = $"❌ Lỗi: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ResetMySpins()
        {
            try
            {
                var customerId = GetCurrentCustomerId();
                if (!customerId.HasValue)
                {
                    // Reset cho guest
                    HttpContext.Session.SetInt32("GuestSpins", 3);
                    return Json(new { success = true, message = "✅ Đã reset số lần quay của bạn về 3!", remainingSpins = 3 });
                }

                var customer = await _context.Customers.FindAsync(customerId.Value);
                if (customer != null)
                {
                    customer.SpinNumber = 3;
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation($"Spins reset for customer {customerId}");
                return Json(new { success = true, message = "✅ Đã reset số lần quay của bạn về 3!", remainingSpins = 3 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting customer spins");
                return Json(new { success = false, message = $"❌ Lỗi: {ex.Message}" });
            }
        }

        [HttpGet]
        public IActionResult GetRemainingSpins()
        {
            var customerId = GetCurrentCustomerId();
            var remainingSpins = GetRemainingSpins(customerId);
            var dailySpins = GetDailySpins(customerId);

            return Json(new
            {
                remainingSpins = remainingSpins,
                dailySpins = dailySpins,
                isLoggedIn = customerId.HasValue
            });
        }

        [HttpGet]
        public IActionResult GetVoucherInfo()
        {
            var voucherJson = HttpContext.Session.GetString("AppliedVoucher");
            if (string.IsNullOrEmpty(voucherJson))
            {
                return Json(new { hasVoucher = false });
            }

            try
            {
                var voucher = JsonSerializer.Deserialize<VoucherModel>(voucherJson);
                return Json(new { hasVoucher = true, voucher = voucher });
            }
            catch
            {
                return Json(new { hasVoucher = false });
            }
        }

        private async Task<List<VoucherModel>> GetAvailableCouponsAsync()
        {
            var baseColors = new[]
            {
                "linear-gradient(135deg, #ffecd2 0%, #fcb69f 100%)",
                "linear-gradient(135deg, #a18cd1 0%, #fbc2eb 100%)",
                "linear-gradient(135deg, #f6d365 0%, #fda085 100%)",
                "linear-gradient(135deg, #84fab0 0%, #8fd3f4 100%)",
                "linear-gradient(135deg, #cfd9df 0%, #e2ebf0 100%)",
                "linear-gradient(135deg, #ff9a9e 0%, #fecfef 100%)",
                "linear-gradient(135deg, #fbc2eb 0%, #a6c1ee 100%)",
                "linear-gradient(135deg, #fddb92 0%, #d1fdff 100%)",
                "linear-gradient(135deg, #9890e3 0%, #b1f4cf 100%)",
                "linear-gradient(135deg, #f6e58d 0%, #ffbe76 100%)"
            };

            var coupons = await _context.Coupons
                .Where(c =>
                    (c.IsUsed == null || c.IsUsed == false) &&
                    c.CustomerId == null &&
                    (c.ExpiryDate == null || c.ExpiryDate >= DateTime.Now))
                .OrderBy(c => c.ExpiryDate ?? DateTime.MaxValue)
                .ThenBy(c => c.CouponId)
                .ToListAsync();

            if (!coupons.Any())
            {
                return new List<VoucherModel>();
            }

            var equalProbability = Math.Max(1, 100 / coupons.Count);

            var vouchers = coupons.Select((coupon, index) => new VoucherModel
            {
                Id = coupon.CouponId,
                Name = coupon.DiscountAmount.HasValue
                    ? $"Giảm {coupon.DiscountAmount.Value:N0}đ"
                    : $"Coupon #{coupon.CouponId}",
                Code = coupon.Code ?? $"CP{coupon.CouponId}",
                Value = coupon.DiscountAmount ?? 0,
                Type = "amount",
                Color = baseColors[index % baseColors.Length],
                Probability = equalProbability,
                Description = coupon.ExpiryDate.HasValue
                    ? $"Hạn sử dụng: {coupon.ExpiryDate:dd/MM/yyyy}"
                    : "Không có hạn sử dụng",
                ExpiryDate = coupon.ExpiryDate,
                IsActive = true
            }).ToList();

            // Bổ sung xác suất cho phần dư để tổng ~100
            var totalProbability = vouchers.Sum(v => v.Probability);
            if (totalProbability < 100 && vouchers.Any())
            {
                vouchers[0].Probability += (100 - totalProbability);
            }

            return vouchers;
        }

        public class VoucherRequestModel
        {
            public string Code { get; set; } = "";
        }

        private async Task<bool> AssignCouponToCustomerAsync(int couponId, int customerId)
        {
            var rowsAffected = await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE [Coupons]
                SET [CustomerId] = {customerId},
                    [IsUsed] = CASE WHEN [IsUsed] IS NULL THEN 0 ELSE [IsUsed] END,
                    [UsedDate] = NULL
                WHERE [CouponId] = {couponId} AND [CustomerId] IS NULL");

            return rowsAffected > 0;
        }
    }
}