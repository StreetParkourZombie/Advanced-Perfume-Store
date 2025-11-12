using System;
using System.Collections.Generic;

namespace PerfumeStore.Models
{
    public partial class ProductImage
    {
        public int ImageId { get; set; }
        public byte[]? ImageData { get; set; }
        public string ImageMimeType { get; set; } = null!;
        public int ProductId { get; set; }

        public virtual Product Product { get; set; } = null!;
    }
}
