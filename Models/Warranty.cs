using System;
using System.Collections.Generic;

namespace PerfumeStore.Models
{
    public partial class Warranty
    {
        public Warranty()
        {
            WarrantyClaims = new HashSet<WarrantyClaim>();
        }

        public int WarrantyId { get; set; }
        public int OrderDetailId { get; set; }
        public int CustomerId { get; set; }
        public string WarrantyCode { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int WarrantyPeriodMonths { get; set; }
        public string Status { get; set; } = null!;
        public string? Notes { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }

        public virtual ICollection<WarrantyClaim> WarrantyClaims { get; set; }
    }
}
