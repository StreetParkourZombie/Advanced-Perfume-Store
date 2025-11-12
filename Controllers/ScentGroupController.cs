using Microsoft.AspNetCore.Mvc;

namespace PerfumeStore.Controllers
{
    public class ScentGroupController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult DanhSach()
        {
            ViewData["Title"] = "Các nhóm hương nước hoa phổ biến";
            return View();
        }

        public IActionResult ThuVien()
        {
            ViewData["Title"] = "Thư viện nhóm hương";
            return View();
        }

        public IActionResult WoodyFruity()
        {
            ViewData["Title"] = "Nhóm Hương Gỗ Và Trái Cây";
            return View();
        }

        public IActionResult OrientalWoody()
        {
            ViewData["Title"] = "Nhóm Hương Gỗ Phương Đông";
            return View();
        }

        public IActionResult FloralWoodyMusk()
        {
            ViewData["Title"] = "Nhóm Hương Hoa Cỏ, Gỗ, Xạ Hương";
            return View();
        }

        public IActionResult FloralWoody()
        {
            ViewData["Title"] = "Nhóm Hương Hoa Cỏ Gỗ Thơm";
            return View();
        }

        public IActionResult FloralFruityWoody()
        {
            ViewData["Title"] = "Nhóm Hương Hoa Cỏ Trái Cây Gỗ";
            return View();
        }

        public IActionResult FloralFruityGourmand()
        {
            ViewData["Title"] = "Nhóm Hương Hoa Cỏ Trái Cây Ngọt Ngào";
            return View();
        }

        public IActionResult FloralFruity()
        {
            ViewData["Title"] = "Nhóm Hương Thơm Hoa Cỏ Trái Cây";
            return View();
        }
    }
}
