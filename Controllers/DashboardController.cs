using Microsoft.AspNetCore.Mvc;
using MRIV.Attributes;

namespace MRIV.Controllers
{
    [CustomAuthorize]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
