using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PerfumeStore.Areas.Admin.Models;
using PerfumeStore.Areas.Admin.Filters;
using OfficeOpenXml;

namespace PerfumeStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class DashboardController : Controller
    {
        private readonly PerfumeStore.Areas.Admin.Models.PerfumeStoreContext _context;

        public DashboardController(PerfumeStore.Areas.Admin.Models.PerfumeStoreContext context)
        {
            _context = context;
        }

        [RequirePermission("View Dashboard")]
        public async Task<IActionResult> Index(DashboardFilterViewModel filter)
        {
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var startOfYear = new DateTime(today.Year, 1, 1);
            var lastMonth = startOfMonth.AddMonths(-1);
            var endOfLastMonth = startOfMonth.AddDays(-1);

            var viewModel = new DashboardViewModel();
            viewModel.Filter = filter ?? new DashboardFilterViewModel();

            try
            {
                // Setup filter options
                await SetupFilterOptions(viewModel.Filter);

                // Check if any specific filter is applied
                bool hasFilter = viewModel.Filter.FromDate.HasValue || 
                               viewModel.Filter.ToDate.HasValue ||
                               viewModel.Filter.Month.HasValue || 
                               viewModel.Filter.BrandId.HasValue ||
                               (!string.IsNullOrEmpty(viewModel.Filter.TimeType) && viewModel.Filter.TimeType != "month") ||
                               (viewModel.Filter.TimeType == "year" && viewModel.Filter.Year != DateTime.Now.Year) ||
                               (!string.IsNullOrEmpty(viewModel.Filter.TimeType));

                // Debug logging
                Console.WriteLine($"Filter Debug - TimeType: '{viewModel.Filter.TimeType}', Month: {viewModel.Filter.Month}, Year: {viewModel.Filter.Year}, BrandId: {viewModel.Filter.BrandId}, HasFilter: {hasFilter}");

                // Always load filtered data (even if no filter, use default range)
                await LoadFilteredData(viewModel, viewModel.Filter);
                
                if (hasFilter)
                {
                    // Set summary cards to filtered data
                    viewModel.TongDoanhThuHomNay = 0; // Will be set by filter
                    viewModel.TongDoanhThuThangNay = viewModel.TongDoanhThuLoc;
                    viewModel.TongDoanhThuNamNay = viewModel.TongDoanhThuLoc;
                    viewModel.TangTruongThang = 0; // No comparison for filtered data
                    
                    // Count orders in filtered period
                    DateTime startDate, endDate;
                    GetDateRange(viewModel.Filter, out startDate, out endDate);
                    
                    var filteredQuery = _context.Orders.Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate);
                    
                    if (viewModel.Filter.BrandId.HasValue)
                    {
                        filteredQuery = filteredQuery.Where(o => o.OrderDetails.Any(od => od.Product.BrandId == viewModel.Filter.BrandId.Value));
                    }
                    
                    viewModel.TongSoDonHang = await filteredQuery.CountAsync();
                    viewModel.SoKhachHangMoi = await _context.Customers
                        .Where(c => c.CreatedDate >= startDate && c.CreatedDate <= endDate)
                        .CountAsync();
                    
                    viewModel.TyLeThanhCong = 100; // Simplified
                    viewModel.GiaTriDonHangTrungBinh = viewModel.TongSoDonHang > 0 ? 
                        viewModel.TongDoanhThuLoc / viewModel.TongSoDonHang : 0;
                }
                else
                {
                    // Load default summary data (no filter)
                    // Tổng doanh thu hôm nay
                    viewModel.TongDoanhThuHomNay = await _context.Orders
                        .Where(o => o.OrderDate.HasValue && o.OrderDate.Value.Date == today)
                        .SumAsync(o => o.TotalAmount ?? 0);

                    // Tổng doanh thu tháng này
                    viewModel.TongDoanhThuThangNay = await _context.Orders
                        .Where(o => o.OrderDate >= startOfMonth)
                        .SumAsync(o => o.TotalAmount ?? 0m);

                    // Tổng doanh thu năm này
                    viewModel.TongDoanhThuNamNay = await _context.Orders
                        .Where(o => o.OrderDate >= startOfYear)
                        .SumAsync(o => o.TotalAmount ?? 0m);

                    // Doanh thu tháng trước để tính tăng trưởng
                    var doanhThuThangTruoc = await _context.Orders
                        .Where(o => o.OrderDate >= lastMonth && o.OrderDate <= endOfLastMonth)
                        .SumAsync(o => o.TotalAmount ?? 0m);

                    // Tính tăng trưởng
                    viewModel.TangTruongThang = doanhThuThangTruoc > 0 ? 
                        ((viewModel.TongDoanhThuThangNay - doanhThuThangTruoc) / doanhThuThangTruoc) * 100 : 0;

                    // Tổng số đơn hàng tháng này
                    viewModel.TongSoDonHang = await _context.Orders
                        .Where(o => o.OrderDate >= startOfMonth)
                        .CountAsync();

                    // Số khách hàng mới tháng này
                    viewModel.SoKhachHangMoi = await _context.Customers.CountAsync();

                    // Tính toán các metrics bổ sung
                    var totalOrders = await _context.Orders
                        .Where(o => o.OrderDate >= startOfMonth)
                        .CountAsync();

                    viewModel.TyLeThanhCong = totalOrders > 0 ? 100 : 0;
                    viewModel.GiaTriDonHangTrungBinh = totalOrders > 0 ? 
                        viewModel.TongDoanhThuThangNay / totalOrders : 0;
                }

            }
            catch (Exception ex)
            {
                // Log error và return empty viewmodel
                Console.WriteLine($"Dashboard error: {ex.Message}");
                
                // Fallback to basic queries if views don't exist
                await LoadFallbackData(viewModel, today, startOfMonth, startOfYear);
            }

            return View(viewModel);
        }

        private async Task LoadFallbackData(DashboardViewModel viewModel, DateTime today, DateTime startOfMonth, DateTime startOfYear)
        {
            try
            {
                // Simple date revenue data (30 ngày gần nhất)
                viewModel.RevenueByDates = await _context.Orders
                    .Where(o => o.OrderDate.HasValue && o.OrderDate >= today.AddDays(-30))
                    .GroupBy(o => o.OrderDate.Value.Date)
                    .Select(g => new RevenueByDate
                    {
                        Ngay = g.Key,
                        DoanhThu = g.Sum(o => o.TotalAmount ?? 0m)
                    })
                    .OrderBy(r => r.Ngay)
                    .ToListAsync();

                // Fill missing dates for chart
                FillMissingDates(viewModel.RevenueByDates, today.AddDays(-30), today);

                // Top products tháng này
                viewModel.TopProducts = await _context.OrderDetails
                    .Include(od => od.Product)
                    .Include(od => od.Order)
                    .Where(od => od.Order.OrderDate >= startOfMonth)
                    .GroupBy(od => new { od.ProductId, od.Product.ProductName })
                    .Select(g => new RevenueByProduct
                    {
                        ProductName = g.Key.ProductName,
                        SoLuongBan = (g.Sum(od => od.Quantity) ?? 0),
                        DoanhThu = g.Sum(od => od.TotalPrice)
                    })
                    .OrderByDescending(p => p.SoLuongBan)
                    .Take(5)
                    .ToListAsync();

                // Brand revenue data tháng này
                viewModel.RevenueByBrands = await _context.OrderDetails
                    .Include(od => od.Product)
                    .ThenInclude(p => p.Brand)
                    .Include(od => od.Order)
                    .Where(od => od.Order.OrderDate >= startOfMonth)
                    .GroupBy(od => od.Product.Brand.BrandName)
                    .Select(g => new RevenueByBrand
                    {
                        BrandName = g.Key,
                        DoanhThu = g.Sum(od => od.TotalPrice)
                    })
                    .OrderByDescending(b => b.DoanhThu)
                    .Take(10)
                    .ToListAsync();

                // Set filter description for default view
                viewModel.FilterDescription = "";
                viewModel.TongDoanhThuLoc = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fallback data error: {ex.Message}");
            }
        }

        private async Task SetupFilterOptions(DashboardFilterViewModel filter)
        {
            // Brand options
            var brands = await _context.Brands.OrderBy(b => b.BrandName).ToListAsync();
            filter.BrandOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "-- Tất cả thương hiệu --" }
            };
            filter.BrandOptions.AddRange(brands.Select(b => new SelectListItem 
            { 
                Value = b.BrandId.ToString(), 
                Text = b.BrandName 
            }));

            // Year options (last 5 years)
            var currentYear = DateTime.Now.Year;
            filter.YearOptions = Enumerable.Range(currentYear - 4, 5)
                .Select(y => new SelectListItem 
                { 
                    Value = y.ToString(), 
                    Text = y.ToString(),
                    Selected = y == filter.Year
                })
                .OrderByDescending(x => x.Value)
                .ToList();

            // Month options
            filter.MonthOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "-- Tất cả tháng --" }
            };
            for (int i = 1; i <= 12; i++)
            {
                filter.MonthOptions.Add(new SelectListItem 
                { 
                    Value = i.ToString(), 
                    Text = $"Tháng {i}",
                    Selected = i == filter.Month
                });
            }
        }

        private async Task LoadFilteredData(DashboardViewModel viewModel, DashboardFilterViewModel filter)
        {
            try
            {
                // Get date range
                DateTime startDate, endDate;
                
                // Always use GetDateRange for consistency
                GetDateRange(filter, out startDate, out endDate);

                Console.WriteLine($"LoadFilteredData - StartDate: {startDate}, EndDate: {endDate}, TimeType: {filter.TimeType}");

                // Base queries
                var ordersQuery = _context.Orders.Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate);
                var orderDetailsQuery = _context.OrderDetails
                    .Include(od => od.Product)
                    .ThenInclude(p => p.Brand)
                    .Include(od => od.Order)
                    .Where(od => od.Order.OrderDate >= startDate && od.Order.OrderDate <= endDate);

                // Apply brand filter if specified
                if (filter.BrandId.HasValue)
                {
                    orderDetailsQuery = orderDetailsQuery.Where(od => od.Product.BrandId == filter.BrandId.Value);
                    viewModel.TongDoanhThuLoc = await orderDetailsQuery.SumAsync(od => od.TotalPrice);
                }
                else
                {
                    viewModel.TongDoanhThuLoc = await ordersQuery.SumAsync(o => o.TotalAmount ?? 0m);
                }

                // Set filter description
                viewModel.FilterDescription = GetFilterDescription(filter, startDate, endDate);

                // Revenue by dates based on filter type
                switch (filter.TimeType)
                {
                    case "day":
                        if (filter.BrandId.HasValue)
                        {
                            viewModel.RevenueByDates = await orderDetailsQuery
                                .Where(od => od.Order.OrderDate.HasValue)
                                .GroupBy(od => od.Order.OrderDate.Value.Date)
                                .Select(g => new RevenueByDate { Ngay = g.Key, DoanhThu = g.Sum(od => od.TotalPrice) })
                                .OrderBy(r => r.Ngay)
                                .ToListAsync();
                        }
                        else
                        {
                            viewModel.RevenueByDates = await ordersQuery
                                .Where(o => o.OrderDate.HasValue)
                                .GroupBy(o => o.OrderDate.Value.Date)
                                .Select(g => new RevenueByDate { Ngay = g.Key, DoanhThu = g.Sum(o => o.TotalAmount ?? 0m) })
                                .OrderBy(r => r.Ngay)
                                .ToListAsync();
                        }
                        FillMissingDates(viewModel.RevenueByDates, startDate, endDate);
                        break;

                    case "month":
                        if (filter.Month.HasValue && filter.Month.Value > 0)
                        {
                            // Single month - show daily data
                            goto case "day";
                        }
                        else
                        {
                            // All months in year - show monthly data
                            if (filter.BrandId.HasValue)
                            {
                                viewModel.RevenueByDates = await orderDetailsQuery
                                    .Where(od => od.Order.OrderDate.HasValue)
                                    .GroupBy(od => new { od.Order.OrderDate.Value.Year, od.Order.OrderDate.Value.Month })
                                .Select(g => new RevenueByDate 
                                { 
                                    Ngay = new DateTime(g.Key.Year, g.Key.Month, 1), 
                                    DoanhThu = g.Sum(od => od.TotalPrice) 
                                })
                                    .OrderBy(r => r.Ngay)
                                    .ToListAsync();
                            }
                            else
                            {
                                viewModel.RevenueByDates = await ordersQuery
                                    .Where(o => o.OrderDate.HasValue)
                                    .GroupBy(o => new { o.OrderDate.Value.Year, o.OrderDate.Value.Month })
                                    .Select(g => new RevenueByDate 
                                    { 
                                        Ngay = new DateTime(g.Key.Year, g.Key.Month, 1), 
                                        DoanhThu = g.Sum(o => o.TotalAmount ?? 0m) 
                                    })
                                    .OrderBy(r => r.Ngay)
                                    .ToListAsync();
                            }
                            
                            // Fill missing months for the year
                            FillMissingMonths(viewModel.RevenueByDates, filter.Year);
                        }
                        break;

                    case "year":
                        if (filter.BrandId.HasValue)
                        {
                            viewModel.RevenueByDates = await orderDetailsQuery
                                .Where(od => od.Order.OrderDate.HasValue)
                                .GroupBy(od => od.Order.OrderDate.Value.Year)
                                .Select(g => new RevenueByDate 
                                { 
                                    Ngay = new DateTime(g.Key, 1, 1), 
                                    DoanhThu = g.Sum(od => od.TotalPrice) 
                                })
                                .OrderBy(r => r.Ngay)
                                .ToListAsync();
                        }
                        else
                        {
                            viewModel.RevenueByDates = await ordersQuery
                                .Where(o => o.OrderDate.HasValue)
                                .GroupBy(o => o.OrderDate.Value.Year)
                                .Select(g => new RevenueByDate 
                                { 
                                    Ngay = new DateTime(g.Key, 1, 1), 
                                    DoanhThu = g.Sum(o => o.TotalAmount ?? 0m) 
                                })
                                .OrderBy(r => r.Ngay)
                                .ToListAsync();
                        }
                        break;
                }

                // Top products
                viewModel.TopProducts = await orderDetailsQuery
                    .GroupBy(od => new { od.ProductId, od.Product.ProductName })
                    .Select(g => new RevenueByProduct
                    {
                        ProductName = g.Key.ProductName,
                        SoLuongBan = (g.Sum(od => od.Quantity) ?? 0),
                        DoanhThu = g.Sum(od => od.TotalPrice)
                    })
                    .OrderByDescending(p => p.SoLuongBan)
                    .Take(5)
                    .ToListAsync();

                // Revenue by brands
                if (filter.BrandId.HasValue)
                {
                    var brand = await _context.Brands.FindAsync(filter.BrandId.Value);
                    if (brand != null)
                    {
                        viewModel.RevenueByBrands = new List<RevenueByBrand>
                        {
                            new RevenueByBrand { BrandName = brand.BrandName, DoanhThu = viewModel.TongDoanhThuLoc }
                        };
                    }
                }
                else
                {
                    viewModel.RevenueByBrands = await orderDetailsQuery
                        .GroupBy(od => od.Product.Brand.BrandName)
                        .Select(g => new RevenueByBrand { BrandName = g.Key, DoanhThu = g.Sum(od => od.TotalPrice) })
                        .OrderByDescending(b => b.DoanhThu)
                        .Take(10)
                        .ToListAsync();
                }

                Console.WriteLine($"LoadFilteredData completed - Revenue: {viewModel.TongDoanhThuLoc}, Products: {viewModel.TopProducts.Count}, Brands: {viewModel.RevenueByBrands.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LoadFilteredData error: {ex.Message}");
                viewModel.RevenueByDates = new List<RevenueByDate>();
                viewModel.TopProducts = new List<RevenueByProduct>();
                viewModel.RevenueByBrands = new List<RevenueByBrand>();
            }
        }

        private void GetDateRange(DashboardFilterViewModel filter, out DateTime startDate, out DateTime endDate)
        {
            var today = DateTime.Today;

            // Set default values if not provided
            if (string.IsNullOrEmpty(filter.TimeType))
            {
                filter.TimeType = "day";
            }
            
            if (filter.Year == 0)
            {
                filter.Year = today.Year;
            }

            switch (filter.TimeType)
            {
                case "day":
                    if (filter.FromDate.HasValue && filter.ToDate.HasValue)
                    {
                        startDate = filter.FromDate.Value.Date;
                        endDate = filter.ToDate.Value.Date.AddDays(1).AddSeconds(-1); // End of day
                    }
                    else
                    {
                        startDate = today.AddDays(-30);
                        endDate = today.AddDays(1).AddSeconds(-1);
                    }
                    break;

                case "month":
                    if (filter.Month.HasValue && filter.Month.Value > 0)
                    {
                        startDate = new DateTime(filter.Year, filter.Month.Value, 1);
                        endDate = startDate.AddMonths(1).AddSeconds(-1); // End of month
                    }
                    else
                    {
                        // All months in the year
                        startDate = new DateTime(filter.Year, 1, 1);
                        endDate = new DateTime(filter.Year, 12, 31, 23, 59, 59);
                    }
                    break;

                case "year":
                    startDate = new DateTime(filter.Year, 1, 1);
                    endDate = new DateTime(filter.Year, 12, 31, 23, 59, 59);
                    break;

                default:
                    // Default to last 30 days
                    startDate = today.AddDays(-30);
                    endDate = today.AddDays(1).AddSeconds(-1);
                    filter.TimeType = "day";
                    break;
            }

            // Debug logging
            Console.WriteLine($"GetDateRange - TimeType: {filter.TimeType}, Year: {filter.Year}, Month: {filter.Month}, StartDate: {startDate}, EndDate: {endDate}");
        }

        private string GetFilterDescription(DashboardFilterViewModel filter, DateTime startDate, DateTime endDate)
        {
            var description = "";

            switch (filter.TimeType)
            {
                case "day":
                    description = $"Từ {startDate:dd/MM/yyyy} đến {endDate:dd/MM/yyyy}";
                    break;
                case "month":
                    if (filter.Month.HasValue)
                        description = $"Tháng {filter.Month}/{filter.Year}";
                    else
                        description = $"Năm {filter.Year}";
                    break;
                case "year":
                    description = $"Năm {filter.Year}";
                    break;
            }

            if (filter.BrandId.HasValue)
            {
                var brand = _context.Brands.Find(filter.BrandId.Value);
                if (brand != null)
                    description += $" - Thương hiệu: {brand.BrandName}";
            }

            return description;
        }

        private void FillMissingDates(List<RevenueByDate> revenueData, DateTime startDate, DateTime endDate)
        {
            var existingDates = revenueData.Select(r => r.Ngay.Date).ToHashSet();
            
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                if (!existingDates.Contains(date))
                {
                    revenueData.Add(new RevenueByDate { Ngay = date, DoanhThu = 0 });
                }
            }
            
            revenueData.Sort((a, b) => a.Ngay.CompareTo(b.Ngay));
        }

        private void FillMissingMonths(List<RevenueByDate> revenueData, int year)
        {
            var existingMonths = revenueData.Select(r => r.Ngay.Month).ToHashSet();
            
            for (int month = 1; month <= 12; month++)
            {
                if (!existingMonths.Contains(month))
                {
                    revenueData.Add(new RevenueByDate { Ngay = new DateTime(year, month, 1), DoanhThu = 0 });
                }
            }
            
            revenueData.Sort((a, b) => a.Ngay.CompareTo(b.Ngay));
        }



        [HttpGet]
        [RequirePermission("Export Dashboard")]
        public async Task<IActionResult> ExportExcel(string timeType = "month", int year = 0, int? month = null, int? brandId = null)
        {
            try
            {
                var filter = new DashboardFilterViewModel
                {
                    TimeType = timeType,
                    Year = year == 0 ? DateTime.Now.Year : year,
                    Month = month,
                    BrandId = brandId
                };

                var viewModel = new DashboardViewModel { Filter = filter };
                await LoadFilteredData(viewModel, filter);

                // Create HTML that Excel can open
                var html = new System.Text.StringBuilder();
                
                html.AppendLine("<!DOCTYPE html>");
                html.AppendLine("<html>");
                html.AppendLine("<head>");
                html.AppendLine("<meta charset='utf-8'>");
                html.AppendLine("<style>");
                html.AppendLine("table { border-collapse: collapse; width: 100%; margin-bottom: 20px; }");
                html.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
                html.AppendLine("th { background-color: #f2f2f2; font-weight: bold; }");
                html.AppendLine(".number { text-align: right; }");
                html.AppendLine("h1 { color: #333; }");
                html.AppendLine("h2 { color: #666; margin-top: 30px; }");
                html.AppendLine("</style>");
                html.AppendLine("</head>");
                html.AppendLine("<body>");
                
                // Header
                html.AppendLine("<h1>BÁO CÁO DOANH THU - PERFUME STORE</h1>");
                html.AppendLine($"<p><strong>Thời gian xuất:</strong> {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>");
                html.AppendLine($"<p><strong>Bộ lọc:</strong> {viewModel.FilterDescription ?? "Tất cả"}</p>");

                // Summary
                html.AppendLine("<h2>TỔNG QUAN</h2>");
                html.AppendLine("<table>");
                html.AppendLine("<tr><th>Chỉ số</th><th>Giá trị</th></tr>");
                html.AppendLine($"<tr><td>Tổng doanh thu</td><td class='number'>{viewModel.TongDoanhThuLoc:N0} VNĐ</td></tr>");
                html.AppendLine("</table>");

                // Revenue by dates
                html.AppendLine("<h2>DOANH THU THEO THỜI GIAN</h2>");
                html.AppendLine("<table>");
                html.AppendLine("<tr><th>Ngày</th><th>Doanh thu (VNĐ)</th></tr>");
                if (viewModel.RevenueByDates != null && viewModel.RevenueByDates.Any())
                {
                    foreach (var item in viewModel.RevenueByDates.OrderBy(r => r.Ngay))
                    {
                        html.AppendLine($"<tr><td>{item.Ngay:dd/MM/yyyy}</td><td class='number'>{item.DoanhThu:N0}</td></tr>");
                    }
                }
                html.AppendLine("</table>");

                // Top products
                html.AppendLine("<h2>TOP SẢN PHẨM BÁN CHẠY</h2>");
                html.AppendLine("<table>");
                html.AppendLine("<tr><th>STT</th><th>Tên sản phẩm</th><th>Doanh thu (VNĐ)</th></tr>");
                if (viewModel.TopProducts != null && viewModel.TopProducts.Any())
                {
                    for (int i = 0; i < viewModel.TopProducts.Count; i++)
                    {
                        var product = viewModel.TopProducts[i];
                        html.AppendLine($"<tr><td>{i + 1}</td><td>{product.ProductName}</td><td class='number'>{product.DoanhThu:N0}</td></tr>");
                    }
                }
                html.AppendLine("</table>");
                
                html.AppendLine("</body>");
                html.AppendLine("</html>");

                var fileName = $"BaoCaoDoanhThu_{DateTime.Now:yyyyMMdd_HHmmss}.xls";
                var bytes = System.Text.Encoding.UTF8.GetBytes(html.ToString());
                
                return File(bytes, "application/vnd.ms-excel", fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Export Excel error: {ex.Message}");
                return BadRequest($"Lỗi khi xuất Excel: {ex.Message}");
            }
        }





        private string GenerateExcelReport(DashboardViewModel viewModel, DateTime startDate, DateTime endDate)
        {
            var html = new System.Text.StringBuilder();
            
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset='utf-8'>");
            html.AppendLine("<style>");
            html.AppendLine("table { border-collapse: collapse; width: 100%; }");
            html.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
            html.AppendLine("th { background-color: #f2f2f2; font-weight: bold; }");
            html.AppendLine(".number { text-align: right; }");
            html.AppendLine(".header { font-size: 18px; font-weight: bold; margin: 20px 0; }");
            html.AppendLine(".section { margin: 20px 0; }");
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            // Header
            html.AppendLine("<div class='header'>BÁO CÁO DASHBOARD - PERFUME STORE</div>");
            html.AppendLine($"<p><strong>Thời gian xuất:</strong> {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>");
            html.AppendLine($"<p><strong>Khoảng thời gian:</strong> {startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}</p>");
            html.AppendLine($"<p><strong>Bộ lọc:</strong> {viewModel.FilterDescription}</p>");

            // Summary
            html.AppendLine("<div class='section'>");
            html.AppendLine("<h3>TỔNG QUAN</h3>");
            html.AppendLine("<table>");
            html.AppendLine("<tr><th>Chỉ số</th><th>Giá trị</th></tr>");
            html.AppendLine($"<tr><td>Tổng doanh thu</td><td class='number'>{viewModel.TongDoanhThuLoc:N0} VNĐ</td></tr>");
            html.AppendLine($"<tr><td>Số đơn hàng</td><td class='number'>{viewModel.TongSoDonHang}</td></tr>");
            html.AppendLine($"<tr><td>Khách hàng mới</td><td class='number'>{viewModel.SoKhachHangMoi}</td></tr>");
            html.AppendLine($"<tr><td>Giá trị đơn hàng TB</td><td class='number'>{viewModel.GiaTriDonHangTrungBinh:N0} VNĐ</td></tr>");
            html.AppendLine("</table>");
            html.AppendLine("</div>");

            // Revenue by dates
            html.AppendLine("<div class='section'>");
            html.AppendLine("<h3>DOANH THU THEO THỜI GIAN</h3>");
            html.AppendLine("<table>");
            html.AppendLine("<tr><th>Thời gian</th><th>Doanh thu (VNĐ)</th></tr>");
            foreach (var item in viewModel.RevenueByDates.OrderBy(r => r.Ngay))
            {
                var dateFormat = viewModel.Filter.TimeType == "month" && !viewModel.Filter.Month.HasValue ? "MM/yyyy" :
                               viewModel.Filter.TimeType == "year" ? "yyyy" : "dd/MM/yyyy";
                html.AppendLine($"<tr><td>{item.Ngay.ToString(dateFormat)}</td><td class='number'>{item.DoanhThu:N0}</td></tr>");
            }
            html.AppendLine("</table>");
            html.AppendLine("</div>");

            // Top products
            html.AppendLine("<div class='section'>");
            html.AppendLine("<h3>TOP SẢN PHẨM BÁN CHẠY</h3>");
            html.AppendLine("<table>");
            html.AppendLine("<tr><th>STT</th><th>Tên sản phẩm</th><th>Số lượng bán</th><th>Doanh thu (VNĐ)</th></tr>");
            for (int i = 0; i < viewModel.TopProducts.Count; i++)
            {
                var product = viewModel.TopProducts[i];
                html.AppendLine($"<tr><td>{i + 1}</td><td>{product.ProductName}</td><td class='number'>{product.SoLuongBan}</td><td class='number'>{product.DoanhThu:N0}</td></tr>");
            }
            html.AppendLine("</table>");
            html.AppendLine("</div>");

            // Revenue by brands
            html.AppendLine("<div class='section'>");
            html.AppendLine("<h3>DOANH THU THEO THƯƠNG HIỆU</h3>");
            html.AppendLine("<table>");
            html.AppendLine("<tr><th>STT</th><th>Thương hiệu</th><th>Doanh thu (VNĐ)</th></tr>");
            for (int i = 0; i < viewModel.RevenueByBrands.Count; i++)
            {
                var brand = viewModel.RevenueByBrands[i];
                html.AppendLine($"<tr><td>{i + 1}</td><td>{brand.BrandName}</td><td class='number'>{brand.DoanhThu:N0}</td></tr>");
            }
            html.AppendLine("</table>");
            html.AppendLine("</div>");

            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }
    }
}