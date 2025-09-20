using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        private readonly IDashboardService _dashboardService;
        private readonly IDepartmentService _departmentService;
        private readonly IUserProfileService _userProfileService;
        private readonly IVisibilityAuthorizeService _visibilityService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            RequisitionContext context,
            IDashboardService dashboardService,
            IDepartmentService departmentService,
            IUserProfileService userProfileService,
            IVisibilityAuthorizeService visibilityService,
            ILogger<DashboardController> logger)
        {
            _context = context;
            _dashboardService = dashboardService;
            _departmentService = departmentService;
            _userProfileService = userProfileService;
            _visibilityService = visibilityService;
            _logger = logger;
        }

        // Enhanced default dashboard with intelligent routing based on user role
        public async Task<IActionResult> Index()
        {
            try
            {
                // Get user profile for role-based routing
                var userProfile = await _userProfileService.GetCurrentUserProfileAsync();

                if (userProfile?.BasicInfo?.PayrollNo == null)
                {
                    // Try to get payroll from session and rebuild profile
                    var payrollNo = HttpContext.Session.GetString("EmployeePayrollNo");
                    if (!string.IsNullOrEmpty(payrollNo))
                    {
                        try
                        {
                            userProfile = await _userProfileService.BuildUserProfileAsync(payrollNo);
                        }
                        catch (Exception ex)
                        {
                            TempData["Error"] = $"Profile creation failed: {ex.Message}";
                        }
                    }
                    
                    if (userProfile?.BasicInfo?.PayrollNo == null)
                    {
                        TempData["Error"] = "Unable to load user profile. Please log in again.";
                        return RedirectToAction("Index", "Login");
                    }
                }

                // Enhanced intelligent routing based on user role and permissions
                if (userProfile.VisibilityScope.IsDefaultUser)
                {
                    // Default users only see personal dashboard
                    return RedirectToAction("MyRequisitions");
                }
                else if (userProfile.VisibilityScope.CanAccessAcrossDepartments ||
                         userProfile.VisibilityScope.CanAccessAcrossStations ||
                         userProfile.RoleInformation.IsAdmin)
                {
                    // Users with management capabilities see management dashboard by default
                    return RedirectToAction("Management");
                }
                else
                {
                    // Department managers see personal dashboard by default, but can access management
                    return RedirectToAction("MyRequisitions");
                }
            }
            catch (Exception ex)
            {
                // Log error and fallback to basic dashboard
                TempData["Error"] = "Unable to load dashboard. Please try again.";
                return await MyRequisitions();
            }
        }

        // Enhanced personal dashboard with rich data and visualizations
        [ResponseCache(Duration = 300, VaryByHeader = "User-Agent", Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> MyRequisitions()
        {
            try
            {
                // Get enhanced dashboard data
                var viewModel = await _dashboardService.GetMyRequisitionsDashboardAsync(HttpContext);
                var userProfile = await _userProfileService.GetCurrentUserProfileAsync();

                // Set ViewBag data for enhanced UI
                ViewBag.UserProfile = userProfile;
                ViewBag.ShowActionAlerts = viewModel.AwaitingMyAction > 0 || viewModel.OverdueRequisitions > 0;
                ViewBag.CanAccessDepartmentDashboard = userProfile?.VisibilityScope?.CanAccessAcrossDepartments ?? false;
                ViewBag.PageTitle = $"Dashboard - {userProfile?.BasicInfo?.Name ?? "User"}";

                // Add navigation breadcrumbs
                ViewBag.Breadcrumbs = new[]
                {
                    new { Name = "Home", Url = "/" },
                    new { Name = "My Dashboard", Url = "/Dashboard/MyRequisitions" }
                };

                return View("MyRequisitions", viewModel);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Unable to load personal dashboard data.";
                return View("MyRequisitions", new MyRequisitionsDashboardViewModel
                {
                    ErrorMessage = "Dashboard data temporarily unavailable."
                });
            }
        }

        // Enhanced department dashboard with role-based access control
        public async Task<IActionResult> Department()
        {
            try
            {
                var userProfile = await _userProfileService.GetCurrentUserProfileAsync();

                // Verify department access permissions
                if (!HasDepartmentAccess(userProfile))
                {
                    TempData["Error"] = "You don't have permission to access the department dashboard.";
                    return RedirectToAction("MyRequisitions");
                }

                var viewModel = await _dashboardService.GetDepartmentDashboardAsync(HttpContext);

                // Set ViewBag data
                ViewBag.UserProfile = userProfile;
                ViewBag.DepartmentName = userProfile?.LocationAccess?.HomeDepartment?.Name ?? "Department";
                ViewBag.PageTitle = $"Department Dashboard - {ViewBag.DepartmentName}";

                ViewBag.Breadcrumbs = new[]
                {
                    new { Name = "Home", Url = "/" },
                    new { Name = "Department Dashboard", Url = "/Dashboard/Department" }
                };

                return View("Department", viewModel);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Unable to load department dashboard data.";
                return RedirectToAction("MyRequisitions");
            }
        }

        // API endpoint for real-time dashboard data refresh
        [HttpGet]
        [Route("Dashboard/api/data")]
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "*" })]
        public async Task<JsonResult> GetDashboardData()
        {
            try
            {
                var userProfile = await _userProfileService.GetCurrentUserProfileAsync();
                if (userProfile?.BasicInfo?.PayrollNo == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var viewModel = await _dashboardService.GetMyRequisitionsDashboardAsync(HttpContext);

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        // Primary metrics
                        totalRequisitions = viewModel.TotalRequisitions,
                        pendingRequisitions = viewModel.PendingRequisitions,
                        completedRequisitions = viewModel.CompletedRequisitions,
                        awaitingAction = viewModel.AwaitingMyAction,
                        thisMonth = viewModel.ThisMonthRequisitions,
                        overdue = viewModel.OverdueRequisitions,
                        averageProcessingDays = viewModel.AverageProcessingDays,

                        // Trend data
                        trends = new
                        {
                            totalTrend = viewModel.TotalRequisitionsTrend,
                            pendingTrend = viewModel.PendingRequisitionsTrend,
                            completedTrend = viewModel.CompletedRequisitionsTrend,
                            monthlyTrend = viewModel.ThisMonthTrend
                        },

                        // Chart data
                        statusDistribution = viewModel.RequisitionStatusCounts,
                        monthlyTrend = viewModel.TrendData?.RequisitionsByMonth ?? new List<MonthlyRequisitionData>(),
                        categoryDistribution = viewModel.TrendData?.RequisitionsByCategory ?? new Dictionary<string, int>(),
                        topItems = viewModel.TrendData?.TopRequestedItems?.Take(5) ?? new List<TopRequestedItem>(),

                        // Quick stats
                        quickStats = viewModel.QuickStats,

                        // Last updated
                        lastUpdated = DateTime.UtcNow.ToString("HH:mm:ss")
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to load dashboard data" });
            }
        }

        // API endpoint for action required items
        [HttpGet]
        [Route("Dashboard/api/actions")]
        public async Task<JsonResult> GetActionRequiredItems()
        {
            try
            {
                var userProfile = await _userProfileService.GetCurrentUserProfileAsync();
                if (userProfile?.BasicInfo?.PayrollNo == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var viewModel = await _dashboardService.GetMyRequisitionsDashboardAsync(HttpContext);

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        summary = new
                        {
                            pendingReceipt = viewModel.ActionRequired.PendingReceiptConfirmation,
                            needClarification = viewModel.ActionRequired.RequiringClarification,
                            overdue = viewModel.ActionRequired.OverdueItems,
                            readyForCollection = viewModel.ActionRequired.ReadyForCollection
                        },
                        urgentItems = viewModel.ActionRequired.UrgentRequisitions?.Take(10),
                        totalActionItems = viewModel.AwaitingMyAction
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to load action items" });
            }
        }

        // API endpoint for recent requisitions with pagination
        [HttpGet]
        [Route("Dashboard/api/recent")]
        public async Task<JsonResult> GetRecentRequisitions(int page = 1, int pageSize = 10)
        {
            try
            {
                var userProfile = await _userProfileService.GetCurrentUserProfileAsync();
                if (userProfile?.BasicInfo?.PayrollNo == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Get user's requisitions with pagination
                var query = _context.Requisitions
                    .Where(r => r.PayrollNo == userProfile.BasicInfo.PayrollNo)
                    .Include(r => r.RequisitionItems)
                    .OrderByDescending(r => r.CreatedAt);

                var totalCount = await query.CountAsync();
                var requisitions = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var requisitionSummaries = new List<object>();

                foreach (var r in requisitions)
                {
                    string issueLocationName = await _departmentService.GetLocationNameFromIdsAsync(
                        r.IssueStationId, r.IssueDepartmentId);

                    string deliveryLocationName = await _departmentService.GetLocationNameFromIdsAsync(
                        r.DeliveryStationId, r.DeliveryDepartmentId);

                    requisitionSummaries.Add(new
                    {
                        id = r.Id,
                        issueStation = issueLocationName,
                        deliveryStation = deliveryLocationName,
                        status = r.Status?.GetDescription() ?? "Not Started",
                        statusBadgeClass = GetStatusBadgeClass(r.Status),
                        createdAt = r.CreatedAt?.ToString("dd MMM yyyy") ?? "N/A",
                        itemCount = r.RequisitionItems?.Count() ?? 0,
                        priority = "Normal", // Default since Priority doesn't exist in model
                        isOverdue = r.CreatedAt.HasValue && r.CreatedAt.Value.AddDays(7) < DateTime.UtcNow &&
                                   r.Status != RequisitionStatus.Completed,
                        daysInStatus = CalculateDaysInStatus(r),
                        urgencyColor = GetUrgencyColor(r)
                    });
                }

                return Json(new
                {
                    success = true,
                    data = requisitionSummaries,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalCount = totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                        hasNext = page * pageSize < totalCount,
                        hasPrevious = page > 1
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to load recent requisitions" });
            }
        }

        // API endpoint for chart data
        [HttpGet]
        [Route("Dashboard/api/charts")]
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "*" })]
        public async Task<JsonResult> GetChartData()
        {
            try
            {
                var userProfile = await _userProfileService.GetCurrentUserProfileAsync();
                if (userProfile?.BasicInfo?.PayrollNo == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var viewModel = await _dashboardService.GetMyRequisitionsDashboardAsync(HttpContext);

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        statusDistribution = viewModel.RequisitionStatusCounts,
                        categoryDistribution = viewModel.TrendData?.RequisitionsByCategory ?? new Dictionary<string, int>(),
                        monthlyTrend = viewModel.TrendData?.RequisitionsByMonth?.Take(12) ?? new List<MonthlyRequisitionData>(),
                        topItems = viewModel.TrendData?.TopRequestedItems?.Take(5) ?? new List<TopRequestedItem>()
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to load chart data" });
            }
        }

        // API endpoint for performance metrics
        [HttpGet]
        [Route("Dashboard/api/performance")]
        public async Task<JsonResult> GetPerformanceMetrics()
        {
            try
            {
                var userProfile = await _userProfileService.GetCurrentUserProfileAsync();
                if (userProfile?.BasicInfo?.PayrollNo == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var viewModel = await _dashboardService.GetMyRequisitionsDashboardAsync(HttpContext);

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        completionRate = viewModel.CompletionRate,
                        performanceStatus = viewModel.PerformanceStatus,
                        performanceColor = viewModel.PerformanceColor,
                        averageProcessingDays = viewModel.FormattedAverageProcessingDays,
                        totalRequests = viewModel.TotalRequisitions,
                        monthlyActivity = viewModel.ThisMonthRequisitions,
                        efficiency = new
                        {
                            completedOnTime = viewModel.CompletedRequisitions - viewModel.OverdueRequisitions,
                            overdue = viewModel.OverdueRequisitions,
                            pending = viewModel.PendingRequisitions,
                            averageDays = viewModel.AverageProcessingDays
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to load performance metrics" });
            }
        }

        // Quick action endpoint for creating new requisition
        [HttpGet]
        [Route("Dashboard/quick-create")]
        public async Task<IActionResult> QuickCreateRequisition()
        {
            try
            {
                var userProfile = await _userProfileService.GetCurrentUserProfileAsync();
                if (userProfile?.BasicInfo?.PayrollNo == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                // Pre-populate requisition with user data
                TempData["QuickCreate"] = true;
                TempData["UserDepartment"] = userProfile.LocationAccess.HomeDepartment.Id;
                TempData["UserStation"] = userProfile.LocationAccess.HomeStation.Id;

                return RedirectToAction("Create", "Requisitions");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Unable to initiate quick create";
                return RedirectToAction("MyRequisitions");
            }
        }

        // Generate and download dashboard report
        [HttpGet]
        [Route("Dashboard/export")]
        public async Task<IActionResult> ExportDashboardReport(string format = "csv")
        {
            try
            {
                var userProfile = await _userProfileService.GetCurrentUserProfileAsync();
                if (userProfile?.BasicInfo?.PayrollNo == null)
                {
                    return BadRequest("User not authenticated");
                }

                var viewModel = await _dashboardService.GetMyRequisitionsDashboardAsync(HttpContext);

                switch (format.ToLower())
                {
                    case "csv":
                        return await GenerateCsvReport(viewModel, userProfile);
                    case "excel":
                        return await GenerateExcelReport(viewModel, userProfile);
                    case "pdf":
                        return await GeneratePdfReport(viewModel, userProfile);
                    default:
                        return BadRequest("Unsupported format. Use csv, excel, or pdf.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Failed to generate report");
            }
        }

        // Refresh user profile cache
        [HttpPost]
        [Route("Dashboard/refresh-profile")]
        public async Task<JsonResult> RefreshUserProfile()
        {
            try
            {
                var userProfile = await _userProfileService.GetCurrentUserProfileAsync();
                if (userProfile?.BasicInfo?.PayrollNo == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Refresh the user profile
                var refreshedProfile = await _userProfileService.RefreshUserProfileAsync(
                    userProfile.BasicInfo.PayrollNo);

                return Json(new
                {
                    success = true,
                    message = "Profile refreshed successfully",
                    lastRefresh = refreshedProfile.CacheInfo.LastRefresh
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to refresh profile" });
            }
        }

        #region Private Helper Methods

        // Check if user has access to department dashboard
        private bool HasDepartmentAccess(UserProfile userProfile)
        {
            return userProfile?.VisibilityScope?.CanAccessAcrossDepartments == true;
        }

        // Get status badge class for UI styling
        private string GetStatusBadgeClass(RequisitionStatus? status)
        {
            return status switch
            {
                RequisitionStatus.NotStarted => "bg-warning",
                RequisitionStatus.PendingDispatch => "bg-info",
                RequisitionStatus.PendingReceipt => "bg-primary",
                RequisitionStatus.Completed => "bg-success",
                RequisitionStatus.Cancelled => "bg-danger",
                _ => "bg-secondary"
            };
        }

        // Calculate days in current status
        private int CalculateDaysInStatus(Requisition requisition)
        {
            var statusDate = requisition.UpdatedAt ?? requisition.CreatedAt ?? DateTime.UtcNow;
            return (int)(DateTime.UtcNow - statusDate).TotalDays;
        }

        // Get urgency color based on status and age
        private string GetUrgencyColor(Requisition requisition)
        {
            var daysInStatus = CalculateDaysInStatus(requisition);
            var isOverdue = requisition.CreatedAt.HasValue &&
                           requisition.CreatedAt.Value.AddDays(7) < DateTime.UtcNow &&
                           requisition.Status != RequisitionStatus.Completed;

            if (isOverdue) return "text-danger";
            if (daysInStatus > 5) return "text-warning";
            return "text-muted";
        }

        // Generate CSV report
        private async Task<IActionResult> GenerateCsvReport(
            MyRequisitionsDashboardViewModel viewModel,
            UserProfile userProfile)
        {
            var csv = new System.Text.StringBuilder();

            // Header
            csv.AppendLine($"Dashboard Report for {userProfile.BasicInfo.Fullname}");
            csv.AppendLine($"Generated on: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
            csv.AppendLine($"Department: {userProfile.BasicInfo.Department}");
            csv.AppendLine($"Station: {userProfile.BasicInfo.Station}");
            csv.AppendLine("");

            // Summary Metrics
            csv.AppendLine("SUMMARY METRICS");
            csv.AppendLine("Metric,Value,Trend %");
            csv.AppendLine($"Total Requisitions,{viewModel.TotalRequisitions},{viewModel.TotalRequisitionsTrend:F2}");
            csv.AppendLine($"Pending Requisitions,{viewModel.PendingRequisitions},{viewModel.PendingRequisitionsTrend:F2}");
            csv.AppendLine($"Completed Requisitions,{viewModel.CompletedRequisitions},{viewModel.CompletedRequisitionsTrend:F2}");
            csv.AppendLine($"This Month Requisitions,{viewModel.ThisMonthRequisitions},{viewModel.ThisMonthTrend:F2}");
            csv.AppendLine($"Awaiting My Action,{viewModel.AwaitingMyAction},-");
            csv.AppendLine($"Overdue Requisitions,{viewModel.OverdueRequisitions},-");
            csv.AppendLine($"Average Processing Days,{viewModel.AverageProcessingDays:F1},-");
            csv.AppendLine($"Completion Rate,{viewModel.CompletionRate:F1}%,-");
            csv.AppendLine("");

            // Action Required
            if (viewModel.ActionRequired?.HasAnyActions == true)
            {
                csv.AppendLine("ACTION REQUIRED");
                csv.AppendLine("Category,Count");
                csv.AppendLine($"Pending Receipt Confirmation,{viewModel.ActionRequired.PendingReceiptConfirmation}");
                csv.AppendLine($"Requiring Clarification,{viewModel.ActionRequired.RequiringClarification}");
                csv.AppendLine($"Overdue Items,{viewModel.ActionRequired.OverdueItems}");
                csv.AppendLine($"Ready for Collection,{viewModel.ActionRequired.ReadyForCollection}");
                csv.AppendLine("");
            }

            // Recent Requisitions
            csv.AppendLine("RECENT REQUISITIONS");
            csv.AppendLine("ID,Issue Station,Delivery Station,Status,Created Date,Items,Priority,Days in Status,Overdue");

            foreach (var req in viewModel.RecentRequisitions?.Take(20) ?? new List<RequisitionSummary>())
            {
                csv.AppendLine($"{req.Id},{req.IssueStation},{req.DeliveryStation},{req.StatusDescription},{req.FormattedCreatedDate},{req.ItemCount},{req.Priority},{req.DaysInCurrentStatus},{req.IsOverdue}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"Dashboard_Report_{userProfile.BasicInfo.PayrollNo}_{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        // Generate Excel report placeholder
        private async Task<IActionResult> GenerateExcelReport(
            MyRequisitionsDashboardViewModel viewModel,
            UserProfile userProfile)
        {
            // TODO: Implement Excel generation using EPPlus or similar
            // For now, return CSV with Excel MIME type
            var csvResult = await GenerateCsvReport(viewModel, userProfile);

            // Convert to Excel-compatible format
            var fileName = $"Dashboard_Report_{userProfile.BasicInfo.PayrollNo}_{DateTime.UtcNow:yyyyMMdd}.xlsx";
            return File(((FileContentResult)csvResult).FileContents,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        // Generate PDF report placeholder
        private async Task<IActionResult> GeneratePdfReport(
            MyRequisitionsDashboardViewModel viewModel,
            UserProfile userProfile)
        {
            // TODO: Implement PDF generation using iTextSharp or similar
            // For now, return a simple text-based PDF
            var reportContent = $@"
DASHBOARD REPORT
================

User: {userProfile.BasicInfo.Fullname}
Department: {userProfile.BasicInfo.Department}
Station: {userProfile.BasicInfo.Station}
Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}

SUMMARY METRICS
===============
Total Requisitions: {viewModel.TotalRequisitions} (Trend: {viewModel.TotalRequisitionsTrend:F2}%)
Pending Requisitions: {viewModel.PendingRequisitions} (Trend: {viewModel.PendingRequisitionsTrend:F2}%)
Completed Requisitions: {viewModel.CompletedRequisitions} (Trend: {viewModel.CompletedRequisitionsTrend:F2}%)
This Month: {viewModel.ThisMonthRequisitions} (Trend: {viewModel.ThisMonthTrend:F2}%)
Awaiting Action: {viewModel.AwaitingMyAction}
Average Processing: {viewModel.AverageProcessingDays:F1} days
Completion Rate: {viewModel.CompletionRate:F1}%

PERFORMANCE STATUS
==================
Status: {viewModel.PerformanceStatus}
";

            var bytes = System.Text.Encoding.UTF8.GetBytes(reportContent);
            return File(bytes, "application/pdf", $"Dashboard_Report_{userProfile.BasicInfo.PayrollNo}_{DateTime.UtcNow:yyyyMMdd}.pdf");
        }

        #endregion

        #region Management Dashboard

        /// <summary>
        /// Management Dashboard - Role-based organizational insights
        /// Separate from personal "My Dashboard" - shows organizational data based on permissions
        /// </summary>
        [HttpGet]
        [Route("Dashboard/Management")]
        public async Task<IActionResult> Management()
        {
            try
            {
                var userProfile = await _userProfileService.GetCurrentUserProfileAsync();

                // Default users cannot access Management Dashboard
                if (userProfile == null || userProfile.VisibilityScope.IsDefaultUser)
                {
                    TempData["Warning"] = "You don't have access to Management Dashboard. Showing your personal dashboard instead.";
                    return RedirectToAction("MyRequisitions");
                }

                // Get management dashboard data
                var viewModel = await _dashboardService.GetManagementDashboardAsync(HttpContext);

                // Handle service-level access denial
                if (viewModel.IsDefaultUser || viewModel.HasError)
                {
                    TempData["Error"] = viewModel.ErrorMessage ?? "Unable to access Management Dashboard.";
                    return RedirectToAction("MyRequisitions");
                }

                // Set ViewBag data for breadcrumbs and context
                ViewBag.UserProfile = userProfile;
                ViewBag.PageTitle = viewModel.DashboardTitle;
                ViewBag.AccessLevel = viewModel.AccessLevel;
                ViewBag.CanAccessDepartmentDashboard = userProfile.VisibilityScope.CanAccessAcrossDepartments;

                ViewBag.Breadcrumbs = new[]
                {
                    new { Name = "Home", Url = "/" },
                    new { Name = "Management Dashboard", Url = "/Dashboard/Management" }
                };

                return View("Management", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Management Dashboard for user");
                TempData["Error"] = "Unable to load management dashboard. Please try again.";
                return RedirectToAction("MyRequisitions");
            }
        }

        /// <summary>
        /// API endpoint for refreshing management dashboard data
        /// </summary>
        [HttpGet]
        [Route("Dashboard/Management/Refresh")]
        public async Task<IActionResult> RefreshManagementData()
        {
            try
            {
                var userProfile = await _userProfileService.GetCurrentUserProfileAsync();

                // Validate access
                if (userProfile == null || userProfile.VisibilityScope.IsDefaultUser)
                {
                    return Json(new { success = false, message = "Access denied" });
                }

                // Get fresh data
                var viewModel = await _dashboardService.GetManagementDashboardAsync(HttpContext);

                if (viewModel.HasError)
                {
                    return Json(new { success = false, message = viewModel.ErrorMessage });
                }

                // Return key metrics for AJAX refresh
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        primaryMetrics = viewModel.PrimaryMetrics,
                        statusDistribution = viewModel.StatusDistribution,
                        hasComparisonData = viewModel.HasComparisonData,
                        comparisonCount = viewModel.ComparisonData?.Count ?? 0,
                        actionItemsCount = viewModel.ActionRequired?.Count ?? 0,
                        recentActivityCount = viewModel.RecentActivity?.Count ?? 0,
                        lastUpdated = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing management dashboard data");
                return Json(new { success = false, message = "Unable to refresh data" });
            }
        }

        /// <summary>
        /// API endpoint for getting chart data
        /// </summary>
        [HttpGet]
        [Route("Dashboard/Management/ChartData")]
        public async Task<IActionResult> GetManagementChartData()
        {
            try
            {
                var userProfile = await _userProfileService.GetCurrentUserProfileAsync();

                // Validate access
                if (userProfile == null || userProfile.VisibilityScope.IsDefaultUser)
                {
                    return Json(new { success = false, message = "Access denied" });
                }

                var viewModel = await _dashboardService.GetManagementDashboardAsync(HttpContext);

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        statusDistribution = viewModel.StatusDistribution,
                        trendAnalysis = viewModel.TrendAnalysis,
                        comparisonData = viewModel.ComparisonData?.Take(5) // Limit for chart performance
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chart data");
                return Json(new { success = false, message = "Unable to load chart data" });
            }
        }

        #endregion

        #region Material Dashboard

        /// <summary>
        /// Material Dashboard - Role-based material management insights
        /// Separate from requisition-focused dashboards - shows material asset tracking and analytics
        /// </summary>
        [HttpGet]
        [Route("Dashboard/Material")]
        public async Task<IActionResult> Material()
        {
            try
            {
                Console.WriteLine("=== MATERIAL DASHBOARD CONTROLLER ===");

                var userProfile = await _userProfileService.GetCurrentUserProfileAsync();

                if (userProfile == null)
                {
                    Console.WriteLine("ERROR: User profile is null");
                    TempData["Error"] = "Unable to load user profile. Please try again.";
                    return RedirectToAction("MyRequisitions");
                }

                // DEBUG: Log controller-level checks
                Console.WriteLine($"CONTROLLER - IsAdmin: {userProfile.RoleInformation.IsAdmin}");
                Console.WriteLine($"CONTROLLER - IsDefaultUser: {userProfile.VisibilityScope.IsDefaultUser}");
                Console.WriteLine($"CONTROLLER - User Role: {userProfile.BasicInfo.Role}");

                // Allow admin users OR non-default users
                if (userProfile.VisibilityScope.IsDefaultUser && !userProfile.RoleInformation.IsAdmin)
                {
                    Console.WriteLine($"REDIRECTING: Default user without admin access: {userProfile.BasicInfo.PayrollNo}");
                    TempData["Error"] = "You don't have permission to access the Material Dashboard.";
                    return RedirectToAction("MyRequisitions");
                }

                Console.WriteLine($"PROCEEDING: User has access to Material Dashboard: {userProfile.BasicInfo.PayrollNo}");

                // Get material dashboard data
                var viewModel = await _dashboardService.GetMaterialDashboardAsync(HttpContext);

                // Handle service-level access denial
                if (viewModel.IsDefaultUser && !userProfile.RoleInformation.IsAdmin)
                {
                    Console.WriteLine($"SERVICE LEVEL REDIRECT: Service returned IsDefaultUser for: {userProfile.BasicInfo.PayrollNo}");
                    TempData["Error"] = viewModel.ErrorMessage ?? "Unable to access Material Dashboard.";
                    return RedirectToAction("MyRequisitions");
                }

                // Set ViewBag data
                ViewBag.UserProfile = userProfile;
                ViewBag.PageTitle = viewModel.DashboardTitle;
                ViewBag.AccessLevel = viewModel.AccessLevel;
                ViewBag.CanAccessDepartmentDashboard = userProfile.VisibilityScope.CanAccessAcrossDepartments;

                ViewBag.Breadcrumbs = new[]
                {
            new { Name = "Home", Url = "/" },
            new { Name = "Material Dashboard", Url = "/Dashboard/Material" }
        };

                Console.WriteLine($"SUCCESS: Rendering Material Dashboard for: {userProfile.BasicInfo.PayrollNo}");
                return View("Material", viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION in Material Dashboard: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                TempData["Error"] = "Unable to load material dashboard. Please try again.";
                return RedirectToAction("MyRequisitions");
            }
        }

        [HttpGet]
        [Route("Dashboard/GetFilterData")]
        public async Task<IActionResult> GetFilterData()
        {
            try
            {
                var userProfile = await _userProfileService.GetCurrentUserProfileAsync();

                if (userProfile == null)
                {
                    return Json(new { error = "Unable to load user profile" });
                }

                // Get station categories from accessible stations
                var categories = userProfile.LocationAccess.AccessibleStations
                    .Select(s => new {
                        Code = s.Category.Code,
                        Name = s.Category.Name
                    })
                    .Where(c => !string.IsNullOrEmpty(c.Code))
                    .Distinct()
                    .OrderBy(c => c.Code == "headoffice" ? 1 :
                                  c.Code == "region" ? 2 :
                                  c.Code == "factory" ? 3 : 4)
                    .ToList();

                // Get accessible stations with categories
                var stations = userProfile.LocationAccess.AccessibleStations
                    .Select(s => new {
                        Id = s.Id,
                        Name = s.Name,
                        CategoryCode = s.Category.Code
                    })
                    .Where(s => !string.IsNullOrEmpty(s.Name))
                    .OrderBy(s => s.Name)
                    .ToList();

                // Get accessible departments
                var departments = userProfile.LocationAccess.AccessibleDepartments
                    .Select(d => new {
                        Id = d.Id,
                        Name = d.Name
                    })
                    .Where(d => !string.IsNullOrEmpty(d.Name))
                    .OrderBy(d => d.Name)
                    .ToList();

                _logger.LogInformation("Filter data loaded: {CategoryCount} categories, {StationCount} stations, {DepartmentCount} departments",
                    categories.Count, stations.Count, departments.Count);

                return Json(new { categories, stations, departments });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading filter data");
                return Json(new { error = "Error loading filter data" });
            }
        }

        [HttpPost]
        [Route("Dashboard/GetMaterialKPIs")]
        public async Task<IActionResult> GetMaterialKPIs([FromBody] MaterialKPIRequest request)
        {
            try
            {
                var userProfile = await _userProfileService.GetCurrentUserProfileAsync();

                if (userProfile == null)
                {
                    return Json(new { error = "Unable to load user profile", success = false });
                }

                _logger.LogInformation("Loading Material KPIs for user: {PayrollNo} with filters: {@Filters}",
                    userProfile.BasicInfo.PayrollNo, request);

                // Use MaterialDashboard view directly to avoid decimal casting issues
                var visibleQuery = _visibilityService.ApplyVisibilityScopeWithProfile(
                    _context.MaterialDashboardViews,
                    userProfile
                );

                // Apply additional dashboard filters on top of visibility filtering
                if (!string.IsNullOrEmpty(request.Category))
                {
                    visibleQuery = visibleQuery.Where(m => m.StationCategoryCode == request.Category);
                }

                if (!string.IsNullOrEmpty(request.Station))
                {
                    if (int.TryParse(request.Station, out var stationId))
                    {
                        visibleQuery = visibleQuery.Where(m => m.CurrentStationId == stationId);
                    }
                }

                if (!string.IsNullOrEmpty(request.Department))
                {
                    if (int.TryParse(request.Department, out var departmentId))
                    {
                        visibleQuery = visibleQuery.Where(m => m.CurrentDepartmentId == departmentId);
                    }
                }

                // Count materials directly - no problematic decimal aggregations
                var totalMaterials = await visibleQuery.CountAsync();

                // Calculate total value using SUM on PurchasePrice
                var totalValue = await visibleQuery
                    .Where(m => m.PurchasePrice.HasValue)
                    .SumAsync(m => m.PurchasePrice.Value);

                // Calculate available materials count (using MaterialStatus enum: Available = 4)
                var availableMaterials = await visibleQuery
                    .Where(m => m.MaterialStatus == MRIV.Enums.MaterialStatus.Available)
                    .CountAsync();

                // Calculate warranty expired materials count
                var warrantyExpired = await visibleQuery
                    .Where(m => m.WarrantyEndDate.HasValue && m.WarrantyEndDate.Value < DateTime.Now)
                    .CountAsync();

                // TODO: Calculate trend based on date range comparison
                var trend = 0.0m;

                var response = new
                {
                    totalMaterials = totalMaterials,
                    totalValue = totalValue,
                    availableMaterials = availableMaterials,
                    warrantyExpired = warrantyExpired,
                    trend = trend,
                    success = true,
                    debug = new
                    {
                        materialsFound = totalMaterials,
                        totalValueCalculated = totalValue,
                        availableMaterialsFound = availableMaterials,
                        warrantyExpiredFound = warrantyExpired,
                        userIsAdmin = userProfile.RoleInformation.IsAdmin,
                        userAccessLevel = userProfile.VisibilityScope.PermissionLevel.ToString(),
                        appliedFilters = new
                        {
                            category = request.Category,
                            station = request.Station,
                            department = request.Department
                        }
                    }
                };

                _logger.LogInformation("Material KPIs calculated: Total Materials = {TotalMaterials}, Total Value = {TotalValue}, Available = {AvailableMaterials}, Warranty Expired = {WarrantyExpired}",
                    totalMaterials, totalValue, availableMaterials, warrantyExpired);

                return Json(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Material KPIs");
                return Json(new {
                    error = "Error loading Material KPIs: " + ex.Message,
                    success = false
                });
            }
        }

        [HttpPost]
        [Route("Dashboard/GetMaterialChartData")]
        public async Task<IActionResult> GetMaterialChartData([FromBody] MaterialKPIRequest request)
        {
            try
            {
                var userProfile = await _userProfileService.GetCurrentUserProfileAsync();

                if (userProfile == null)
                {
                    return Json(new { error = "Unable to load user profile", success = false });
                }

                _logger.LogInformation("Loading Material Chart Data for user: {PayrollNo} with filters: {@Filters}",
                    userProfile.BasicInfo.PayrollNo, request);

                // Apply visibility filtering using VisibilityService
                var visibleQuery = _visibilityService.ApplyVisibilityScopeWithProfile(
                    _context.MaterialDashboardViews,
                    userProfile
                );

                // Apply additional dashboard filters
                if (!string.IsNullOrEmpty(request.Category))
                {
                    visibleQuery = visibleQuery.Where(m => m.StationCategoryCode == request.Category);
                }

                if (!string.IsNullOrEmpty(request.Station))
                {
                    if (int.TryParse(request.Station, out var stationId))
                    {
                        visibleQuery = visibleQuery.Where(m => m.CurrentStationId == stationId);
                    }
                }

                if (!string.IsNullOrEmpty(request.Department))
                {
                    if (int.TryParse(request.Department, out var departmentId))
                    {
                        visibleQuery = visibleQuery.Where(m => m.CurrentDepartmentId == departmentId);
                    }
                }

                // Get materials by category data
                var categoryData = await visibleQuery
                    .GroupBy(m => new { m.CategoryId, m.CategoryName })
                    .Select(g => new {
                        categoryId = g.Key.CategoryId,
                        categoryName = g.Key.CategoryName ?? "Unknown",
                        count = g.Count()
                    })
                    .OrderByDescending(x => x.count)
                    .ToListAsync();

                // Get status breakdown data
                var statusData = await visibleQuery
                    .GroupBy(m => m.MaterialStatus)
                    .Select(g => new {
                        status = g.Key,
                        count = g.Count()
                    })
                    .ToListAsync();

                var response = new
                {
                    categoryBreakdown = categoryData,
                    statusBreakdown = statusData,
                    success = true,
                    debug = new
                    {
                        totalMaterials = await visibleQuery.CountAsync(),
                        categoriesFound = categoryData.Count,
                        statusTypesFound = statusData.Count,
                        userAccessLevel = userProfile.VisibilityScope.PermissionLevel.ToString()
                    }
                };

                _logger.LogInformation("Material Chart Data calculated: {Categories} categories, {StatusTypes} status types",
                    categoryData.Count, statusData.Count);

                return Json(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Material Chart Data");
                return Json(new { success = false, error = "Unable to load Material Chart Data. Please try again." });
            }
        }

        [HttpPost]
        [Route("Dashboard/GetMaterialInsightsData")]
        public async Task<IActionResult> GetMaterialInsightsData([FromBody] MaterialKPIRequest request)
        {
            try
            {
                var userProfile = await _userProfileService.GetCurrentUserProfileAsync();
                if (userProfile == null)
                {
                    return Json(new { error = "Unable to load user profile", success = false });
                }

                var result = await _dashboardService.GetMaterialInsightsAsync(HttpContext, request);
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Material Insights Data");
                return Json(new { success = false, error = "Unable to load Material Insights. Please try again." });
            }
        }

        [HttpGet]
        [Route("Dashboard/GetRecentMaterialActivity")]
        public async Task<IActionResult> GetRecentMaterialActivity()
        {
            try
            {
                var userProfile = await _userProfileService.GetCurrentUserProfileAsync();
                if (userProfile == null)
                {
                    return Json(new { error = "Unable to load user profile", success = false });
                }

                var result = await _dashboardService.GetRecentMaterialActivityAsync(HttpContext);
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Recent Material Activity");
                return Json(new { success = false, error = "Unable to load recent activity. Please try again." });
            }
        }

        public class MaterialKPIRequest
        {
            public string? Category { get; set; }
            public string? Station { get; set; }
            public string? Department { get; set; }
            public string? StartDate { get; set; }
            public string? EndDate { get; set; }
        }

        #endregion
    }
}
