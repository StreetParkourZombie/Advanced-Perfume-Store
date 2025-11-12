using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System.Collections.Generic;

namespace PerfumeStore.Areas.Admin.Models
{
    public class AdminProductFormVM
    {
        public int? ProductId { get; set; }

        [Required]
        [StringLength(100)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string SuggestionName { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue)]
        public int Stock { get; set; }

        [Required]
        [StringLength(100)]
        public string Scent { get; set; } = string.Empty;

        [Required]
        public int BrandId { get; set; }

        [Range(1, 120)]
        public int WarrantyPeriodMonths { get; set; } = 12;

        public bool IsPublished { get; set; } = true;

        [MinLength(1, ErrorMessage = "Chọn ít nhất 1 danh mục")]
        public List<int> SelectedCategoryIds { get; set; } = new List<int>();

        [StringLength(30)]
        public string? Origin { get; set; }
        public int? ReleaseYear { get; set; }
        [StringLength(150)]
        public string? Introduction { get; set; }

        // Detail fields
        [StringLength(100)]
        public string? Concentration { get; set; }
        [StringLength(100)]
        public string? Craftsman { get; set; }
        [StringLength(250)]
        public string? Style { get; set; }
        [StringLength(250)]
        public string? UsingOccasion { get; set; }
        [StringLength(100)]
        public string? TopNote { get; set; }
        [StringLength(100)]
        public string? HeartNote { get; set; }
        [StringLength(100)]
        public string? BaseNote { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int? DiscountId { get; set; }
        [StringLength(500)]
        public string? DescriptionNo1 { get; set; }
        public string? DescriptionNo2 { get; set; }

        // Image upload properties
        public IFormFileCollection? ImageFiles { get; set; }
        public List<ProductImage> ExistingImages { get; set; } = new List<ProductImage>();
        public List<int> DeletedImageIds { get; set; } = new List<int>();

        public IEnumerable<SelectListItem> BrandOptions { get; set; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> CategoryOptions { get; set; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<SelectListItem> DiscountOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    }
}


