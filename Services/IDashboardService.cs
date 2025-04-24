using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MRIV.Enums;
using MRIV.Extensions;
using MRIV.Models;
using MRIV.ViewModels;
using System.Threading.Tasks;

namespace MRIV.Services
{
    public interface IDashboardService
    {
        Task<MyRequisitionsDashboardViewModel> GetMyRequisitionsDashboardAsync(HttpContext httpContext);
        Task<DepartmentDashboardViewModel> GetDepartmentDashboardAsync(HttpContext httpContext);
    }
    public class DashboardService : IDashboardService
    {
        private readonly RequisitionContext _context;
        private readonly IEmployeeService _employeeService;

        public DashboardService(RequisitionContext context, IEmployeeService employeeService)
        {
            _context = context;
            _employeeService = employeeService;
        }

        public async Task<MyRequisitionsDashboardViewModel> GetMyRequisitionsDashboardAsync(HttpContext httpContext)
        {
            // Get current user's payroll number from session
            var payrollNo = httpContext.Session.GetString("EmployeePayrollNo");
            if (string.IsNullOrEmpty(payrollNo))
            {
                return new MyRequisitionsDashboardViewModel();
            }

            // Get employee, department, and station information
            var (loggedInUserEmployee, loggedInUserDepartment, loggedInUserStation) =
                await _employeeService.GetEmployeeAndDepartmentAsync(payrollNo);

            // Create dashboard view model
            var viewModel = new MyRequisitionsDashboardViewModel();

            // Get requisitions for the current user
            var userRequisitions = await _context.Requisitions
                .Where(r => r.PayrollNo == payrollNo)
                .Include(r => r.RequisitionItems)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // Calculate metrics
            viewModel.TotalRequisitions = userRequisitions.Count();
            viewModel.PendingRequisitions = userRequisitions.Count(r => r.Status == RequisitionStatus.NotStarted ||
                                                                      r.Status == RequisitionStatus.PendingDispatch ||
                                                                      r.Status == RequisitionStatus.PendingReceipt);
            viewModel.CompletedRequisitions = userRequisitions.Count(r => r.Status == RequisitionStatus.Completed);
            viewModel.CancelledRequisitions = userRequisitions.Count(r => r.Status == RequisitionStatus.Cancelled);

            // Get status distribution for chart
            var statusCounts = userRequisitions
                .GroupBy(r => r.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToList();

            foreach (var status in Enum.GetValues(typeof(RequisitionStatus)).Cast<RequisitionStatus>())
            {
                var count = statusCounts.FirstOrDefault(s => s.Status == status)?.Count ?? 0;
                viewModel.RequisitionStatusCounts.Add(status.GetDescription(), count);
            }

            // Get recent requisitions
            viewModel.RecentRequisitions = userRequisitions
                .Take(5)
                .Select(r => new RequisitionSummary
                {
                    Id = r.Id,
                    IssueStation = r.IssueStation,
                    DeliveryStation = r.DeliveryStation,
                    Status = r.Status ?? RequisitionStatus.NotStarted,
                    StatusDescription = r.Status?.GetDescription() ?? "Not Started",
                    CreatedAt = r.CreatedAt,
                    ItemCount = r.RequisitionItems?.Count() ?? 0
                })
                .ToList();

            return viewModel;
        }

        public async Task<DepartmentDashboardViewModel> GetDepartmentDashboardAsync(HttpContext httpContext)
        {
            // Get current user's payroll number from session
            var payrollNo = httpContext.Session.GetString("EmployeePayrollNo");
            if (string.IsNullOrEmpty(payrollNo))
            {
                return new DepartmentDashboardViewModel();
            }

            // Get employee, department, and station information
            var (loggedInUserEmployee, loggedInUserDepartment, loggedInUserStation) =
                await _employeeService.GetEmployeeAndDepartmentAsync(payrollNo);

            if (loggedInUserDepartment == null)
            {
                return new DepartmentDashboardViewModel();
            }

            // Create dashboard view model
            var viewModel = new DepartmentDashboardViewModel
            {
                DepartmentName = loggedInUserDepartment.DepartmentName
            };

            // Get all requisitions for the department
            var departmentRequisitions = await _context.Requisitions
                .Where(r => r.DepartmentId == loggedInUserDepartment.DepartmentCode)
                .Include(r => r.RequisitionItems)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // Calculate metrics
            viewModel.TotalDepartmentRequisitions = departmentRequisitions.Count();
            viewModel.PendingDepartmentRequisitions = departmentRequisitions.Count(r => r.Status == RequisitionStatus.NotStarted ||
                                                                                r.Status == RequisitionStatus.PendingDispatch ||
                                                                                r.Status == RequisitionStatus.PendingReceipt);
            viewModel.CompletedDepartmentRequisitions = departmentRequisitions.Count(r => r.Status == RequisitionStatus.Completed);
            viewModel.CancelledDepartmentRequisitions = departmentRequisitions.Count(r => r.Status == RequisitionStatus.Cancelled);

            // Get status distribution for chart
            var statusCounts = departmentRequisitions
                .GroupBy(r => r.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToList();

            foreach (var status in Enum.GetValues(typeof(RequisitionStatus)).Cast<RequisitionStatus>())
            {
                var count = statusCounts.FirstOrDefault(s => s.Status == status)?.Count ?? 0;
                viewModel.DepartmentRequisitionStatusCounts.Add(status.GetDescription(), count);
            }

            // Get recent department requisitions
            viewModel.RecentDepartmentRequisitions = departmentRequisitions
                .Take(5)
                .Select(r => new RequisitionSummary
                {
                    Id = r.Id,
                    IssueStation = r.IssueStation,
                    DeliveryStation = r.DeliveryStation,
                    Status = r.Status ?? RequisitionStatus.NotStarted,
                    StatusDescription = r.Status?.GetDescription() ?? "Not Started",
                    CreatedAt = r.CreatedAt,
                    ItemCount = r.RequisitionItems?.Count() ?? 0,
                    PayrollNo = r.PayrollNo
                })
                .ToList();

            // Get employee names for the requisitions
            var payrollNumbers = viewModel.RecentDepartmentRequisitions.Select(r => r.PayrollNo).Distinct().ToList();

            // Retrieve employees one by one since there's no bulk method available
            foreach (var requisition in viewModel.RecentDepartmentRequisitions)
            {
                var employee = await _employeeService.GetEmployeeByPayrollAsync(requisition.PayrollNo);
                requisition.EmployeeName = employee != null ? $"{employee.SurName} {employee.OtherNames}" : requisition.PayrollNo;
            }

            return viewModel;
        }
    }
}
