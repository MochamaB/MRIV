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
        public IActionResult Index()
        {
            return View("~/Views/Auth/Login.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> Authenticate(string payrollNo, string password, string returnUrl = null)
        {
          //  return RedirectToAction("Index", "Summary");
            // Check if payrollNo or password is null or empty
            if (string.IsNullOrEmpty(payrollNo) || string.IsNullOrEmpty(password))
            {
                ViewBag.ErrorMessage = "Payroll number and password are required.";
                return View("~/Views/Auth/Login.cshtml");
            }

            Console.WriteLine($"Received payrollNo: {payrollNo}, password: {password}");
            if (await _authenticationService.AuthenticateAsync(payrollNo, password))
            {
                // Fetch employee details directly using DbContext
                var employee = await _context.EmployeeBkps.SingleOrDefaultAsync(e => e.PayrollNo == payrollNo);

                if (employee != null)
                {
                    // Store employee information in session
                    // Store the whole employee object in session
                    HttpContext.Session.SetObject("Employee", employee);
                    HttpContext.Session.SetString("EmployeeName", employee.Fullname);
                    HttpContext.Session.SetString("EmployeePayrollNo", employee.PayrollNo);
                    HttpContext.Session.SetString("EmployeeDepartmentID", employee.Department);
                    HttpContext.Session.SetString("EmployeeRole", employee.Role);


                }
                Console.WriteLine($"ReturnUrl: {returnUrl}"); // Log the returnUrl
                Console.WriteLine($"Is Local URL: {Url.IsLocalUrl(returnUrl)}");
                // Authentication successful
                // Redirect to authenticated page
                if (!string.IsNullOrEmpty(returnUrl)
                    //   && Url.IsLocalUrl(returnUrl)
                    )
                {
                    // Decode the URL before redirecting
                    returnUrl = Uri.UnescapeDataString(returnUrl);
                    return Redirect(returnUrl);
                }
                else
                {
                    Console.WriteLine("Redirecting to Summary Index");
                    return RedirectToAction("Index", "Summary");
                }
            }
            else
            {
                // Authentication failed
                // Return login view with error message
                ViewBag.ErrorMessage = "The payroll number and password do not match";
                return View("~/Views/Auth/Login.cshtml");
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            // Clear the session
            HttpContext.Session.Clear();

            // Redirect to the login page
            return View("~/Views/Auth/Login.cshtml");
        }


    }
}
