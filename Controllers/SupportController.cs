using Microsoft.AspNetCore.Mvc;

namespace PerfumeStore.Controllers
{
    public class SupportController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
