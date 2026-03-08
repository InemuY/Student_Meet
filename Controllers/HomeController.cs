using Microsoft.AspNetCore.Mvc;

namespace NIRApp.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
