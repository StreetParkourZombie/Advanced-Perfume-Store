using System;
using System.Collections.Generic;

namespace PerfumeStore.Areas.Admin.Models
{
    public partial class VwRevenueByBrand
    {
        public DateTime? Ngay { get; set; }
        public string BrandName { get; set; } = null!;
        public decimal? DoanhThu { get; set; }
    }
}
