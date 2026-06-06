using Microsoft.AspNetCore.Mvc;

namespace FE.Controllers
{
    public class Dashboard : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult News()
        {
            return View();
        }
        public IActionResult SystemAccounts()
        {
            return View();
        }
        public IActionResult Category()
        {
            return View();
        }
        public IActionResult Tag()
        {
            return View();
        }
        public IActionResult AuditLog()
        {
            return View();
        }
    }
}
