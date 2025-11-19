using System;
using System.Collections.Generic;

namespace PerfumeStore.Models.ViewModels
{
    public class CustomerVoucherVM
    {
        public IList<CouponItem> PersonalCoupons { get; set; } = new List<CouponItem>();

        public class CouponItem
        {
            public int CouponId { get; set; }
            public string Code { get; set; } = string.Empty;
            public decimal? DiscountAmount { get; set; }
            public DateTime? ExpiryDate { get; set; }
            public DateTime? CreatedDate { get; set; }
            public bool IsUsed { get; set; }
            public bool IsExpired { get; set; }
            public DateTime? UsedDate { get; set; }
            public bool IsAssignedToCustomer { get; set; }
        }
    }
}

