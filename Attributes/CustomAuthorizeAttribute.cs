using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace MRIV.Attributes
{
    public class CustomAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        public string ReturnUrl { get; set; }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Check if the session contains "EmployeePayrollNo"
            var isAuthenticated = context.HttpContext.Session.GetString("EmployeePayrollNo") != null;

            if (!isAuthenticated)
            {
                // Determine the return URL
                string returnUrl;
                
                if (!string.IsNullOrEmpty(ReturnUrl))
                {
                    // Use the provided ReturnUrl property
                    returnUrl = ReturnUrl;
                }
                else
                {
                    // Get the complete current URL including path, route values, and query string
                    returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                }

                // Encode the returnUrl to ensure it's properly formatted
                var encodedReturnUrl = Uri.EscapeDataString(returnUrl);

                // Redirect to Login page with returnUrl
                context.Result = new RedirectToActionResult("Index", "Login", new { returnUrl = encodedReturnUrl });
            }
        }
    }
}
