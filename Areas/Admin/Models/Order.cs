using System;
using System.Collections.Generic;

namespace PerfumeStore.Areas.Admin.Models
{
    public partial class Order
    {
        public Order()
        {
            OrderDetails = new HashSet<OrderDetail>();
        }

        public int OrderId { get; set; }
        public DateTime? OrderDate { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? Notes { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Status { get; set; }
        public int CustomerId { get; set; }
        public int? CouponId { get; set; }
        public int AddressId { get; set; }

        public virtual ShippingAddress Address { get; set; } = null!;
        public virtual Coupon? Coupon { get; set; }
        public virtual Customer Customer { get; set; } = null!;
        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
    }
}
