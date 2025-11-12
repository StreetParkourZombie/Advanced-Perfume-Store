using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerfumeStore.Models;
using System.Security.Claims;

namespace PerfumeStore.Controllers
{
    [Authorize]
    public class CommentsController : Controller
    {
        private readonly PerfumeStoreContext _db;
        private readonly ILogger<CommentsController> _logger;

        public CommentsController(PerfumeStoreContext db, ILogger<CommentsController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, int rating, string content, CancellationToken ct)
        {
            if (productId <= 0 || rating < 1 || rating > 5 || string.IsNullOrWhiteSpace(content) || content.Trim().Length < 5)
            {
                return BadRequest(new { message = "Dữ liệu không hợp lệ." });
            }

            var customerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(customerIdClaim) || !int.TryParse(customerIdClaim, out var customerId))
            {
                return Unauthorized(new { message = "Bạn cần đăng nhập để bình luận." });
            }

            // Ensure product exists
            var exists = await _db.Products.AnyAsync(p => p.ProductId == productId, ct);
            if (!exists)
            {
                return NotFound(new { message = "Sản phẩm không tồn tại." });
            }

            var now = DateTime.UtcNow;

            // Insert or update comment (composite key ProductId + CustomerId). Always reset to pending review.
            var comment = await _db.Comments.FirstOrDefaultAsync(c => c.ProductId == productId && c.CustomerId == customerId, ct);
            if (comment == null)
            {
                comment = new Comment
                {
                    ProductId = productId,
                    CustomerId = customerId,
                    CommentDate = now,
                    Rating = rating,
                    Content = content.Trim(),
                    IsPublished = false
                };
                _db.Comments.Add(comment);
            }
            else
            {
                comment.Rating = rating;
                comment.Content = content.Trim();
                comment.CommentDate = now;
                comment.IsPublished = false; // resets to pending on edit
                _db.Comments.Update(comment);
            }

            await _db.SaveChangesAsync(ct);

            return Ok(new { message = "Bình luận đã gửi. Vui lòng chờ phê duyệt." });
        }
    }
}


