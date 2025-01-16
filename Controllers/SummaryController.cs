using Microsoft.AspNetCore.Mvc;

namespace MRIV.Controllers
{
    public class SummaryController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
