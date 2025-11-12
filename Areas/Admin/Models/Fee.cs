using System;
using System.Collections.Generic;

namespace PerfumeStore.Areas.Admin.Models
{
    public partial class Fee
    {
        public int FeeId { get; set; }
        public string Name { get; set; } = null!;
        public decimal Value { get; set; }
        public string? Description { get; set; }
        public decimal? Threshold { get; set; } // Ngưỡng áp dụng (cho Shipping fee: chỉ áp dụng khi đơn hàng < Threshold)
    }
}
