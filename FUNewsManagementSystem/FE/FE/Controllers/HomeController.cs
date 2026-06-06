using Microsoft.AspNetCore.Mvc;

namespace FE.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult News()
        {
            return View();

        }
    }
}
