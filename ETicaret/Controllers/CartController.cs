using Microsoft.AspNetCore.Mvc;

namespace ETicaret.Controllers
{
    public class CartController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Alışveriş Sepetim";
            ViewData["UseBlazor"] = true;

            return View();
        }
    }
}
