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
          //  return RedirectToAction("Index", "Summary");
            // Check if payrollNo or password is null or empty
            if (string.IsNullOrEmpty(payrollNo) || string.IsNullOrEmpty(password))
            {
                ViewBag.ErrorMessage = "Payroll number and password are required.";
                ViewBag.ReturnUrl = returnUrl; // Preserve returnUrl on validation error
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
                
                // Authentication successful
                // Redirect to authenticated page
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
                
                // Default redirect if returnUrl is empty or not local
                Console.WriteLine("Redirecting to Dashboard Index");
                return RedirectToAction("Index", "Dashboard");
            }
            else
            {
                // Authentication failed
                // Return login view with error message
                ViewBag.ErrorMessage = "The payroll number and password do not match";
                ViewBag.ReturnUrl = returnUrl; // Preserve returnUrl on authentication error
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
