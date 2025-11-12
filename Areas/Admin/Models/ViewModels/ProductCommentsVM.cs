namespace PerfumeStore.Areas.Admin.Models.ViewModels
{
    public class ProductCommentsVM
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? BrandName { get; set; }
        public string? CategoryNames { get; set; }
        public int TotalComments { get; set; }
        public int PublishedComments { get; set; }
        public int PendingComments { get; set; }
        public double AverageRating { get; set; }
        public int? FirstImageId { get; set; }
    }

    public class CommentDetailsVM
    {
        public int ProductId { get; set; }
        public int CustomerId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public DateTime CommentDate { get; set; }
        public int Rating { get; set; }
        public string? Content { get; set; }
        public bool? IsPublished { get; set; }
    }
}
