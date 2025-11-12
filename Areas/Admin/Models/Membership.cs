using System;
using System.Collections.Generic;

namespace PerfumeStore.Areas.Admin.Models
{
    public partial class Membership
    {
        public Membership()
        {
            Customers = new HashSet<Customer>();
        }

        public int MembershipId { get; set; }
        public string Name { get; set; } = null!;
        public DateTime CreatedDate { get; set; }
        public string? Description { get; set; }
        public int? DiscountPercentage { get; set; }
        public bool? IsActive { get; set; }
        public decimal? MinimumSpend { get; set; }
        public DateTime? UpdatedDate { get; set; }

        public virtual ICollection<Customer> Customers { get; set; }
    }
}
