using Microsoft.AspNetCore.Mvc;
using PerfumeStore.Models;
using PerfumeStore.Services;

namespace PerfumeStore.ViewComponents
{
    public class CommentViewComponent : ViewComponent
    {
        private readonly IDbQueryService _dbQueryService;
        private readonly ILogger<CommentViewComponent> _logger;

        public CommentViewComponent(IDbQueryService dbQueryService, ILogger<CommentViewComponent> logger)
        {
            _dbQueryService = dbQueryService;
            _logger = logger;
        }

        public async Task<IViewComponentResult> InvokeAsync(int productId)
        {
            try
            {
                var comments = await _dbQueryService.GetCommentsByProductIdAsync(productId) 
                               ?? new List<Comment>();

                var commentStats = CalculateCommentStats(comments);

                var model = new CommentViewModel
                {
                    ProductId = productId,
                    Comments = comments,
                    AverageRating = commentStats.AverageRating,
                    TotalComments = commentStats.TotalComments,
                    RatingDistribution = commentStats.RatingDistribution
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load comments for product {ProductId}", productId);

                var model = new CommentViewModel
                {
                    ProductId = productId,
                    Comments = new List<Comment>(),
                    AverageRating = 0,
                    TotalComments = 0,
                    RatingDistribution = new Dictionary<int, int> { { 5, 0 }, { 4, 0 }, { 3, 0 }, { 2, 0 }, { 1, 0 } }
                };
                return View(model);
            }
        }


        private CommentStats CalculateCommentStats(List<Comment> comments)
        {
            if (!comments.Any())
            {
                return new CommentStats
                {
                    AverageRating = 0,
                    TotalComments = 0,
                    RatingDistribution = new Dictionary<int, int> { { 5, 0 }, { 4, 0 }, { 3, 0 }, { 2, 0 }, { 1, 0 } }
                };
            }

            var averageRating = Math.Round(comments.Average(c => c.Rating), 1);
            var totalComments = comments.Count;
            
            var ratingDistribution = new Dictionary<int, int>();
            for (int i = 5; i >= 1; i--)
            {
                ratingDistribution[i] = comments.Count(c => c.Rating == i);
            }

            return new CommentStats
            {
                AverageRating = averageRating,
                TotalComments = totalComments,
                RatingDistribution = ratingDistribution
            };
        }
    }

    public class CommentViewModel
    {
        public int ProductId { get; set; }
        public List<Comment> Comments { get; set; } = new();
        public double AverageRating { get; set; }
        public int TotalComments { get; set; }
        public Dictionary<int, int> RatingDistribution { get; set; } = new();
    }

    public class CommentStats
    {
        public double AverageRating { get; set; }
        public int TotalComments { get; set; }
        public Dictionary<int, int> RatingDistribution { get; set; } = new();
    }
}
