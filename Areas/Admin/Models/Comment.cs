using System;
using System.Collections.Generic;

namespace PerfumeStore.Areas.Admin.Models
{
    public partial class Comment
    {
        public int ProductId { get; set; }
        public int CustomerId { get; set; }
        public DateTime CommentDate { get; set; }
        public int Rating { get; set; }
        public string? Content { get; set; }
        public bool? IsPublished { get; set; }

        public virtual Customer Customer { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }
}
