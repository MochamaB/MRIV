using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MRIV.Extensions;
using MRIV.Models;
using MRIV.Services;


namespace MRIV.Controllers.Auth
{
    public class LoginController : Controller
    {
        private readonly IUserAuthenticationService _authenticationService;
        private readonly KtdaleaveContext _context;
        public LoginController(
            IUserAuthenticationService authenticationService, 
            KtdaleaveContext context)
        {
            _authenticationService = authenticationService;
            _context = context;
        }
        public IActionResult Index(string returnUrl = null)
        {
            // Store the returnUrl in ViewBag to pass it to the form
            ViewBag.ReturnUrl = returnUrl;
            return View("~/Views/Auth/Login.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> Authenticate(string payrollNo, string password, string returnUrl = null)
        {
            if (string.IsNullOrEmpty(payrollNo) || string.IsNullOrEmpty(password))
            {
                ViewBag.ErrorMessage = "Payroll number and password are required.";
                ViewBag.ReturnUrl = returnUrl;
                return View("~/Views/Auth/Login.cshtml");
            }

            Console.WriteLine($"Received payrollNo: {payrollNo}, password: {password}");

            var employee = await _authenticationService.AuthenticateAsync(payrollNo, password);

            if (employee != null)
            {
                // Store employee information in session
                HttpContext.Session.SetObject("Employee", employee);
                HttpContext.Session.SetString("EmployeeName", employee.Fullname);
                HttpContext.Session.SetString("EmployeePayrollNo", employee.PayrollNo);
                HttpContext.Session.SetString("EmployeeDepartmentID", employee.Department);
                HttpContext.Session.SetString("EmployeeRole", employee.Role);

                // Authentication successful - Handle returnUrl logic
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    // Decode the URL before redirecting
                    returnUrl = Uri.UnescapeDataString(returnUrl);
                    Console.WriteLine($"Decoded ReturnUrl: {returnUrl}");
                    Console.WriteLine($"Is Local URL: {Url.IsLocalUrl(returnUrl)}");

                    // Only redirect if it's a local URL for security
                    if (Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                }

                return RedirectToAction("Index", "Dashboard");
            }
            else
            {
                ViewBag.ErrorMessage = "The payroll number and password do not match";
                ViewBag.ReturnUrl = returnUrl;
                return View("~/Views/Auth/Login.cshtml");
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            // Clear the session
            HttpContext.Session.Clear();

            // Redirect to the home/login page instead of returning a view
            // This will change the URL in the browser to the root URL
            return RedirectToAction("Index", "Login");
        }


    }
}
