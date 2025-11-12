using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PerfumeStore.Areas.Admin.Models
{
    public partial class Coupon
    {
        public Coupon()
        {
            Orders = new HashSet<Order>();
        }

        public int CouponId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã coupon.")]
        [StringLength(30, ErrorMessage = "Mã coupon tối đa 30 ký tự.")]
        public string? Code { get; set; }

        public bool? IsUsed { get; set; }
        public DateTime? CreatedDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày hết hạn.")]
        public DateTime? ExpiryDate { get; set; }

        public DateTime? UsedDate { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số tiền giảm.")]
        [Range(typeof(decimal), "0", "9999999.99", ErrorMessage = "Số tiền giảm tối đa 9,999,999.99.")]
        public decimal? DiscountAmount { get; set; }

        public virtual ICollection<Order> Orders { get; set; }
    }
}
