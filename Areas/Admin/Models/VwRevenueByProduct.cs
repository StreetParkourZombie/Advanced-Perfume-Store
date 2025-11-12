using System;
using System.Collections.Generic;

namespace PerfumeStore.Areas.Admin.Models
{
    public partial class VwRevenueByProduct
    {
        public DateTime? Ngay { get; set; }
        public string ProductName { get; set; } = null!;
        public int? SoLuongBan { get; set; }
        public decimal? DoanhThu { get; set; }
    }
}
