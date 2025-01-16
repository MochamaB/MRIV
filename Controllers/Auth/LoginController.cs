using Microsoft.AspNetCore.Mvc;

namespace MRIV.Controllers.Auth
{
    public class LoginController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Auth/Login.cshtml");
        }

        [HttpPost]
        public IActionResult Authenticate(string Email, string Password, string returnUrl)
        {
            return RedirectToAction("Index", "Summary");
        }
    }
}
