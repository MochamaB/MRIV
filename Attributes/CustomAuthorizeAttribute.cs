using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace MRIV.Attributes
{
    public class CustomAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Check if the session contains "EmployeePayrollNo"
            var isAuthenticated = context.HttpContext.Session.GetString("EmployeePayrollNo") != null;

            if (!isAuthenticated)
            {
                // Get the current request path and query string
                var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;

                // Encode the returnUrl to ensure it's properly formatted
                var encodedReturnUrl = Uri.EscapeDataString(returnUrl);

                // Redirect to Login page with returnUrl
                context.Result = new RedirectToActionResult("Index", "Login", new { returnUrl = encodedReturnUrl });
            }
        }
    }
}
