using System;
using System.Collections.Generic;

namespace PerfumeStore.Models
{
    public partial class Brand
    {
        public Brand()
        {
            Products = new HashSet<Product>();
        }

        public int BrandId { get; set; }
        public string BrandName { get; set; } = null!;
        public byte[]? ImageData { get; set; }
        public string ImageMimeType { get; set; } = null!;

        public virtual ICollection<Product> Products { get; set; }
    }
}
