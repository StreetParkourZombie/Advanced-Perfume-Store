using Microsoft.AspNetCore.Mvc;
using PerfumeStore.Models;

namespace PerfumeStore.ViewComponents
{
    public class ReviewFormViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(string productName, int productId)
        {
            var model = new ReviewFormViewModel
            {
                ProductName = productName,
                ProductId = productId
            };

            return View(model);
        }
    }

    public class ReviewFormViewModel
    {
        public string ProductName { get; set; } = string.Empty;
        public int ProductId { get; set; }
    }
}
