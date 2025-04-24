using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MRIV.Attributes;
using MRIV.Enums;
using MRIV.Extensions;
using MRIV.Models;
using MRIV.Services;
using MRIV.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MRIV.Controllers
{
    [CustomAuthorize]
    public class DashboardController : Controller
    {
        private readonly RequisitionContext _context;
        private readonly IEmployeeService _employeeService;
        private readonly IDashboardService _dashboardService;

        public DashboardController(
            RequisitionContext context, 
            IEmployeeService employeeService,
            IDashboardService dashboardService)
        {
            _context = context;
            _employeeService = employeeService;
            _dashboardService = dashboardService;
        }

        // Default dashboard now directly returns the MyRequisitions view
        public async Task<IActionResult> Index()
        {
            var viewModel = await _dashboardService.GetMyRequisitionsDashboardAsync(HttpContext);
            return View("MyRequisitions", viewModel);
        }

        // My Requisitions Dashboard
        public async Task<IActionResult> MyRequisitions()
        {
            var viewModel = await _dashboardService.GetMyRequisitionsDashboardAsync(HttpContext);
            return View(viewModel);
        }

        // Department Dashboard
        public async Task<IActionResult> Department()
        {
            var viewModel = await _dashboardService.GetDepartmentDashboardAsync(HttpContext);
            return View(viewModel);
        }
    }
}
