using Microsoft.AspNetCore.Mvc;
using MRIV.Services.Reports;
using MRIV.ViewModels.Reports.Requisition;
using MRIV.ViewModels.Reports.Filters;
using System.Threading.Tasks;
using MRIV.Attributes;

namespace MRIV.Controllers.Reports
{
    [CustomAuthorize]
    public class RequisitionReportsController : Controller
    {
        private readonly IRequisitionReportService _reportService;
        private readonly IReportFilterService _filterService;
        public RequisitionReportsController(IRequisitionReportService reportService, IReportFilterService filterService)
        {
            _reportService = reportService;
            _filterService = filterService;
        }

        // GET: /Reports/RequisitionReports/ByLocation
        public async Task<IActionResult> ByLocationAsync(RequisitionReportFilterViewModel filters)
        { 
            var userPayrollNo = HttpContext.Session.GetString("EmployeePayrollNo");
            if (userPayrollNo == null)
                return RedirectToAction("Index", "Login");

            var filterDefinitions = await _filterService.GetRequisitionByLocationFiltersAsync(userPayrollNo);
            var model = await _reportService.GetRequisitionsByLocation(filters, userPayrollNo);
            ViewBag.Filters = filterDefinitions;
            ViewBag.SelectedFilters = filters;
            return View("~/Views/Reports/Requisition/ByLocation.cshtml", model);
        }
    }
} 