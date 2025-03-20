using Microsoft.AspNetCore.Mvc;

namespace MRIV.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
