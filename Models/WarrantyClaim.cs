using System;
using System.Collections.Generic;

namespace PerfumeStore.Models
{
    public partial class WarrantyClaim
    {
        public int WarrantyClaimId { get; set; }
        public int WarrantyId { get; set; }
        public string? Resolution { get; set; }
        public string? ResolutionType { get; set; }
        public string ClaimCode { get; set; } = null!;
        public string IssueDescription { get; set; } = null!;
        public string IssueType { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? AdminNotes { get; set; }
        public DateTime SubmittedDate { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string? ProcessedByAdmin { get; set; }

        public virtual Warranty Warranty { get; set; } = null!;
    }
}
