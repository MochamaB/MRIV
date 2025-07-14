using Microsoft.AspNetCore.Mvc;
using MRIV.Attributes;
using MRIV.ViewModels.Reports;

namespace MRIV.Controllers.Reports
{
    [CustomAuthorize]
    public class ReportsController : Controller
    {
        public IActionResult Index()
        {
            // For now, just return the view. You can pass a ViewModel if needed for dynamic content.
            return View();
        }
    }
} 