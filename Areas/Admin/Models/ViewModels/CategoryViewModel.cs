using System.ComponentModel.DataAnnotations;

namespace PerfumeStore.Areas.Admin.Models.ViewModels
{
    // ViewModel cho danh sách danh mục với phân trang
    public class CategoryListViewModel
    {
        public IEnumerable<Category> Categories { get; set; } = new List<Category>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; } = "name";
        public string? SortOrder { get; set; } = "asc";
        public int TotalCategories { get; set; }
        
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    // ViewModel cho tạo danh mục mới
    public class CategoryCreateViewModel
    {
        [Required(ErrorMessage = "Tên danh mục là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên danh mục không được vượt quá 100 ký tự")]
        [Display(Name = "Tên danh mục")]
        public string CategoryName { get; set; } = string.Empty;
    }

    // ViewModel cho chỉnh sửa danh mục
    public class CategoryEditViewModel
    {
        public int CategoryId { get; set; }
        
        [Required(ErrorMessage = "Tên danh mục là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên danh mục không được vượt quá 100 ký tự")]
        [Display(Name = "Tên danh mục")]
        public string CategoryName { get; set; } = string.Empty;
        
        public int ProductCount { get; set; }
    }

    // ViewModel cho chi tiết danh mục với sản phẩm
    public class CategoryDetailsViewModel
    {
        public Category Category { get; set; } = new Category();
        public IEnumerable<Product> Products { get; set; } = new List<Product>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 12;
        public int TotalProducts { get; set; }
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; } = "name";
        public string? SortOrder { get; set; } = "asc";
        
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    // ViewModel cho thống kê danh mục
    public class CategoryStatsViewModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
    }
}