using System;
using System.Collections.Generic;

namespace PerfumeStore.Models
{
    public partial class Fee
    {
        public int FeeId { get; set; }
        public string Name { get; set; } = null!;
        public decimal Value { get; set; }
        public string? Description { get; set; }
        public decimal? Threshold { get; set; }
    }
}
