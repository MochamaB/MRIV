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
        private readonly IUserProfileService _userProfileService;
        private readonly KtdaleaveContext _context;
        private readonly ILogger<LoginController> _logger;

        public LoginController(
            IUserAuthenticationService authenticationService,
            IUserProfileService userProfileService,
            KtdaleaveContext context,
            ILogger<LoginController> logger)
        {
            _authenticationService = authenticationService;
            _userProfileService = userProfileService;
            _context = context;
            _logger = logger;
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
                try
                {
                    // Store basic employee information in session (backward compatibility)
                    HttpContext.Session.SetObject("Employee", employee);
                    HttpContext.Session.SetString("EmployeeName", employee.Fullname);
                    HttpContext.Session.SetString("EmployeePayrollNo", employee.PayrollNo);
                    HttpContext.Session.SetString("EmployeeDepartmentID", employee.Department);
                    HttpContext.Session.SetString("EmployeeRole", employee.Role);

                    // Enhanced Login: Build and cache comprehensive user profile
                    _logger.LogInformation("Building user profile for enhanced login: {PayrollNo}", employee.PayrollNo);
                    var userProfile = await _userProfileService.BuildUserProfileAsync(employee.PayrollNo);
                    
                    if (userProfile != null)
                    {
                        _logger.LogInformation("Successfully built user profile for PayrollNo: {PayrollNo}", employee.PayrollNo);
                        
                        // Console output for debugging UserProfile values
                        Console.WriteLine("=== USER PROFILE CREATED ===");
                        Console.WriteLine($"PayrollNo: {userProfile.BasicInfo.PayrollNo}");
                        Console.WriteLine($"Name: {userProfile.BasicInfo.Name}");
                        Console.WriteLine($"Department: {userProfile.BasicInfo.Department}");
                        Console.WriteLine($"Station: {userProfile.BasicInfo.Station}");
                        Console.WriteLine($"Role: {userProfile.BasicInfo.Role}");
                        Console.WriteLine($"IsAdmin: {userProfile.RoleInformation.IsAdmin}");
                        Console.WriteLine($"Permission Level: {userProfile.VisibilityScope.PermissionLevel}");
                        Console.WriteLine($"CanAccessAcrossStations: {userProfile.VisibilityScope.CanAccessAcrossStations}");
                        Console.WriteLine($"CanAccessAcrossDepartments: {userProfile.VisibilityScope.CanAccessAcrossDepartments}");
                        Console.WriteLine($"Home Department: {userProfile.LocationAccess.HomeDepartment.Name} (ID: {userProfile.LocationAccess.HomeDepartment.Id})");
                        Console.WriteLine($"Home Station: {userProfile.LocationAccess.HomeStation.Name} (ID: {userProfile.LocationAccess.HomeStation.Id})");
                        Console.WriteLine($"Role Groups Count: {userProfile.RoleInformation.RoleGroups.Count}");
                        
                        if (userProfile.RoleInformation.RoleGroups.Any())
                        {
                            Console.WriteLine("Role Groups:");
                            foreach (var rg in userProfile.RoleInformation.RoleGroups)
                            {
                                Console.WriteLine($"  - {rg.Name}: CanAccessAcrossStations={rg.CanAccessAcrossStations}, CanAccessAcrossDepartments={rg.CanAccessAcrossDepartments}");
                            }
                        }
                        
                        Console.WriteLine($"Accessible Departments Count: {userProfile.LocationAccess.AccessibleDepartmentIds.Count}");
                        Console.WriteLine($"Accessible Stations Count: {userProfile.LocationAccess.AccessibleStationIds.Count}");
                        Console.WriteLine($"Cache Expires At: {userProfile.CacheInfo.ExpiresAt}");
                        Console.WriteLine("=== END USER PROFILE ===");
                        
                        // Store additional profile context in session for quick access
                        HttpContext.Session.SetString("UserProfileCreated", "true");
                        HttpContext.Session.SetString("UserPermissionLevel", userProfile.VisibilityScope.PermissionLevel.ToString());
                        HttpContext.Session.SetString("CanAccessAcrossStations", userProfile.VisibilityScope.CanAccessAcrossStations.ToString());
                        HttpContext.Session.SetString("CanAccessAcrossDepartments", userProfile.VisibilityScope.CanAccessAcrossDepartments.ToString());
                    }
                    else
                    {
                        _logger.LogWarning("Failed to build user profile for PayrollNo: {PayrollNo}, continuing with basic login", employee.PayrollNo);
                    }
                }
                catch (Exception ex)
                {
                    // Enhanced login failed - continue with basic login for backward compatibility
                    _logger.LogError(ex, "Error during enhanced login for PayrollNo: {PayrollNo}, continuing with basic login", employee.PayrollNo);
                    HttpContext.Session.SetString("UserProfileCreated", "false");
                }

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
