using System;
using System.Collections.Generic;

namespace PerfumeStore.Areas.Admin.Models
{
    public partial class Liter
    {
        public Liter()
        {
            Products = new HashSet<Product>();
        }

        public int LiterId { get; set; }
        public int LiterNumber { get; set; }
        public string? LiterDescription { get; set; }
        public decimal LiterPrice { get; set; }

        public virtual ICollection<Product> Products { get; set; }
    }
}
