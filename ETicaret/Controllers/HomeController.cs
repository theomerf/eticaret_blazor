using Microsoft.AspNetCore.Mvc;

namespace ETicaret.Controllers{
    public class HomeController : Controller{
        public IActionResult Index(){
            return View();
        }
    }
}