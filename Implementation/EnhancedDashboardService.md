using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MRIV.Enums;
using MRIV.Extensions;
using MRIV.Models;
using MRIV.Services;
using MRIV.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MRIV.Services
{
    /// <summary>
    /// Enhanced Dashboard Service with UserProfile integration and advanced analytics
    /// This replaces the existing DashboardService with enhanced functionality
    /// </summary>
    public class EnhancedDashboardService : IDashboardService
    {
        private readonly RequisitionContext _context;
        private readonly IEmployeeService _employeeService;
        private readonly IDepartmentService _departmentService;
        private readonly IUserProfileService _userProfileService;

        public EnhancedDashboardService(
            RequisitionContext context,
            IEmployeeService employeeService,
            IDepartmentService departmentService,
            IUserProfileService userProfileService)
        {
            _context = context;
            _employeeService = employeeService;
            _departmentService = departmentService;
            _userProfileService = userProfileService;
        }

        /// <summary>
        /// Enhanced MyRequisitions dashboard with UserProfile integration
        /// </summary>
        public async Task<MyRequisitionsDashboardViewModel> GetMyRequisitionsDashboardAsync(HttpContext httpContext)
        {
            try
            {
                // Get user profile instead of manual session extraction
                var userProfile = await _userProfileService.GetCurrentUserProfileAsync();

                if (userProfile?.BasicInfo?.PayrollNo == null)
                {
                    return new MyRequisitionsDashboardViewModel();
                }

                var payrollNo = userProfile.BasicInfo.PayrollNo;
                var viewModel = new MyRequisitionsDashboardViewModel();

                // Set user context from profile
                await SetUserContextAsync(viewModel, userProfile);

                // Get filtered requisitions based on user's access level
                var userRequisitions = await GetFilteredRequisitionsAsync(userProfile);

                // Calculate primary metrics with real trends
                await CalculatePrimaryMetricsAsync(viewModel, userRequisitions, payrollNo);

                // Get action required items
                await GetActionRequiredItemsAsync(viewModel, userRequisitions, payrollNo);

                // Get recent requisitions with enhanced data
                await GetRecentRequisitionsAsync(viewModel, userRequisitions);

                // Generate chart and trend data
                await GetChartAndTrendDataAsync(viewModel, userRequisitions);

                // Calculate quick stats
                await CalculateQuickStatsAsync(viewModel, userRequisitions);

                return viewModel;
            }
            catch (Exception ex)
            {
                // Log error (implement logging as needed)
                // Return basic dashboard on error
                return new MyRequisitionsDashboardViewModel
                {
                    ErrorMessage = "Unable to load dashboard data. Please try again."
                };
            }
        }

        /// <summary>
        /// Enhanced Department dashboard with role-based access
        /// </summary>
        public async Task<DepartmentDashboardViewModel> GetDepartmentDashboardAsync(HttpContext httpContext)
        {
            try
            {
                var userProfile = await _userProfileService.GetCurrentUserProfileAsync();

                if (userProfile?.BasicInfo?.PayrollNo == null)
                {
                    return new DepartmentDashboardViewModel();
                }

                // Check department access permissions
                if (!userProfile.VisibilityScope.CanAccessAcrossDepartments)
                {
                    return new DepartmentDashboardViewModel
                    {
                        ErrorMessage = "You don't have permission to access department dashboard."
                    };
                }

                var viewModel = new DepartmentDashboardViewModel
                {
                    DepartmentName = userProfile.LocationAccess.HomeDepartment.Name,
                    UserRole = userProfile.RoleInformation.SystemRole
                };

                // Get department requisitions based on user's access scope
                var departmentRequisitions = await GetDepartmentRequisitionsAsync(userProfile);

                // Calculate department metrics
                await CalculateDepartmentMetricsAsync(viewModel, departmentRequisitions);

                // Get recent department requisitions with employee details
                await GetRecentDepartmentRequisitionsAsync(viewModel, departmentRequisitions);

                return viewModel;
            }
            catch (Exception ex)
            {
                return new DepartmentDashboardViewModel
                {
                    ErrorMessage = "Unable to load department dashboard data."
                };
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Set user context information from UserProfile
        /// </summary>
        private async Task SetUserContextAsync(MyRequisitionsDashboardViewModel viewModel, UserProfile userProfile)
        {
            viewModel.UserInfo = new UserDashboardInfo
            {
                Name = userProfile.BasicInfo.Name,
                Department = userProfile.BasicInfo.Department,
                Station = userProfile.BasicInfo.Station,
                Role = userProfile.BasicInfo.Role,
                LastLogin = userProfile.CacheInfo.LastRefresh,
                PayrollNo = userProfile.BasicInfo.PayrollNo
            };

            viewModel.CanAccessDepartmentData = userProfile.VisibilityScope.CanAccessAcrossDepartments;
            viewModel.CanAccessStationData = userProfile.VisibilityScope.CanAccessAcrossStations;
            viewModel.PermissionLevel = userProfile.VisibilityScope.PermissionLevel;
        }

        /// <summary>
        /// Get requisitions filtered by user's access permissions
        /// </summary>
        private async Task<List<Requisition>> GetFilteredRequisitionsAsync(UserProfile userProfile)
        {
            var query = _context.Requisitions
                .Include(r => r.RequisitionItems)
                .AsQueryable();

            // Apply role-based filtering
            switch (userProfile.VisibilityScope.PermissionLevel)
            {
                case PermissionLevel.Default:
                    // Regular users: only their own requisitions
                    query = query.Where(r => r.PayrollNo == userProfile.BasicInfo.PayrollNo);
                    break;

                case PermissionLevel.Manager:
                    // Managers: department/station based access
                    if (userProfile.VisibilityScope.CanAccessAcrossDepartments)
                    {
                        query = query.Where(r =>
                            userProfile.LocationAccess.AccessibleDepartmentIds.Contains(r.DepartmentId) ||
                            r.PayrollNo == userProfile.BasicInfo.PayrollNo);
                    }
                    else if (userProfile.VisibilityScope.CanAccessAcrossStations)
                    {
                        query = query.Where(r =>
                            userProfile.LocationAccess.AccessibleStationIds.Contains(r.IssueStationId) ||
                            userProfile.LocationAccess.AccessibleStationIds.Contains(r.DeliveryStationId) ||
                            r.PayrollNo == userProfile.BasicInfo.PayrollNo);
                    }
                    else
                    {
                        query = query.Where(r => r.PayrollNo == userProfile.BasicInfo.PayrollNo);
                    }
                    break;

                case PermissionLevel.Administrator:
                    // Administrators: all requisitions (no filter)
                    break;
            }

            return await query
                .OrderByDescending(r => r.CreatedAt)
                .Take(500) // Reasonable limit for performance
                .ToListAsync();
        }

        /// <summary>
        /// Calculate primary dashboard metrics with real trend data
        /// </summary>
        private async Task CalculatePrimaryMetricsAsync(
            MyRequisitionsDashboardViewModel viewModel,
            List<Requisition> userRequisitions,
            string payrollNo)
        {
            var now = DateTime.UtcNow;
            var thisMonth = new DateTime(now.Year, now.Month, 1);
            var lastMonth = thisMonth.AddMonths(-1);
            var lastMonthEnd = thisMonth.AddDays(-1);

            // Current month requisitions
            var thisMonthRequisitions = userRequisitions
                .Where(r => r.CreatedAt >= thisMonth)
                .ToList();

            // Last month requisitions for trend calculation
            var lastMonthRequisitions = userRequisitions
                .Where(r => r.CreatedAt >= lastMonth && r.CreatedAt < thisMonth)
                .ToList();

            // Primary counts
            viewModel.TotalRequisitions = userRequisitions.Count;
            viewModel.ThisMonthRequisitions = thisMonthRequisitions.Count;

            viewModel.PendingRequisitions = userRequisitions.Count(r =>
                r.Status == RequisitionStatus.NotStarted ||
                r.Status == RequisitionStatus.PendingDispatch ||
                r.Status == RequisitionStatus.PendingReceipt);

            viewModel.CompletedRequisitions = userRequisitions.Count(r =>
                r.Status == RequisitionStatus.Completed);

            viewModel.CancelledRequisitions = userRequisitions.Count(r =>
                r.Status == RequisitionStatus.Cancelled);

            // Action required (items pending receipt confirmation by this user)
            viewModel.AwaitingMyAction = userRequisitions.Count(r =>
                r.Status == RequisitionStatus.PendingReceipt &&
                r.PayrollNo == payrollNo);

            // Overdue requisitions
            viewModel.OverdueRequisitions = userRequisitions.Count(r =>
                r.ExpectedDeliveryDate.HasValue &&
                r.ExpectedDeliveryDate < now &&
                r.Status != RequisitionStatus.Completed &&
                r.Status != RequisitionStatus.Cancelled);

            // Calculate real month-over-month trends
            viewModel.TotalRequisitionsTrend = CalculatePercentageChange(
                viewModel.TotalRequisitions,
                lastMonthRequisitions.Count);

            viewModel.ThisMonthTrend = CalculatePercentageChange(
                thisMonthRequisitions.Count,
                lastMonthRequisitions.Count);

            var thisMonthPending = thisMonthRequisitions.Count(r =>
                r.Status == RequisitionStatus.PendingDispatch ||
                r.Status == RequisitionStatus.PendingReceipt);

            var lastMonthPending = lastMonthRequisitions.Count(r =>
                r.Status == RequisitionStatus.PendingDispatch ||
                r.Status == RequisitionStatus.PendingReceipt);

            viewModel.PendingRequisitionsTrend = CalculatePercentageChange(
                thisMonthPending, lastMonthPending);

            var thisMonthCompleted = thisMonthRequisitions.Count(r =>
                r.Status == RequisitionStatus.Completed);

            var lastMonthCompleted = lastMonthRequisitions.Count(r =>
                r.Status == RequisitionStatus.Completed);

            viewModel.CompletedRequisitionsTrend = CalculatePercentageChange(
                thisMonthCompleted, lastMonthCompleted);

            // Average processing time calculation
            var completedRequisitions = userRequisitions
                .Where(r => r.Status == RequisitionStatus.Completed &&
                           r.CompletedAt.HasValue &&
                           r.CreatedAt >= DateTime.UtcNow.AddMonths(-6)) // Last 6 months only
                .ToList();

            if (completedRequisitions.Any())
            {
                viewModel.AverageProcessingDays = Math.Round(
                    completedRequisitions.Average(r =>
                        (r.CompletedAt.Value - r.CreatedAt).TotalDays), 1);
            }
        }

        /// <summary>
        /// Get action required items for the user
        /// </summary>
        private async Task GetActionRequiredItemsAsync(
            MyRequisitionsDashboardViewModel viewModel,
            List<Requisition> userRequisitions,
            string payrollNo)
        {
            var now = DateTime.UtcNow;

            viewModel.ActionRequired = new ActionRequiredSection
            {
                PendingReceiptConfirmation = userRequisitions.Count(r =>
                    r.Status == RequisitionStatus.PendingReceipt &&
                    r.PayrollNo == payrollNo),

                RequiringClarification = userRequisitions.Count(r =>
                    r.Status == RequisitionStatus.NotStarted &&
                    r.PayrollNo == payrollNo &&
                    r.CreatedAt < DateTime.UtcNow.AddDays(-3)), // Older than 3 days

                OverdueItems = userRequisitions.Count(r =>
                    r.ExpectedDeliveryDate.HasValue &&
                    r.ExpectedDeliveryDate < now &&
                    r.Status != RequisitionStatus.Completed &&
                    r.Status != RequisitionStatus.Cancelled),

                ReadyForCollection = userRequisitions.Count(r =>
                    r.Status == RequisitionStatus.PendingReceipt),

                UrgentRequisitions = userRequisitions
                    .Where(r => (r.Priority == "High" || r.IsUrgent == true) &&
                               r.Status != RequisitionStatus.Completed &&
                               r.Status != RequisitionStatus.Cancelled)
                    .Select(r => new UrgentRequisition
                    {
                        RequisitionId = r.Id,
                        Description = $"Requisition #{r.Id}",
                        DaysOverdue = r.ExpectedDeliveryDate.HasValue
                            ? Math.Max(0, (int)(now - r.ExpectedDeliveryDate.Value).TotalDays)
                            : 0,
                        ActionRequired = GetActionRequired(r.Status),
                        Priority = r.Priority ?? "Medium"
                    })
                    .OrderByDescending(u => u.DaysOverdue)
                    .Take(10)
                    .ToList()
            };
        }

        /// <summary>
        /// Get enhanced recent requisitions with location names
        /// </summary>
        private async Task GetRecentRequisitionsAsync(
            MyRequisitionsDashboardViewModel viewModel,
            List<Requisition> userRequisitions)
        {
            var recentRequisitions = userRequisitions
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .ToList();

            viewModel.RecentRequisitions = new List<RequisitionSummary>();

            foreach (var r in recentRequisitions)
            {
                // Resolve location names efficiently
                string issueLocationName = await _departmentService.GetLocationNameFromIdsAsync(
                    r.IssueStationId, r.IssueDepartmentId);

                string deliveryLocationName = await _departmentService.GetLocationNameFromIdsAsync(
                    r.DeliveryStationId, r.DeliveryDepartmentId);

                // Get employee name for display
                var employee = await _employeeService.GetEmployeeByPayrollAsync(r.PayrollNo);
                string employeeName = employee != null
                    ? $"{employee.SurName} {employee.OtherNames}"
                    : r.PayrollNo;

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
                    Priority = r.Priority,
                    ExpectedDeliveryDate = r.ExpectedDeliveryDate,
                    DaysInCurrentStatus = CalculateDaysInStatus(r),
                    IsOverdue = r.ExpectedDeliveryDate.HasValue &&
                               r.ExpectedDeliveryDate < DateTime.UtcNow &&
                               r.Status != RequisitionStatus.Completed
                });
            }
        }

        /// <summary>
        /// Generate chart and trend analysis data
        /// </summary>
        private async Task GetChartAndTrendDataAsync(
            MyRequisitionsDashboardViewModel viewModel,
            List<Requisition> userRequisitions)
        {
            // Status distribution for donut chart
            var statusCounts = userRequisitions
                .GroupBy(r => r.Status)
                .ToDictionary(
                    g => g.Key?.GetDescription() ?? "Unknown",
                    g => g.Count());

            viewModel.RequisitionStatusCounts = statusCounts;

            // Monthly trend data for line chart (last 12 months)
            var twelveMonthsAgo = DateTime.UtcNow.AddMonths(-12);
            var monthlyData = userRequisitions
                .Where(r => r.CreatedAt >= twelveMonthsAgo)
                .GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month })
                .Select(g => new MonthlyRequisitionData
                {
                    Month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM"),
                    Year = g.Key.Year,
                    Count = g.Count(),
                    Completed = g.Count(r => r.Status == RequisitionStatus.Completed),
                    Pending = g.Count(r => r.Status != RequisitionStatus.Completed &&
                                          r.Status != RequisitionStatus.Cancelled),
                    Cancelled = g.Count(r => r.Status == RequisitionStatus.Cancelled)
                })
                .OrderBy(d => d.Year).ThenBy(d => DateTime.ParseExact(d.Month, "MMM", null).Month)
                .ToList();

            // Top requested items
            var topItems = userRequisitions
                .SelectMany(r => r.RequisitionItems)
                .GroupBy(item => new { item.ItemCode, item.ItemName, item.Category })
                .Select(g => new TopRequestedItem
                {
                    ItemCode = g.Key.ItemCode,
                    ItemName = g.Key.ItemName,
                    Category = g.Key.Category,
                    RequestCount = g.Count(),
                    TotalQuantity = g.Sum(item => item.QuantityRequested)
                })
                .OrderByDescending(item => item.RequestCount)
                .Take(10)
                .ToList();

            // Category distribution
            var categoryData = userRequisitions
                .SelectMany(r => r.RequisitionItems)
                .GroupBy(item => item.Category ?? "Uncategorized")
                .ToDictionary(g => g.Key, g => g.Count());

            viewModel.TrendData = new TrendAnalysisData
            {
                RequisitionsByMonth = monthlyData,
                TopRequestedItems = topItems,
                RequisitionsByCategory = categoryData,
                StatusDistribution = statusCounts
            };
        }

        /// <summary>
        /// Calculate quick stats for CRM-style widget
        /// </summary>
        private async Task CalculateQuickStatsAsync(
            MyRequisitionsDashboardViewModel viewModel,
            List<Requisition> userRequisitions)
        {
            var now = DateTime.UtcNow;
            var thisMonth = new DateTime(now.Year, now.Month, 1);

            viewModel.QuickStats = new QuickStatsData
            {
                CompletedThisMonth = userRequisitions.Count(r =>
                    r.Status == RequisitionStatus.Completed && r.CreatedAt >= thisMonth),

                AverageItemsPerRequisition = userRequisitions.Any()
                    ? Math.Round(userRequisitions.Average(r => r.RequisitionItems?.Count() ?? 0), 1)
                    : 0,

                MostRequestedCategory = userRequisitions
                    .SelectMany(r => r.RequisitionItems)
                    .GroupBy(item => item.Category ?? "Uncategorized")
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?.Key ?? "N/A",

                FastestCompletionDays = userRequisitions
                    .Where(r => r.Status == RequisitionStatus.Completed && r.CompletedAt.HasValue)
                    .Min(r => (r.CompletedAt.Value - r.CreatedAt).TotalDays),

                TotalItemsRequested = userRequisitions
                    .SelectMany(r => r.RequisitionItems)
                    .Sum(item => item.QuantityRequested),

                ActiveRequisitions = userRequisitions.Count(r =>
                    r.Status != RequisitionStatus.Completed &&
                    r.Status != RequisitionStatus.Cancelled)
            };
        }

        /// <summary>
        /// Get department requisitions based on user access
        /// </summary>
        private async Task<List<Requisition>> GetDepartmentRequisitionsAsync(UserProfile userProfile)
        {
            var query = _context.Requisitions
                .Include(r => r.RequisitionItems)
                .AsQueryable();

            if (userProfile.VisibilityScope.CanAccessAcrossDepartments)
            {
                // Filter by accessible departments
                query = query.Where(r =>
                    userProfile.LocationAccess.AccessibleDepartmentIds.Contains(r.DepartmentId));
            }
            else
            {
                // Home department only
                query = query.Where(r =>
                    r.DepartmentId == userProfile.LocationAccess.HomeDepartment.Id);
            }

            return await query
                .OrderByDescending(r => r.CreatedAt)
                .Take(1000)
                .ToListAsync();
        }

        /// <summary>
        /// Calculate department-specific metrics
        /// </summary>
        private async Task CalculateDepartmentMetricsAsync(
            DepartmentDashboardViewModel viewModel,
            List<Requisition> departmentRequisitions)
        {
            viewModel.TotalDepartmentRequisitions = departmentRequisitions.Count;
            viewModel.PendingDepartmentRequisitions = departmentRequisitions.Count(r =>
                r.Status == RequisitionStatus.NotStarted ||
                r.Status == RequisitionStatus.PendingDispatch ||
                r.Status == RequisitionStatus.PendingReceipt);
            viewModel.CompletedDepartmentRequisitions = departmentRequisitions.Count(r =>
                r.Status == RequisitionStatus.Completed);
            viewModel.CancelledDepartmentRequisitions = departmentRequisitions.Count(r =>
                r.Status == RequisitionStatus.Cancelled);

            // Status distribution
            var statusCounts = departmentRequisitions
                .GroupBy(r => r.Status)
                .ToDictionary(
                    g => g.Key?.GetDescription() ?? "Unknown",
                    g => g.Count());

            viewModel.DepartmentRequisitionStatusCounts = statusCounts;
        }

        /// <summary>
        /// Get recent department requisitions with employee details
        /// </summary>
        private async Task GetRecentDepartmentRequisitionsAsync(
            DepartmentDashboardViewModel viewModel,
            List<Requisition> departmentRequisitions)
        {
            var recentRequisitions = departmentRequisitions
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .ToList();

            viewModel.RecentDepartmentRequisitions = new List<RequisitionSummary>();

            foreach (var r in recentRequisitions)
            {
                string issueLocationName = await _departmentService.GetLocationNameFromIdsAsync(
                    r.IssueStationId, r.IssueDepartmentId);

                string deliveryLocationName = await _departmentService.GetLocationNameFromIdsAsync(
                    r.DeliveryStationId, r.DeliveryDepartmentId);

                var employee = await _employeeService.GetEmployeeByPayrollAsync(r.PayrollNo);
                string employeeName = employee != null
                    ? $"{employee.SurName} {employee.OtherNames}"
                    : r.PayrollNo;

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
                    PayrollNo = r.PayrollNo,
                    EmployeeName = employeeName
                });
            }
        }

        /// <summary>
        /// Calculate percentage change between two values
        /// </summary>
        private decimal CalculatePercentageChange(int current, int previous)
        {
            if (previous == 0)
                return current > 0 ? 100 : 0;

            return Math.Round(((decimal)(current - previous) / previous) * 100, 2);
        }

        /// <summary>
        /// Get action required description based on status
        /// </summary>
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

        /// <summary>
        /// Calculate days in current status
        /// </summary>
        private int CalculateDaysInStatus(Requisition requisition)
        {
            var statusChangeDate = requisition.LastStatusChangeDate ?? requisition.CreatedAt;
            return (int)(DateTime.UtcNow - statusChangeDate).TotalDays;
        }

        #endregion
    }

    #region Enhanced ViewModels

    /// <summary>
    /// Enhanced MyRequisitions Dashboard ViewModel
    /// </summary>
    public class MyRequisitionsDashboardViewModel
    {
        // User Information
        public UserDashboardInfo UserInfo { get; set; } = new();

        // Primary Metrics (existing enhanced)
        public int TotalRequisitions { get; set; }
        public int PendingRequisitions { get; set; }
        public int CompletedRequisitions { get; set; }
        public int CancelledRequisitions { get; set; }

        // New Primary Metrics
        public int AwaitingMyAction { get; set; }
        public int ThisMonthRequisitions { get; set; }
        public double AverageProcessingDays { get; set; }
        public int OverdueRequisitions { get; set; }

        // Trend Data (real calculations)
        public decimal TotalRequisitionsTrend { get; set; }
        public decimal PendingRequisitionsTrend { get; set; }
        public decimal CompletedRequisitionsTrend { get; set; }
        public decimal ThisMonthTrend { get; set; }

        // Existing Collections (enhanced)
        public Dictionary<string, int> RequisitionStatusCounts { get; set; } = new();
        public List<RequisitionSummary> RecentRequisitions { get; set; } = new();

        // New Collections
        public ActionRequiredSection ActionRequired { get; set; } = new();
        public TrendAnalysisData TrendData { get; set; } = new();
        public QuickStatsData QuickStats { get; set; } = new();

        // Permission Context
        public bool CanAccessDepartmentData { get; set; }
        public bool CanAccessStationData { get; set; }
        public PermissionLevel PermissionLevel { get; set; }

        // Error Handling
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Enhanced Department Dashboard ViewModel
    /// </summary>
    public class DepartmentDashboardViewModel
    {
        public string DepartmentName { get; set; }
        public string UserRole { get; set; }

        // Department metrics
        public int TotalDepartmentRequisitions { get; set; }
        public int PendingDepartmentRequisitions { get; set; }
        public int CompletedDepartmentRequisitions { get; set; }
        public int CancelledDepartmentRequisitions { get; set; }

        // Status distribution for chart
        public Dictionary<string, int> DepartmentRequisitionStatusCounts { get; set; } = new();

        // Recent department requisitions
        public List<RequisitionSummary> RecentDepartmentRequisitions { get; set; } = new();

        // Error handling
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Enhanced Requisition Summary with additional fields
    /// </summary>
    public class RequisitionSummary
    {
        public int Id { get; set; }
        public string IssueStation { get; set; }
        public string DeliveryStation { get; set; }
        public int IssueStationId { get; set; }
        public string IssueDepartmentId { get; set; }
        public int DeliveryStationId { get; set; }
        public string DeliveryDepartmentId { get; set; }
        public RequisitionStatus Status { get; set; }
        public string StatusDescription { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int ItemCount { get; set; }
        public string PayrollNo { get; set; }
        public string EmployeeName { get; set; }

        // New fields
        public string Priority { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public int DaysInCurrentStatus { get; set; }
        public bool IsOverdue { get; set; }
    }

    /// <summary>
    /// User dashboard context information
    /// </summary>
    public class UserDashboardInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Station { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string PayrollNo { get; set; } = string.Empty;
        public DateTime LastLogin { get; set; }
    }

    /// <summary>
    /// Action required section data
    /// </summary>
    public class ActionRequiredSection
    {
        public int PendingReceiptConfirmation { get; set; }
        public int RequiringClarification { get; set; }
        public int OverdueItems { get; set; }
        public int ReadyForCollection { get; set; }
        public List<UrgentRequisition> UrgentRequisitions { get; set; } = new();
    }

    /// <summary>
    /// Urgent requisition information
    /// </summary>
    public class UrgentRequisition
    {
        public int RequisitionId { get; set; }
        public string Description { get; set; } = string.Empty;
        public int DaysOverdue { get; set; }
        public string ActionRequired { get; set; } = string.Empty;
        public string Priority { get; set; } = "Medium";
    }

    /// <summary>
    /// Trend analysis data for charts
    /// </summary>
    public class TrendAnalysisData
    {
        public List<MonthlyRequisitionData> RequisitionsByMonth { get; set; } = new();
        public Dictionary<string, int> RequisitionsByCategory { get; set; } = new();
        public List<TopRequestedItem> TopRequestedItems { get; set; } = new();
        public Dictionary<string, int> StatusDistribution { get; set; } = new();
    }

    /// <summary>
    /// Monthly requisition trend data
    /// </summary>
    public class MonthlyRequisitionData
    {
        public string Month { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Count { get; set; }
        public int Completed { get; set; }
        public int Pending { get; set; }
        public int Cancelled { get; set; }
    }

    /// <summary>
    /// Top requested items data
    /// </summary>
    public class TopRequestedItem
    {
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int RequestCount { get; set; }
        public int TotalQuantity { get; set; }
    }

    /// <summary>
    /// Quick stats for CRM-style widget
    /// </summary>
    public class QuickStatsData
    {
        public int CompletedThisMonth { get; set; }
        public double AverageItemsPerRequisition { get; set; }
        public string MostRequestedCategory { get; set; } = string.Empty;
        public double FastestCompletionDays { get; set; }
        public int TotalItemsRequested { get; set; }
        public int ActiveRequisitions { get; set; }
    }

    #endregion
}