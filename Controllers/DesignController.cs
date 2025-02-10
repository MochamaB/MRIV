using Microsoft.AspNetCore.Mvc;

namespace MRIV.Controllers
{
    public class DesignController : Controller
    {
        public IActionResult Index()
        {
            return View("SearchDropdown");
        }
    }
}
