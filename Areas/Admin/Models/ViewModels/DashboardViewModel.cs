using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PerfumeStore.Areas.Admin.Models
{
    public class RevenueByDate
    {
        public DateTime Ngay { get; set; }
        public decimal DoanhThu { get; set; }
    }

    public class RevenueByProduct
    {
        public string ProductName { get; set; } = string.Empty;
        public decimal DoanhThu { get; set; }
        public int SoLuongBan { get; set; }
    }

    public class RevenueByBrand
    {
        public string BrandName { get; set; } = string.Empty;
        public decimal DoanhThu { get; set; }
    }

    public class DashboardFilterViewModel
    {
        [Display(Name = "Loại thời gian")]
        public string TimeType { get; set; } = "month"; // day, month, year

        [Display(Name = "Từ ngày")]
        [DataType(DataType.Date)]
        public DateTime? FromDate { get; set; }

        [Display(Name = "Đến ngày")]
        [DataType(DataType.Date)]
        public DateTime? ToDate { get; set; }

        [Display(Name = "Tháng")]
        public int? Month { get; set; }

        [Display(Name = "Năm")]
        public int Year { get; set; } = DateTime.Now.Year;

        [Display(Name = "Thương hiệu")]
        public int? BrandId { get; set; }

        // Dropdown options
        public List<SelectListItem> BrandOptions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> YearOptions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> MonthOptions { get; set; } = new List<SelectListItem>();
    }

    public class DashboardViewModel
    {
        [Display(Name = "Tổng doanh thu hôm nay")]
        [DisplayFormat(DataFormatString = "{0:N0} VNĐ")]
        public decimal TongDoanhThuHomNay { get; set; }

        [Display(Name = "Tổng doanh thu tháng này")]
        [DisplayFormat(DataFormatString = "{0:N0} VNĐ")]
        public decimal TongDoanhThuThangNay { get; set; }

        [Display(Name = "Tổng doanh thu năm này")]
        [DisplayFormat(DataFormatString = "{0:N0} VNĐ")]
        public decimal TongDoanhThuNamNay { get; set; }

        [Display(Name = "Tổng số đơn hàng")]
        public int TongSoDonHang { get; set; }

        [Display(Name = "Số khách hàng mới")]
        public int SoKhachHangMoi { get; set; }

        [Display(Name = "Doanh thu theo ngày")]
        public List<RevenueByDate> RevenueByDates { get; set; } = new List<RevenueByDate>();

        [Display(Name = "Top sản phẩm bán chạy")]
        public List<RevenueByProduct> TopProducts { get; set; } = new List<RevenueByProduct>();

        [Display(Name = "Doanh thu theo thương hiệu")]
        public List<RevenueByBrand> RevenueByBrands { get; set; } = new List<RevenueByBrand>();

        // Additional metrics
        public decimal TyLeThanhCong { get; set; }
        
        [Display(Name = "Giá trị đơn hàng trung bình")]
        [DisplayFormat(DataFormatString = "{0:N0} VNĐ")]
        public decimal GiaTriDonHangTrungBinh { get; set; } // Average Order Value (AOV)
        
        public decimal TangTruongThang { get; set; }

        // Filter information
        public DashboardFilterViewModel Filter { get; set; } = new DashboardFilterViewModel();
        public string FilterDescription { get; set; } = string.Empty;
        public decimal TongDoanhThuLoc { get; set; } // Tổng doanh thu theo filter
    }
}