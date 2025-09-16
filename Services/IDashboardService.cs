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

        // New enhanced methods
        Task<TrendAnalysisData> GetTrendDataAsync(string payrollNo);
        Task<ActionRequiredSection> GetActionRequiredAsync(string payrollNo);
        Task<QuickStatsData> GetQuickStatsAsync(string payrollNo);
    }
    public class DashboardService : IDashboardService
    {
        private readonly RequisitionContext _context;
        private readonly IEmployeeService _employeeService;
        private readonly IDepartmentService _departmentService;
        private readonly IUserProfileService _userProfileService;

        public DashboardService(RequisitionContext context, IEmployeeService employeeService, IDepartmentService departmentService, IUserProfileService userProfileService)
        {
            _context = context;
            _employeeService = employeeService;
            _departmentService = departmentService;
            _userProfileService = userProfileService;
        }

        public async Task<MyRequisitionsDashboardViewModel> GetMyRequisitionsDashboardAsync(HttpContext httpContext)
        {
            try
            {
                // Try UserProfile first, fallback to session
                var userProfile = await _userProfileService.GetCurrentUserProfileAsync();
                var payrollNo = userProfile?.BasicInfo?.PayrollNo ?? httpContext.Session.GetString("EmployeePayrollNo");

                if (string.IsNullOrEmpty(payrollNo))
                {
                    return new MyRequisitionsDashboardViewModel();
                }

                // Get employee, department, and station information
                var (loggedInUserEmployee, loggedInUserDepartment, loggedInUserStation) =
                    await _employeeService.GetEmployeeAndDepartmentAsync(payrollNo);

                // Create enhanced dashboard view model
                var viewModel = new MyRequisitionsDashboardViewModel();

                // Set user context from profile if available
                if (userProfile != null)
                {
                    viewModel.UserInfo = new UserDashboardInfo
                    {
                        Name = userProfile.BasicInfo.Name,
                        Department = userProfile.BasicInfo.Department,
                        Station = userProfile.BasicInfo.Station,
                        Role = userProfile.BasicInfo.Role,
                        PayrollNo = userProfile.BasicInfo.PayrollNo,
                        LastLogin = userProfile.CacheInfo.LastRefresh
                    };
                    viewModel.CanAccessDepartmentData = userProfile.VisibilityScope.CanAccessAcrossDepartments;
                    viewModel.CanAccessStationData = userProfile.VisibilityScope.CanAccessAcrossStations;
                 //   viewModel.PermissionLevel = userProfile.VisibilityScope.PermissionLevel;
                }

                // Get requisitions for the current user
                var userRequisitions = await _context.Requisitions
                    .Where(r => r.PayrollNo == payrollNo)
                    .Include(r => r.RequisitionItems)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                // Calculate enhanced primary metrics with real trends
                await CalculateEnhancedMetrics(viewModel, userRequisitions, payrollNo);

                // Get action required items
                await CalculateActionRequired(viewModel, userRequisitions, payrollNo);

                // Calculate quick stats
                await CalculateQuickStats(viewModel, userRequisitions);

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

                // Get enhanced recent requisitions
                await GetEnhancedRecentRequisitions(viewModel, userRequisitions);

                // Generate trend analysis data
                await GenerateTrendData(viewModel, userRequisitions);

                return viewModel;
            }
            catch (Exception ex)
            {
                // Log error and return basic dashboard
                return new MyRequisitionsDashboardViewModel
                {
                    ErrorMessage = "Unable to load dashboard data. Please try again."
                };
            }
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
            var recentDepartmentRequisitions = departmentRequisitions.Take(5).ToList();
            viewModel.RecentDepartmentRequisitions = new List<RequisitionSummary>();
            
            foreach (var r in recentDepartmentRequisitions)
            {
                // Resolve location names
                string issueLocationName = await _departmentService.GetLocationNameFromIdsAsync(
                    r.IssueStationId, r.IssueDepartmentId);
                
                string deliveryLocationName = await _departmentService.GetLocationNameFromIdsAsync(
                    r.DeliveryStationId, r.DeliveryDepartmentId);
                
                viewModel.RecentDepartmentRequisitions.Add(new RequisitionSummary
                {
                    Id = r.Id,
                    IssueStation = issueLocationName,
                    DeliveryStation = deliveryLocationName,
                    IssueStationId = r.IssueStationId,
                    IssueDepartmentId = r.IssueDepartmentId,
                    DeliveryStationId = r.DeliveryStationId,
                    DeliveryDepartmentId = r.DeliveryDepartmentId,
                    Status = r.Status ?? RequisitionStatus.NotStarted,
                    StatusDescription = r.Status?.GetDescription() ?? "Not Started",
                    CreatedAt = r.CreatedAt,
                    ItemCount = r.RequisitionItems?.Count() ?? 0,
                    PayrollNo = r.PayrollNo
                });
            }

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

        #region Enhanced Helper Methods

        /// <summary>
        /// Calculate enhanced primary metrics with real trend data
        /// </summary>
        private async Task CalculateEnhancedMetrics(MyRequisitionsDashboardViewModel viewModel, List<Requisition> userRequisitions, string payrollNo)
        {
            var now = DateTime.UtcNow;
            var thisMonth = new DateTime(now.Year, now.Month, 1);
            var lastMonth = thisMonth.AddMonths(-1);

            // Current month requisitions
            var thisMonthRequisitions = userRequisitions.Where(r => r.CreatedAt >= thisMonth).ToList();
            var lastMonthRequisitions = userRequisitions.Where(r => r.CreatedAt >= lastMonth && r.CreatedAt < thisMonth).ToList();

            // Primary counts
            viewModel.TotalRequisitions = userRequisitions.Count;
            viewModel.ThisMonthRequisitions = thisMonthRequisitions.Count;
            viewModel.PendingRequisitions = userRequisitions.Count(r =>
                r.Status == RequisitionStatus.NotStarted ||
                r.Status == RequisitionStatus.PendingDispatch ||
                r.Status == RequisitionStatus.PendingReceipt);
            viewModel.CompletedRequisitions = userRequisitions.Count(r => r.Status == RequisitionStatus.Completed);
            viewModel.CancelledRequisitions = userRequisitions.Count(r => r.Status == RequisitionStatus.Cancelled);

            // Action required count
            viewModel.AwaitingMyAction = userRequisitions.Count(r =>
                r.Status == RequisitionStatus.PendingReceipt && r.PayrollNo == payrollNo);

            // Overdue requisitions (using CreatedAt + 7 days as approximation since ExpectedDeliveryDate doesn't exist)
            viewModel.OverdueRequisitions = userRequisitions.Count(r =>
                r.CreatedAt.HasValue &&
                r.CreatedAt.Value.AddDays(7) < now &&
                r.Status != RequisitionStatus.Completed &&
                r.Status != RequisitionStatus.Cancelled);

            // Calculate real trends
            viewModel.ThisMonthTrend = CalculatePercentageChange(thisMonthRequisitions.Count, lastMonthRequisitions.Count);

            var thisMonthPending = thisMonthRequisitions.Count(r => r.Status == RequisitionStatus.PendingDispatch || r.Status == RequisitionStatus.PendingReceipt);
            var lastMonthPending = lastMonthRequisitions.Count(r => r.Status == RequisitionStatus.PendingDispatch || r.Status == RequisitionStatus.PendingReceipt);
            viewModel.PendingRequisitionsTrend = CalculatePercentageChange(thisMonthPending, lastMonthPending);

            var thisMonthCompleted = thisMonthRequisitions.Count(r => r.Status == RequisitionStatus.Completed);
            var lastMonthCompleted = lastMonthRequisitions.Count(r => r.Status == RequisitionStatus.Completed);
            viewModel.CompletedRequisitionsTrend = CalculatePercentageChange(thisMonthCompleted, lastMonthCompleted);

            // Average processing time (using CompleteDate property that exists)
            var completedRequisitions = userRequisitions.Where(r => r.Status == RequisitionStatus.Completed && r.CompleteDate.HasValue).ToList();
            if (completedRequisitions.Any())
            {
                viewModel.AverageProcessingDays = Math.Round(completedRequisitions.Average(r => (r.CompleteDate.Value - r.CreatedAt.Value).TotalDays), 1);
            }
        }

        /// <summary>
        /// Calculate action required items
        /// </summary>
        private async Task CalculateActionRequired(MyRequisitionsDashboardViewModel viewModel, List<Requisition> userRequisitions, string payrollNo)
        {
            var now = DateTime.UtcNow;

            viewModel.ActionRequired = new ActionRequiredSection
            {
                PendingReceiptConfirmation = userRequisitions.Count(r =>
                    r.Status == RequisitionStatus.PendingReceipt && r.PayrollNo == payrollNo),
                RequiringClarification = userRequisitions.Count(r =>
                    r.Status == RequisitionStatus.NotStarted && r.PayrollNo == payrollNo && r.CreatedAt < DateTime.UtcNow.AddDays(-3)),
                OverdueItems = userRequisitions.Count(r =>
                    r.CreatedAt.HasValue && r.CreatedAt.Value.AddDays(7) < now &&
                    r.Status != RequisitionStatus.Completed && r.Status != RequisitionStatus.Cancelled),
                ReadyForCollection = userRequisitions.Count(r => r.Status == RequisitionStatus.PendingReceipt),
                UrgentRequisitions = userRequisitions
                    .Where(r => r.Status != RequisitionStatus.Completed && r.Status != RequisitionStatus.Cancelled &&
                               r.CreatedAt.HasValue && r.CreatedAt.Value.AddDays(5) < now) // Consider urgent if over 5 days old
                    .Select(r => new UrgentRequisition
                    {
                        RequisitionId = r.Id,
                        Description = $"Requisition #{r.Id}",
                        DaysOverdue = r.CreatedAt.HasValue ? Math.Max(0, (int)(now - r.CreatedAt.Value.AddDays(7)).TotalDays) : 0,
                        ActionRequired = GetActionRequired(r.Status),
                        Priority = "High" // Default to High since we don't have Priority field
                    }).Take(10).ToList()
            };
        }

        /// <summary>
        /// Calculate quick stats for dashboard
        /// </summary>
        private async Task CalculateQuickStats(MyRequisitionsDashboardViewModel viewModel, List<Requisition> userRequisitions)
        {
            var now = DateTime.UtcNow;
            var thisMonth = new DateTime(now.Year, now.Month, 1);

            viewModel.QuickStats = new QuickStatsData
            {
                CompletedThisMonth = userRequisitions.Count(r => r.Status == RequisitionStatus.Completed && r.CreatedAt >= thisMonth),
                AverageItemsPerRequisition = userRequisitions.Any() ? Math.Round(userRequisitions.Average(r => r.RequisitionItems?.Count() ?? 0), 1) : 0,
                MostRequestedCategory = "General", // Since Category doesn't exist in RequisitionItem, use default
                FastestCompletionDays = userRequisitions.Where(r => r.Status == RequisitionStatus.Completed && r.CompleteDate.HasValue && r.CreatedAt.HasValue)
                    .Any() ? userRequisitions.Where(r => r.Status == RequisitionStatus.Completed && r.CompleteDate.HasValue && r.CreatedAt.HasValue)
                    .Min(r => (r.CompleteDate.Value - r.CreatedAt.Value).TotalDays) : 0,
                TotalItemsRequested = userRequisitions.SelectMany(r => r.RequisitionItems).Sum(item => item.Quantity), // Use Quantity instead of QuantityRequested
                ActiveRequisitions = userRequisitions.Count(r => r.Status != RequisitionStatus.Completed && r.Status != RequisitionStatus.Cancelled)
            };
        }

        /// <summary>
        /// Get enhanced recent requisitions with additional data
        /// </summary>
        private async Task GetEnhancedRecentRequisitions(MyRequisitionsDashboardViewModel viewModel, List<Requisition> userRequisitions)
        {
            var recentRequisitions = userRequisitions.Take(10).ToList();
            viewModel.RecentRequisitions = new List<RequisitionSummary>();

            foreach (var r in recentRequisitions)
            {
                string issueLocationName = await _departmentService.GetLocationNameFromIdsAsync(r.IssueStationId, r.IssueDepartmentId);
                string deliveryLocationName = await _departmentService.GetLocationNameFromIdsAsync(r.DeliveryStationId, r.DeliveryDepartmentId);

                var employee = await _employeeService.GetEmployeeByPayrollAsync(r.PayrollNo);
                string employeeName = employee != null ? $"{employee.SurName} {employee.OtherNames}" : r.PayrollNo;

                viewModel.RecentRequisitions.Add(new RequisitionSummary
                {
                    Id = r.Id,
                    IssueStation = issueLocationName,
                    DeliveryStation = deliveryLocationName,
                    IssueStationId = r.IssueStationId,
                    IssueDepartmentId = r.IssueDepartmentId,
                    DeliveryStationId = r.DeliveryStationId,
                    DeliveryDepartmentId = r.DeliveryDepartmentId,
                    Status = r.Status ?? RequisitionStatus.NotStarted,
                    StatusDescription = r.Status?.GetDescription() ?? "Not Started",
                    CreatedAt = r.CreatedAt,
                    ItemCount = r.RequisitionItems?.Count() ?? 0,
                    PayrollNo = r.PayrollNo,
                    EmployeeName = employeeName,
                    Priority = "Normal", // Default since Priority doesn't exist
                    ExpectedDeliveryDate = r.CreatedAt?.AddDays(7), // Estimate delivery date as 7 days from creation
                    DaysInCurrentStatus = CalculateDaysInStatus(r),
                    IsOverdue = r.CreatedAt.HasValue && r.CreatedAt.Value.AddDays(7) < DateTime.UtcNow && r.Status != RequisitionStatus.Completed
                });
            }
        }

        /// <summary>
        /// Generate trend analysis data for charts
        /// </summary>
        private async Task GenerateTrendData(MyRequisitionsDashboardViewModel viewModel, List<Requisition> userRequisitions)
        {
            var twelveMonthsAgo = DateTime.UtcNow.AddMonths(-12);
            var monthlyData = userRequisitions.Where(r => r.CreatedAt.HasValue && r.CreatedAt >= twelveMonthsAgo)
                .GroupBy(r => new { r.CreatedAt.Value.Year, r.CreatedAt.Value.Month })
                .Select(g => new MonthlyRequisitionData
                {
                    Month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM"),
                    Year = g.Key.Year,
                    Count = g.Count(),
                    Completed = g.Count(r => r.Status == RequisitionStatus.Completed),
                    Pending = g.Count(r => r.Status != RequisitionStatus.Completed && r.Status != RequisitionStatus.Cancelled),
                    Cancelled = g.Count(r => r.Status == RequisitionStatus.Cancelled)
                }).OrderBy(d => d.Year).ThenBy(d => DateTime.ParseExact(d.Month, "MMM", null).Month).ToList();

            var topItems = userRequisitions.SelectMany(r => r.RequisitionItems)
                .GroupBy(item => new { ItemCode = item.Name, ItemName = item.Name, Category = "General" }) // Use Name as ItemCode since ItemCode doesn't exist
                .Select(g => new TopRequestedItem
                {
                    ItemCode = g.Key.ItemCode ?? "Unknown",
                    ItemName = g.Key.ItemName ?? "Unknown",
                    Category = g.Key.Category,
                    RequestCount = g.Count(),
                    TotalQuantity = g.Sum(item => item.Quantity) // Use Quantity instead of QuantityRequested
                }).OrderByDescending(item => item.RequestCount).Take(10).ToList();

            var categoryData = new Dictionary<string, int> { { "General", userRequisitions.SelectMany(r => r.RequisitionItems).Count() } };

            viewModel.TrendData = new TrendAnalysisData
            {
                RequisitionsByMonth = monthlyData,
                TopRequestedItems = topItems,
                RequisitionsByCategory = categoryData
            };
        }

        /// <summary>
        /// New interface method implementations
        /// </summary>
        public async Task<TrendAnalysisData> GetTrendDataAsync(string payrollNo)
        {
            var userRequisitions = await _context.Requisitions
                .Where(r => r.PayrollNo == payrollNo)
                .Include(r => r.RequisitionItems)
                .ToListAsync();

            var trendData = new TrendAnalysisData();
            // Implementation similar to GenerateTrendData method
            return trendData;
        }

        public async Task<ActionRequiredSection> GetActionRequiredAsync(string payrollNo)
        {
            var userRequisitions = await _context.Requisitions
                .Where(r => r.PayrollNo == payrollNo)
                .Include(r => r.RequisitionItems)
                .ToListAsync();

            var actionRequired = new ActionRequiredSection();
            await CalculateActionRequired(new MyRequisitionsDashboardViewModel { ActionRequired = actionRequired }, userRequisitions, payrollNo);
            return actionRequired;
        }

        public async Task<QuickStatsData> GetQuickStatsAsync(string payrollNo)
        {
            var userRequisitions = await _context.Requisitions
                .Where(r => r.PayrollNo == payrollNo)
                .Include(r => r.RequisitionItems)
                .ToListAsync();

            var quickStats = new QuickStatsData();
            await CalculateQuickStats(new MyRequisitionsDashboardViewModel { QuickStats = quickStats }, userRequisitions);
            return quickStats;
        }

        #endregion

        #region Utility Methods

        private decimal CalculatePercentageChange(int current, int previous)
        {
            if (previous == 0) return current > 0 ? 100 : 0;
            return Math.Round(((decimal)(current - previous) / previous) * 100, 2);
        }

        private string GetActionRequired(RequisitionStatus? status)
        {
            return status switch
            {
                RequisitionStatus.PendingReceipt => "Confirm Receipt",
                RequisitionStatus.PendingDispatch => "Awaiting Dispatch",
                RequisitionStatus.NotStarted => "Pending Processing",
                _ => "Review Required"
            };
        }

        private int CalculateDaysInStatus(Requisition requisition)
        {
            var statusChangeDate = requisition.UpdatedAt ?? requisition.CreatedAt ?? DateTime.UtcNow;
            return (int)(DateTime.UtcNow - statusChangeDate).TotalDays;
        }

        #endregion
    }
}
