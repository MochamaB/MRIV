using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MRIV.Enums;
using MRIV.Extensions;
using MRIV.Models;
using MRIV.Models.Views;
using MRIV.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MRIV.Services
{
    public interface IDashboardService
    {
        // Existing personal dashboard methods
        Task<MyRequisitionsDashboardViewModel> GetMyRequisitionsDashboardAsync(HttpContext httpContext);
        Task<DepartmentDashboardViewModel> GetDepartmentDashboardAsync(HttpContext httpContext);

        // NEW: Management Dashboard method
        Task<ManagementDashboardViewModel> GetManagementDashboardAsync(HttpContext httpContext);

        // NEW: Material Dashboard methods
        Task<MaterialDashboardViewModel> GetMaterialDashboardAsync(HttpContext httpContext);

        // Enhanced methods for data components
        Task<TrendAnalysisData> GetTrendDataAsync(string payrollNo);
        Task<ActionRequiredSection> GetActionRequiredAsync(string payrollNo);
        Task<QuickStatsData> GetQuickStatsAsync(string payrollNo);
    }
    public class DashboardService : IDashboardService
    {
        private readonly RequisitionContext _context;
        private readonly IDepartmentService _departmentService;
        private readonly IUserProfileService _userProfileService;
        private readonly IVisibilityAuthorizeService _visibilityService;
        private readonly IMaterialBusinessLogicService _materialBusinessLogicService;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            RequisitionContext context,
            IDepartmentService departmentService,
            IUserProfileService userProfileService,
            IVisibilityAuthorizeService visibilityService,
            IMaterialBusinessLogicService materialBusinessLogicService,
            ILogger<DashboardService> logger)
        {
            _context = context;
            _departmentService = departmentService;
            _userProfileService = userProfileService;
            _visibilityService = visibilityService;
            _materialBusinessLogicService = materialBusinessLogicService;
            _logger = logger;
        }

        public async Task<MyRequisitionsDashboardViewModel> GetMyRequisitionsDashboardAsync(HttpContext httpContext)
        {
            try
            {
                // Get UserProfile - this is our primary data source
                var userProfile = await _userProfileService.GetCurrentUserProfileAsync();

                if (userProfile?.BasicInfo?.PayrollNo == null)
                {
                    // Try to rebuild profile from session if missing
                    var sessionPayrollNo = httpContext.Session.GetString("EmployeePayrollNo");
                    if (!string.IsNullOrEmpty(sessionPayrollNo))
                    {
                        userProfile = await _userProfileService.BuildUserProfileAsync(sessionPayrollNo);
                    }

                    if (userProfile?.BasicInfo?.PayrollNo == null)
                    {
                        _logger.LogWarning("Unable to load user profile for dashboard");
                        return new MyRequisitionsDashboardViewModel
                        {
                            ErrorMessage = "Unable to load user profile. Please log in again."
                        };
                    }
                }

                var payrollNo = userProfile.BasicInfo.PayrollNo;
                _logger.LogInformation("Loading dashboard for user: {PayrollNo}", payrollNo);

                // Create enhanced dashboard view model
                var viewModel = new MyRequisitionsDashboardViewModel();

                // Set user context from UserProfile (single source of truth)
                _logger.LogInformation("UserProfile BasicInfo - PayrollNo: {PayrollNo}, Fullname: '{Fullname}', Role: '{Role}'",
                    userProfile.BasicInfo.PayrollNo, userProfile.BasicInfo.Fullname, userProfile.BasicInfo.Role);

                viewModel.UserInfo = new UserDashboardInfo
                {
                    Name = !string.IsNullOrEmpty(userProfile.BasicInfo.Fullname) ? userProfile.BasicInfo.Fullname : userProfile.BasicInfo.PayrollNo,
                    Department = userProfile.LocationAccess?.HomeDepartment?.Name ?? "Unknown Department",
                    Station = userProfile.LocationAccess?.HomeStation?.Name ?? "Unknown Station",
                    Role = userProfile.BasicInfo.Role ?? "Unknown Role",
                    PayrollNo = userProfile.BasicInfo.PayrollNo,
                    LastLogin = userProfile.CacheInfo.LastRefresh
                };
                viewModel.CanAccessDepartmentData = userProfile.VisibilityScope.CanAccessAcrossDepartments;
                viewModel.CanAccessStationData = userProfile.VisibilityScope.CanAccessAcrossStations;

                // Get all requisitions and apply visibility scope using enhanced method
                var allRequisitions = _context.Requisitions
                    .Include(r => r.RequisitionItems)
                    .AsQueryable();

                // Apply visibility filtering using enhanced UserProfile-based method
                var visibleRequisitions = _visibilityService.ApplyVisibilityScopeWithProfile(allRequisitions, userProfile);

                var userRequisitions = await visibleRequisitions
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
            try
            {
                // Get UserProfile for department context
                var userProfile = await _userProfileService.GetCurrentUserProfileAsync();

                if (userProfile?.LocationAccess?.HomeDepartment == null)
                {
                    _logger.LogWarning("Unable to load user profile or department for department dashboard");
                    return new DepartmentDashboardViewModel
                    {
                        DepartmentName = "Unknown Department"
                    };
                }

                // Verify user has department access
                if (!userProfile.VisibilityScope.CanAccessAcrossDepartments &&
                    !userProfile.RoleInformation.IsAdmin)
                {
                    _logger.LogWarning("User {PayrollNo} attempted to access department dashboard without permission",
                        userProfile.BasicInfo.PayrollNo);
                    return new DepartmentDashboardViewModel
                    {
                        DepartmentName = userProfile.LocationAccess.HomeDepartment.Name
                    };
                }

                // Create dashboard view model using UserProfile data
                var viewModel = new DepartmentDashboardViewModel
                {
                    DepartmentName = userProfile.LocationAccess.HomeDepartment.Name
                };

                // Get all requisitions and apply visibility scope
                var allRequisitions = _context.Requisitions
                    .Include(r => r.RequisitionItems)
                    .AsQueryable();

                // Apply visibility filtering based on user's access level
                var visibleRequisitions = _visibilityService.ApplyVisibilityScopeWithProfile(allRequisitions, userProfile);

                var departmentRequisitions = await visibleRequisitions
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
                // Resolve location names using department service
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

            // Get employee names using the new EmployeeDetailsView method
            var payrollNumbers = viewModel.RecentDepartmentRequisitions.Select(r => r.PayrollNo).Distinct().ToList();
            var employeeNames = await GetEmployeeNamesAsync(payrollNumbers);

            foreach (var requisition in viewModel.RecentDepartmentRequisitions)
            {
                requisition.EmployeeName = employeeNames.ContainsKey(requisition.PayrollNo)
                    ? employeeNames[requisition.PayrollNo]
                    : requisition.PayrollNo;
            }

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading department dashboard");
                return new DepartmentDashboardViewModel
                {
                    DepartmentName = "Error Loading Department"
                };
            }
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

                // Get employee name using the new EmployeeDetailsView method
                string employeeName = await GetEmployeeNameAsync(r.PayrollNo);

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

        /// <summary>
        /// Get employee name from the EmployeeDetailsView (replacing EmployeeService functionality)
        /// </summary>
        private async Task<string> GetEmployeeNameAsync(string payrollNo)
        {
            try
            {
                var employeeDetails = await _context.EmployeeDetailsViews
                    .Where(e => e.PayrollNo == payrollNo && e.IsActive == 0)
                    .Select(e => new { e.Fullname, e.SurName, e.OtherNames })
                    .FirstOrDefaultAsync();

                if (employeeDetails != null)
                {
                    return !string.IsNullOrEmpty(employeeDetails.Fullname)
                        ? employeeDetails.Fullname
                        : $"{employeeDetails.SurName} {employeeDetails.OtherNames}".Trim();
                }

                return payrollNo; // Fallback to PayrollNo if not found
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting employee name for PayrollNo: {PayrollNo}", payrollNo);
                return payrollNo; // Fallback to PayrollNo on error
            }
        }

        /// <summary>
        /// Get multiple employee names in a single query for better performance
        /// </summary>
        private async Task<Dictionary<string, string>> GetEmployeeNamesAsync(IEnumerable<string> payrollNumbers)
        {
            try
            {
                var employees = await _context.EmployeeDetailsViews
                    .Where(e => payrollNumbers.Contains(e.PayrollNo) && e.IsActive == 0)
                    .Select(e => new { e.PayrollNo, e.Fullname, e.SurName, e.OtherNames })
                    .ToListAsync();

                return employees.ToDictionary(
                    e => e.PayrollNo,
                    e => !string.IsNullOrEmpty(e.Fullname)
                        ? e.Fullname
                        : $"{e.SurName} {e.OtherNames}".Trim()
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting employee names for multiple PayrollNos");
                return new Dictionary<string, string>();
            }
        }

        // ===== MANAGEMENT DASHBOARD IMPLEMENTATION =====

        /// <summary>
        /// Get Management Dashboard based on user's role and permissions
        /// Separate from personal "My Dashboard" - shows organizational data
        /// </summary>
        public async Task<ManagementDashboardViewModel> GetManagementDashboardAsync(HttpContext httpContext)
        {
            try
            {
                var userProfile = await _userProfileService.GetCurrentUserProfileAsync();

                if (userProfile == null)
                {
                    _logger.LogError("UserProfile is null in GetManagementDashboardAsync");
                    return new ManagementDashboardViewModel
                    {
                        ErrorMessage = "Unable to load user profile",
                        IsDefaultUser = true
                    };
                }

                _logger.LogInformation("Management Dashboard access attempt - PayrollNo: {PayrollNo}, IsAdmin: {IsAdmin}, IsDefaultUser: {IsDefaultUser}, Role: '{Role}'",
                    userProfile.BasicInfo?.PayrollNo,
                    userProfile.RoleInformation?.IsAdmin,
                    userProfile.VisibilityScope?.IsDefaultUser,
                    userProfile.RoleInformation?.SystemRole);

                // Admin bypass - check this FIRST before any other checks
                if (userProfile.RoleInformation.IsAdmin)
                {
                    _logger.LogInformation("Admin access granted to Management Dashboard - PayrollNo: {PayrollNo}", userProfile.BasicInfo.PayrollNo);
                    return await BuildOrganizationDashboard(userProfile);
                }

                // Default users cannot access Management Dashboard
                if (userProfile.VisibilityScope.IsDefaultUser)
                {
                    _logger.LogWarning("Default user {PayrollNo} attempted to access Management Dashboard", userProfile.BasicInfo.PayrollNo);
                    return new ManagementDashboardViewModel
                    {
                        IsDefaultUser = true,
                        DashboardTitle = "Access Denied",
                        DashboardContext = "Default users can only access personal dashboard",
                        ErrorMessage = "You don't have permission to access the Management Dashboard"
                    };
                }

                // Role-based dashboard building using fixed VisibilityService logic
                if (userProfile.VisibilityScope.CanAccessAcrossStations && userProfile.VisibilityScope.CanAccessAcrossDepartments)
                {
                    return await BuildOrganizationDashboard(userProfile);
                }
                else if (userProfile.VisibilityScope.CanAccessAcrossStations)
                {
                    return await BuildCrossStationDepartmentDashboard(userProfile);
                }
                else if (userProfile.VisibilityScope.CanAccessAcrossDepartments)
                {
                    return await BuildStationDashboard(userProfile);
                }
                else
                {
                    return await BuildDepartmentDashboard(userProfile);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Management Dashboard");
                return new ManagementDashboardViewModel
                {
                    ErrorMessage = "Unable to load management dashboard data",
                    DashboardTitle = "Error",
                    DashboardContext = "System Error"
                };
            }
        }

        /// <summary>
        /// Build dashboard for Department Managers (own department at own station)
        /// </summary>
        private async Task<ManagementDashboardViewModel> BuildDepartmentDashboard(UserProfile userProfile)
        {
            var dashboard = new ManagementDashboardViewModel
            {
                UserInfo = BuildUserDashboardInfo(userProfile),
                DashboardTitle = $"{userProfile.BasicInfo.Department} Department Management",
                DashboardContext = $"Managing {userProfile.BasicInfo.Department} at {userProfile.BasicInfo.Station}",
                AccessLevel = "Department",
                IsDefaultUser = false
            };

            // Get all requisitions using VisibilityService filtering
            var allRequisitions = _context.Requisitions.Include(r => r.RequisitionItems).AsQueryable();
            var visibleRequisitions = _visibilityService.ApplyVisibilityScopeWithProfile(allRequisitions, userProfile).ToList();

            // Build scope metrics
            dashboard.PrimaryMetrics = BuildScopeMetrics(visibleRequisitions);

            // Build status distribution
            dashboard.StatusDistribution = BuildStatusDistribution(visibleRequisitions);

            // Build trend analysis
            dashboard.TrendAnalysis = BuildTrendAnalysis(visibleRequisitions);

            // No comparison data for department managers (single entity view)
            dashboard.ComparisonData = new List<ComparisonEntity>();

            // Build material data (placeholder for now)
            dashboard.MaterialData = await BuildMaterialScopeData(userProfile);

            // Build action items
            dashboard.ActionRequired = await BuildManagementActionItems(userProfile, visibleRequisitions);

            // Build recent activity
            dashboard.RecentActivity = await BuildRecentOrganizationalActivity(visibleRequisitions, "Department");

            return dashboard;
        }

        /// <summary>
        /// Build dashboard for Station Managers (all departments at own station)
        /// </summary>
        private async Task<ManagementDashboardViewModel> BuildStationDashboard(UserProfile userProfile)
        {
            var dashboard = new ManagementDashboardViewModel
            {
                UserInfo = BuildUserDashboardInfo(userProfile),
                DashboardTitle = $"{userProfile.BasicInfo.Station} Station Management",
                DashboardContext = $"Managing all departments at {userProfile.BasicInfo.Station}",
                AccessLevel = "Station",
                IsDefaultUser = false
            };

            // Get all requisitions using VisibilityService filtering
            var allRequisitions = _context.Requisitions.Include(r => r.RequisitionItems).AsQueryable();
            var visibleRequisitions = _visibilityService.ApplyVisibilityScopeWithProfile(allRequisitions, userProfile).ToList();

            // Build scope metrics
            dashboard.PrimaryMetrics = BuildScopeMetrics(visibleRequisitions);

            // Build status distribution
            dashboard.StatusDistribution = BuildStatusDistribution(visibleRequisitions);

            // Build trend analysis
            dashboard.TrendAnalysis = BuildTrendAnalysis(visibleRequisitions);

            // Build department comparison data
            dashboard.ComparisonData = await BuildDepartmentComparison(visibleRequisitions, userProfile);

            // Build material data
            dashboard.MaterialData = await BuildMaterialScopeData(userProfile);

            // Build action items
            dashboard.ActionRequired = await BuildManagementActionItems(userProfile, visibleRequisitions);

            // Build recent activity
            dashboard.RecentActivity = await BuildRecentOrganizationalActivity(visibleRequisitions, "Station");

            return dashboard;
        }

        /// <summary>
        /// Build dashboard for General Managers (own department at all stations)
        /// </summary>
        private async Task<ManagementDashboardViewModel> BuildCrossStationDepartmentDashboard(UserProfile userProfile)
        {
            var dashboard = new ManagementDashboardViewModel
            {
                UserInfo = BuildUserDashboardInfo(userProfile),
                DashboardTitle = $"{userProfile.BasicInfo.Department} Cross-Station Management",
                DashboardContext = $"Managing {userProfile.BasicInfo.Department} across all stations",
                AccessLevel = "Cross-Station",
                IsDefaultUser = false
            };

            // Get all requisitions using VisibilityService filtering
            var allRequisitions = _context.Requisitions.Include(r => r.RequisitionItems).AsQueryable();
            var visibleRequisitions = _visibilityService.ApplyVisibilityScopeWithProfile(allRequisitions, userProfile).ToList();

            // Build scope metrics
            dashboard.PrimaryMetrics = BuildScopeMetrics(visibleRequisitions);

            // Build status distribution
            dashboard.StatusDistribution = BuildStatusDistribution(visibleRequisitions);

            // Build trend analysis
            dashboard.TrendAnalysis = BuildTrendAnalysis(visibleRequisitions);

            // Build station comparison data
            dashboard.ComparisonData = await BuildStationComparison(visibleRequisitions, userProfile);

            // Build material data
            dashboard.MaterialData = await BuildMaterialScopeData(userProfile);

            // Build action items
            dashboard.ActionRequired = await BuildManagementActionItems(userProfile, visibleRequisitions);

            // Build recent activity
            dashboard.RecentActivity = await BuildRecentOrganizationalActivity(visibleRequisitions, "Cross-Station");

            return dashboard;
        }

        /// <summary>
        /// Build dashboard for Administrators (all data everywhere)
        /// </summary>
        private async Task<ManagementDashboardViewModel> BuildOrganizationDashboard(UserProfile userProfile)
        {
            var dashboard = new ManagementDashboardViewModel
            {
                UserInfo = BuildUserDashboardInfo(userProfile),
                DashboardTitle = "Organization Management Dashboard",
                DashboardContext = "Managing entire organization",
                AccessLevel = "Organization",
                IsDefaultUser = false
            };

            // Get all requisitions using VisibilityService filtering (admin sees all)
            var allRequisitions = _context.Requisitions.Include(r => r.RequisitionItems).AsQueryable();
            var visibleRequisitions = _visibilityService.ApplyVisibilityScopeWithProfile(allRequisitions, userProfile).ToList();

            // Build scope metrics
            dashboard.PrimaryMetrics = BuildScopeMetrics(visibleRequisitions);

            // Build status distribution
            dashboard.StatusDistribution = BuildStatusDistribution(visibleRequisitions);

            // Build trend analysis
            dashboard.TrendAnalysis = BuildTrendAnalysis(visibleRequisitions);

            // Build comprehensive comparison data (departments + stations)
            dashboard.ComparisonData = await BuildOrganizationComparison(visibleRequisitions, userProfile);

            // Build material data
            dashboard.MaterialData = await BuildMaterialScopeData(userProfile);

            // Build action items
            dashboard.ActionRequired = await BuildManagementActionItems(userProfile, visibleRequisitions);

            // Build recent activity
            dashboard.RecentActivity = await BuildRecentOrganizationalActivity(visibleRequisitions, "Organization");

            return dashboard;
        }

        // ===== HELPER METHODS FOR MANAGEMENT DASHBOARD =====

        private UserDashboardInfo BuildUserDashboardInfo(UserProfile userProfile)
        {
            return new UserDashboardInfo
            {
                Name = userProfile.BasicInfo.Fullname,
                Department = userProfile.BasicInfo.Department,
                Station = userProfile.BasicInfo.Station,
                Role = userProfile.BasicInfo.Role,
                PayrollNo = userProfile.BasicInfo.PayrollNo,
                LastLogin = DateTime.UtcNow // Placeholder
            };
        }

        private ScopeMetrics BuildScopeMetrics(List<Requisition> requisitions)
        {
            var thisMonth = DateTime.UtcNow.Month;
            var thisYear = DateTime.UtcNow.Year;

            return new ScopeMetrics
            {
                TotalRequisitions = requisitions.Count,
                PendingActions = requisitions.Count(r => r.Status == RequisitionStatus.PendingDispatch || r.Status == RequisitionStatus.PendingReceipt),
                ThisMonthActivity = requisitions.Count(r => r.CreatedAt.HasValue && r.CreatedAt.Value.Month == thisMonth && r.CreatedAt.Value.Year == thisYear),
                CompletionRate = requisitions.Count > 0 ? (decimal)requisitions.Count(r => r.Status == RequisitionStatus.Completed) / requisitions.Count * 100 : 0,
                AverageProcessingTime = CalculateAverageProcessingTime(requisitions),
                OverdueItems = requisitions.Count(r => IsOverdue(r))
            };
        }

        private Dictionary<string, int> BuildStatusDistribution(List<Requisition> requisitions)
        {
            return requisitions
                .GroupBy(r => r.Status?.GetDescription() ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count());
        }

        private List<TrendDataPoint> BuildTrendAnalysis(List<Requisition> requisitions)
        {
            var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
            return requisitions
                .Where(r => r.CreatedAt.HasValue && r.CreatedAt >= sixMonthsAgo)
                .GroupBy(r => new { r.CreatedAt.Value.Year, r.CreatedAt.Value.Month })
                .Select(g => new TrendDataPoint
                {
                    Period = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    Value = g.Count(),
                    Label = $"{g.Count()} requisitions",
                    SubValues = new Dictionary<string, int>
                    {
                        { "Completed", g.Count(r => r.Status == RequisitionStatus.Completed) },
                        { "Pending", g.Count(r => r.Status != RequisitionStatus.Completed && r.Status != RequisitionStatus.Cancelled) }
                    }
                })
                .OrderBy(t => DateTime.ParseExact(t.Period, "MMM yyyy", null))
                .ToList();
        }

        private async Task<List<ComparisonEntity>> BuildDepartmentComparison(List<Requisition> requisitions, UserProfile userProfile)
        {
            // Group requisitions by department and create comparison
            var departmentGroups = requisitions.GroupBy(r => r.DepartmentId).ToList();
            var comparison = new List<ComparisonEntity>();

            foreach (var group in departmentGroups.Take(10)) // Limit to top 10
            {
                var deptRequisitions = group.ToList();
                var departmentName = await GetDepartmentNameById(group.Key);

                comparison.Add(new ComparisonEntity
                {
                    Name = departmentName,
                    Type = "Department",
                    Metrics = BuildScopeMetrics(deptRequisitions),
                    PerformanceIndicator = GetPerformanceIndicator(BuildScopeMetrics(deptRequisitions).CompletionRate),
                    PerformanceColor = GetPerformanceColor(BuildScopeMetrics(deptRequisitions).CompletionRate)
                });
            }

            return comparison.OrderByDescending(c => c.Metrics.CompletionRate).ToList();
        }

        private async Task<List<ComparisonEntity>> BuildStationComparison(List<Requisition> requisitions, UserProfile userProfile)
        {
            // Group requisitions by station and create comparison
            var stationGroups = requisitions.GroupBy(r => r.IssueStationId).ToList();
            var comparison = new List<ComparisonEntity>();

            foreach (var group in stationGroups.Take(10)) // Limit to top 10
            {
                var stationRequisitions = group.ToList();
                var stationName = await GetStationNameByIdAsync(group.Key);

                comparison.Add(new ComparisonEntity
                {
                    Name = stationName,
                    Type = "Station",
                    Metrics = BuildScopeMetrics(stationRequisitions),
                    PerformanceIndicator = GetPerformanceIndicator(BuildScopeMetrics(stationRequisitions).CompletionRate),
                    PerformanceColor = GetPerformanceColor(BuildScopeMetrics(stationRequisitions).CompletionRate)
                });
            }

            return comparison.OrderByDescending(c => c.Metrics.CompletionRate).ToList();
        }

        private async Task<List<ComparisonEntity>> BuildOrganizationComparison(List<Requisition> requisitions, UserProfile userProfile)
        {
            // Combine department and station comparisons for organization view
            var departmentComparison = await BuildDepartmentComparison(requisitions, userProfile);
            var stationComparison = await BuildStationComparison(requisitions, userProfile);

            var combined = new List<ComparisonEntity>();
            combined.AddRange(departmentComparison.Take(5)); // Top 5 departments
            combined.AddRange(stationComparison.Take(5)); // Top 5 stations

            return combined.OrderByDescending(c => c.Metrics.CompletionRate).ToList();
        }

        private async Task<MaterialScopeData> BuildMaterialScopeData(UserProfile userProfile)
        {
            try
            {
                // Get materials based on user's visibility scope
                var materialsQuery = _context.Materials.Include(m => m.MaterialCategory).AsQueryable();

                // Apply visibility filtering through VisibilityService if needed
                // For now, admin sees all materials, others see materials in their scope
                if (!userProfile.RoleInformation.IsAdmin)
                {
                    // Apply location-based filtering for non-admin users
                    // This would need to be implemented based on material location/assignment logic
                    // For now, we'll show all materials for simplicity
                }

                var materials = await materialsQuery.ToListAsync();

                var materialData = new MaterialScopeData
                {
                    TotalMaterials = materials.Count,
                    TotalValue = materials.Sum(m => m.PurchasePrice ?? 0),
                    MaterialByCategory = materials
                        .Where(m => m.MaterialCategory != null)
                        .GroupBy(m => m.MaterialCategory.Name)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    MaterialByCondition = materials
                        .GroupBy(m => m.Status?.ToString() ?? "Unknown")
                        .ToDictionary(g => g.Key, g => g.Count()),
                    MaterialAlerts = await BuildMaterialAlerts(materials)
                };

                _logger.LogInformation("Built MaterialScopeData - TotalMaterials: {Count}, TotalValue: {Value}",
                    materialData.TotalMaterials, materialData.TotalValue);

                return materialData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building material scope data for user {PayrollNo}", userProfile.BasicInfo.PayrollNo);

                // Return empty data on error
                return new MaterialScopeData
                {
                    TotalMaterials = 0,
                    TotalValue = 0,
                    MaterialByCategory = new Dictionary<string, int>(),
                    MaterialByCondition = new Dictionary<string, int>(),
                    MaterialAlerts = new List<MaterialAlert>()
                };
            }
        }

        private async Task<List<MaterialAlert>> BuildMaterialAlerts(List<Material> materials)
        {
            var alerts = new List<MaterialAlert>();

            try
            {
                // Warranty expiring soon (within 30 days)
                var warrantyExpiring = materials
                    .Where(m => m.WarrantyEndDate.HasValue &&
                               m.WarrantyEndDate.Value <= DateTime.UtcNow.AddDays(30) &&
                               m.WarrantyEndDate.Value > DateTime.UtcNow)
                    .Count();

                if (warrantyExpiring > 0)
                {
                    alerts.Add(new MaterialAlert
                    {
                        Type = "Warranty Expiring",
                        Count = warrantyExpiring,
                        Severity = "Warning",
                        Message = $"{warrantyExpiring} materials have warranties expiring within 30 days"
                    });
                }

                // Expired warranties
                var warrantyExpired = materials
                    .Where(m => m.WarrantyEndDate.HasValue && m.WarrantyEndDate.Value < DateTime.UtcNow)
                    .Count();

                if (warrantyExpired > 0)
                {
                    alerts.Add(new MaterialAlert
                    {
                        Type = "Warranty Expired",
                        Count = warrantyExpired,
                        Severity = "Critical",
                        Message = $"{warrantyExpired} materials have expired warranties"
                    });
                }

                // Materials without purchase price (data quality issue)
                var noPriceCount = materials.Where(m => !m.PurchasePrice.HasValue || m.PurchasePrice <= 0).Count();
                if (noPriceCount > 0)
                {
                    alerts.Add(new MaterialAlert
                    {
                        Type = "Missing Price Data",
                        Count = noPriceCount,
                        Severity = "Info",
                        Message = $"{noPriceCount} materials are missing purchase price information"
                    });
                }

                return alerts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building material alerts");
                return new List<MaterialAlert>();
            }
        }

        private async Task<List<ManagementActionItem>> BuildManagementActionItems(UserProfile userProfile, List<Requisition> requisitions)
        {
            var actionItems = new List<ManagementActionItem>();

            // Pending approvals
            var pendingApprovals = requisitions.Count(r => r.Status == RequisitionStatus.PendingDispatch);
            if (pendingApprovals > 0)
            {
                actionItems.Add(new ManagementActionItem
                {
                    Type = "Approval",
                    Title = "Pending Approvals",
                    Description = $"{pendingApprovals} requisitions awaiting approval",
                    Urgency = pendingApprovals > 10 ? "High" : "Medium",
                    Priority = pendingApprovals > 10 ? "High" : "Medium",
                    Count = pendingApprovals,
                    ActionUrl = "/Approvals",
                    DueDate = DateTime.UtcNow.AddDays(2),
                    CreatedAt = DateTime.UtcNow,
                    EntityName = "Organization"
                });
            }

            // Overdue items
            var overdueItems = requisitions.Count(r => IsOverdue(r));
            if (overdueItems > 0)
            {
                actionItems.Add(new ManagementActionItem
                {
                    Type = "Review",
                    Title = "Overdue Items",
                    Description = $"{overdueItems} requisitions are overdue",
                    Urgency = "Critical",
                    Priority = "High",
                    Count = overdueItems,
                    ActionUrl = "/Requisitions?status=overdue",
                    DueDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    EntityName = "Organization"
                });
            }

            return actionItems;
        }

        private async Task<List<RecentOrganizationalActivity>> BuildRecentOrganizationalActivity(List<Requisition> requisitions, string scope)
        {
            var recentRequisitions = requisitions
                .Where(r => r.CreatedAt.HasValue)
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .ToList();

            var activities = new List<RecentOrganizationalActivity>();

            foreach (var r in recentRequisitions)
            {
                var department = !string.IsNullOrEmpty(r.IssueDepartmentId)
                    ? await GetDepartmentNameByStringId(r.IssueDepartmentId)
                    : "Unknown Department";

                activities.Add(new RecentOrganizationalActivity
                {
                    Type = "Requisition",
                    Title = $"Requisition #{r.Id}",
                    Description = $"Status: {r.Status?.GetDescription() ?? "Unknown"}",
                    EntityName = GetEntityNameForScope(r, scope),
                    Timestamp = r.CreatedAt ?? DateTime.UtcNow,
                    ActorName = r.PayrollNo,
                    Department = department,
                    Station = await GetStationNameByIdAsync(r.IssueStationId),
                    Icon = GetStatusIcon(r.Status),
                    StatusColor = GetStatusColor(r.Status)
                });
            }

            return activities;
        }

        // ===== UTILITY METHODS =====

        private double CalculateAverageProcessingTime(List<Requisition> requisitions)
        {
            var completedRequisitions = requisitions.Where(r => r.Status == RequisitionStatus.Completed && r.CreatedAt.HasValue).ToList();
            if (!completedRequisitions.Any()) return 0;

            var totalDays = completedRequisitions.Sum(r => (DateTime.UtcNow - r.CreatedAt.Value).TotalDays);
            return totalDays / completedRequisitions.Count;
        }

        private bool IsOverdue(Requisition requisition)
        {
            if (!requisition.CreatedAt.HasValue || requisition.Status == RequisitionStatus.Completed) return false;
            return requisition.CreatedAt.Value.AddDays(7) < DateTime.UtcNow; // 7-day SLA
        }

        private string GetPerformanceIndicator(decimal completionRate)
        {
            return completionRate switch
            {
                >= 90 => "Excellent",
                >= 75 => "Good",
                >= 50 => "Average",
                _ => "Poor"
            };
        }

        private string GetPerformanceColor(decimal completionRate)
        {
            return completionRate switch
            {
                >= 90 => "text-success",
                >= 75 => "text-info",
                >= 50 => "text-warning",
                _ => "text-danger"
            };
        }

        private async Task<string> GetDepartmentNameById(int departmentId)
        {
            try
            {
                var department = await _context.EmployeeDetailsViews
                    .Where(e => e.DepartmentId == departmentId)
                    .Select(e => e.DepartmentName)
                    .FirstOrDefaultAsync();
                return department ?? $"Department {departmentId}";
            }
            catch
            {
                return $"Department {departmentId}";
            }
        }

        private async Task<string> GetDepartmentNameByStringId(string departmentId)
        {
            try
            {
                // Try to parse string to int first
                if (int.TryParse(departmentId, out int deptId))
                {
                    return await GetDepartmentNameById(deptId);
                }

                // If parsing fails, try to find by string comparison (fallback)
                var department = await _context.EmployeeDetailsViews
                    .Where(e => e.DepartmentId.ToString() == departmentId)
                    .Select(e => e.DepartmentName)
                    .FirstOrDefaultAsync();

                return department ?? $"Department {departmentId}";
            }
            catch
            {
                return $"Department {departmentId}";
            }
        }

        private async Task<string> GetStationNameByIdAsync(int? stationId)
        {
            if (!stationId.HasValue)
                return "Unknown Station";

            try
            {
                var station = await _context.StationDetailsViews
                    .Where(s => s.StationId == stationId.Value)
                    .Select(s => s.StationName)
                    .FirstOrDefaultAsync();
                return station ?? $"Station {stationId}";
            }
            catch
            {
                return $"Station {stationId}";
            }
        }

        private string GetEntityNameForScope(Requisition requisition, string scope)
        {
            return scope switch
            {
                "Department" => $"Department {requisition.DepartmentId}",
                "Station" => $"Station {requisition.IssueStationId}",
                "Cross-Station" => $"Station {requisition.IssueStationId}",
                "Organization" => $"Dept {requisition.DepartmentId} - Station {requisition.IssueStationId}",
                _ => "Unknown"
            };
        }

        private string GetStationNameById(int? stationId)
        {
            if (!stationId.HasValue)
                return "Unknown Station";

            try
            {
                var station = _context.StationDetailsViews.FirstOrDefault(s => s.StationId == stationId.Value);
                return station?.StationName ?? $"Station {stationId}";
            }
            catch
            {
                return $"Station {stationId}";
            }
        }

        private string GetStatusIcon(RequisitionStatus? status)
        {
            return status switch
            {
                RequisitionStatus.NotStarted => "ri-file-line",
                RequisitionStatus.PendingDispatch => "ri-timer-line",
                RequisitionStatus.PendingReceipt => "ri-truck-line",
                RequisitionStatus.Completed => "ri-check-line",
                RequisitionStatus.Cancelled => "ri-close-line",
                _ => "ri-question-line"
            };
        }

        private string GetStatusColor(RequisitionStatus? status)
        {
            return status switch
            {
                RequisitionStatus.NotStarted => "secondary",
                RequisitionStatus.PendingDispatch => "warning",
                RequisitionStatus.PendingReceipt => "info",
                RequisitionStatus.Completed => "success",
                RequisitionStatus.Cancelled => "danger",
                _ => "secondary"
            };
        }

        // ===================================================================
        // MATERIAL DASHBOARD METHODS
        // ===================================================================

        public async Task<MaterialDashboardViewModel> GetMaterialDashboardAsync(HttpContext httpContext)
        {
            try
            {
                var userProfile = await _userProfileService.GetCurrentUserProfileAsync();

                if (userProfile?.BasicInfo?.PayrollNo == null)
                {
                    return new MaterialDashboardViewModel
                    {
                        ErrorMessage = "Unable to load user profile",
                        IsDefaultUser = true
                    };
                }

                _logger.LogInformation("Material Dashboard access attempt - PayrollNo: {PayrollNo}, IsAdmin: {IsAdmin}, IsDefaultUser: {IsDefaultUser}, Role: '{Role}'",
                    userProfile.BasicInfo?.PayrollNo,
                    userProfile.RoleInformation?.IsAdmin,
                    userProfile.VisibilityScope?.IsDefaultUser,
                    userProfile.RoleInformation?.SystemRole);

                // Admin bypass - check this FIRST before any other checks
                if (userProfile.RoleInformation.IsAdmin)
                {
                    _logger.LogInformation("Admin access granted to Material Dashboard - PayrollNo: {PayrollNo}", userProfile.BasicInfo.PayrollNo);
                    // Continue to build dashboard with full organization access
                }
                // Default users cannot access Material Dashboard
                else if (userProfile.VisibilityScope.IsDefaultUser)
                {
                    _logger.LogWarning("Default user {PayrollNo} attempted to access Material Dashboard", userProfile.BasicInfo.PayrollNo);
                    return new MaterialDashboardViewModel
                    {
                        IsDefaultUser = true,
                        DashboardTitle = "Access Denied",
                        ErrorMessage = "You don't have permission to access the Material Dashboard"
                    };
                }

                _logger.LogInformation("Building Material Dashboard for user: {PayrollNo}", userProfile.BasicInfo.PayrollNo);

                var dashboard = new MaterialDashboardViewModel
                {
                    UserProfile = userProfile,
                    IsDefaultUser = false
                };

                // Set dashboard context based on user role
                if (userProfile.RoleInformation.IsAdmin)
                {
                    dashboard.DashboardTitle = "Organization Material Dashboard";
                    dashboard.AccessLevel = "Organization";
                }
                else if (userProfile.VisibilityScope.CanAccessAcrossStations)
                {
                    dashboard.DashboardTitle = $"{userProfile.BasicInfo.Department} Cross-Station Material Dashboard";
                    dashboard.AccessLevel = "Cross-Station";
                }
                else if (userProfile.VisibilityScope.CanAccessAcrossDepartments)
                {
                    dashboard.DashboardTitle = $"{userProfile.BasicInfo.Station} Station Material Dashboard";
                    dashboard.AccessLevel = "Station";
                }
                else
                {
                    dashboard.DashboardTitle = $"{userProfile.BasicInfo.Department} Department Material Dashboard";
                    dashboard.AccessLevel = "Department";
                }

                return dashboard;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building Material Dashboard");
                throw;
            }
        }
    }
}
