using Microsoft.AspNetCore.Mvc;

namespace PerfumeStore.Controllers
{
    public class AboutController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
