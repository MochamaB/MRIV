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
    /// <summary>
    /// Enhanced Dashboard Controller with UserProfile integration and advanced features
    /// This replaces the existing DashboardController with enhanced functionality
    /// </summary>
    [CustomAuthorize]
    public class EnhancedDashboardController : Controller
    {
        private readonly RequisitionContext _context;
        private readonly IEmployeeService _employeeService;
        private readonly IDashboardService _dashboardService;
        private readonly IDepartmentService _departmentService;
        private readonly IUserProfileService _userProfileService;

        public EnhancedDashboardController(
            RequisitionContext context,
            IEmployeeService employeeService,
            IDashboardService dashboardService,
            IDepartmentService departmentService,
            IUserProfileService userProfileService)
        {
            _context = context;
            _employeeService = employeeService;
            _dashboardService = dashboardService;
            _departmentService = departmentService;
            _userProfileService = userProfileService;
        }

        /// <summary>
        /// Enhanced default dashboard with intelligent routing based on user role
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                // Get user profile for role-based routing
                var userProfile = await _userProfileService.GetCurrentUserProfileAsync();

                if (userProfile?.BasicInfo?.PayrollNo == null)
                {
                    TempData["Error"] = "Unable to load user profile. Please log in again.";
                    return RedirectToAction("Login", "Account");
                }

                // Intelligent routing based on user role and permissions
                if (ShouldShowDepartmentDashboard(userProfile))
                {
                    // Redirect managers and supervisors to department dashboard
                    return RedirectToAction("Department");
                }

                // Default to personal dashboard for regular users
                return await MyRequisitions();
            }
            catch (Exception ex)
            {
                // Log error (implement logging framework as needed)
                TempData["Error"] = "Unable to load dashboard. Please try again.";
                return await MyRequisitions(); // Fallback to basic dashboard
            }
        }

        /// <summary>
        /// Enhanced personal dashboard with rich data and visualizations
        /// </summary>
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

        /// <summary>
        /// Enhanced department dashboard with role-based access control
        /// </summary>
        [HttpGet]
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

        /// <summary>
        /// API endpoint for real-time dashboard data refresh
        /// </summary>
        [HttpGet]
        [Route("Dashboard/api/data")]
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
                        monthlyTrend = viewModel.TrendData?.RequisitionsByMonth,
                        categoryDistribution = viewModel.TrendData?.RequisitionsByCategory,
                        topItems = viewModel.TrendData?.TopRequestedItems?.Take(5),

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

        /// <summary>
        /// API endpoint for action required items
        /// </summary>
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

        /// <summary>
        /// API endpoint for recent requisitions with pagination
        /// </summary>
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
                        statusColor = GetStatusColor(r.Status),
                        createdAt = r.CreatedAt.ToString("dd MMM yyyy"),
                        itemCount = r.RequisitionItems?.Count() ?? 0,
                        priority = r.Priority,
                        isOverdue = r.ExpectedDeliveryDate.HasValue &&
                                   r.ExpectedDeliveryDate < DateTime.UtcNow &&
                                   r.Status != RequisitionStatus.Completed,
                        daysInStatus = CalculateDaysInStatus(r)
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

        /// <summary>
        /// Generate and download dashboard report
        /// </summary>
        [HttpGet]
        [Route("Dashboard/export")]
        public async Task<IActionResult> ExportDashboardReport(string format = "pdf")
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
                    case "pdf":
                        return await GeneratePdfReport(viewModel, userProfile);
                    case "excel":
                        return await GenerateExcelReport(viewModel, userProfile);
                    case "csv":
                        return await GenerateCsvReport(viewModel, userProfile);
                    default:
                        return BadRequest("Unsupported format");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Failed to generate report");
            }
        }

        /// <summary>
        /// Quick action endpoint for creating new requisition
        /// </summary>
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

        /// <summary>
        /// Refresh user profile cache
        /// </summary>
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

        /// <summary>
        /// Determine if user should see department dashboard by default
        /// </summary>
        private bool ShouldShowDepartmentDashboard(UserProfile userProfile)
        {
            return userProfile.VisibilityScope.PermissionLevel >= PermissionLevel.Manager &&
                   userProfile.VisibilityScope.CanAccessAcrossDepartments &&
                   userProfile.RoleInformation.RoleGroups.Any(rg =>
                       rg.Name.Contains("Manager") || rg.Name.Contains("Supervisor"));
        }

        /// <summary>
        /// Check if user has access to department dashboard
        /// </summary>
        private bool HasDepartmentAccess(UserProfile userProfile)
        {
            return userProfile?.VisibilityScope?.CanAccessAcrossDepartments == true ||
                   userProfile?.VisibilityScope?.PermissionLevel >= PermissionLevel.Manager;
        }

        /// <summary>
        /// Get status color for UI styling
        /// </summary>
        private string GetStatusColor(RequisitionStatus? status)
        {
            return status switch
            {
                RequisitionStatus.NotStarted => "warning",
                RequisitionStatus.PendingDispatch => "info",
                RequisitionStatus.PendingReceipt => "primary",
                RequisitionStatus.Completed => "success",
                RequisitionStatus.Cancelled => "danger",
                _ => "secondary"
            };
        }

        /// <summary>
        /// Calculate days in current status
        /// </summary>
        private int CalculateDaysInStatus(Requisition requisition)
        {
            var statusDate = requisition.LastStatusChangeDate ?? requisition.CreatedAt;
            return (int)(DateTime.UtcNow - statusDate).TotalDays;
        }

        /// <summary>
        /// Generate PDF report (placeholder - implement with your preferred PDF library)
        /// </summary>
        private async Task<IActionResult> GeneratePdfReport(
            MyRequisitionsDashboardViewModel viewModel,
            UserProfile userProfile)
        {
            // TODO: Implement PDF generation using iTextSharp, SelectPdf, or similar
            // This is a placeholder implementation
            var reportContent = $"Dashboard Report for {userProfile.BasicInfo.Name} - {DateTime.UtcNow:yyyy-MM-dd}";
            var bytes = System.Text.Encoding.UTF8.GetBytes(reportContent);

            return File(bytes, "application/pdf", $"Dashboard_Report_{DateTime.UtcNow:yyyyMMdd}.pdf");
        }

        /// <summary>
        /// Generate Excel report (placeholder - implement with EPPlus or similar)
        /// </summary>
        private async Task<IActionResult> GenerateExcelReport(
            MyRequisitionsDashboardViewModel viewModel,
            UserProfile userProfile)
        {
            // TODO: Implement Excel generation using EPPlus or similar
            var reportContent = $"Dashboard Report for {userProfile.BasicInfo.Name}";
            var bytes = System.Text.Encoding.UTF8.GetBytes(reportContent);

            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Dashboard_Report_{DateTime.UtcNow:yyyyMMdd}.xlsx");
        }

        /// <summary>
        /// Generate CSV report
        /// </summary>
        private async Task<IActionResult> GenerateCsvReport(
            MyRequisitionsDashboardViewModel viewModel,
            UserProfile userProfile)
        {
            var csv = new System.Text.StringBuilder();

            // Header
            csv.AppendLine($"Dashboard Report for {userProfile.BasicInfo.Name}");
            csv.AppendLine($"Generated on: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
            csv.AppendLine("");

            // Metrics
            csv.AppendLine("Metrics");
            csv.AppendLine("Metric,Value,Trend");
            csv.AppendLine($"Total Requisitions,{viewModel.TotalRequisitions},{viewModel.TotalRequisitionsTrend:F2}%");
            csv.AppendLine($"Pending Requisitions,{viewModel.PendingRequisitions},{viewModel.PendingRequisitionsTrend:F2}%");
            csv.AppendLine($"Completed Requisitions,{viewModel.CompletedRequisitions},{viewModel.CompletedRequisitionsTrend:F2}%");
            csv.AppendLine($"This Month,{viewModel.ThisMonthRequisitions},{viewModel.ThisMonthTrend:F2}%");
            csv.AppendLine($"Awaiting Action,{viewModel.AwaitingMyAction},");
            csv.AppendLine($"Average Processing Days,{viewModel.AverageProcessingDays:F1},");
            csv.AppendLine("");

            // Recent Requisitions
            csv.AppendLine("Recent Requisitions");
            csv.AppendLine("ID,Issue Station,Delivery Station,Status,Created Date,Items");

            foreach (var req in viewModel.RecentRequisitions.Take(20))
            {
                csv.AppendLine($"{req.Id},{req.IssueStation},{req.DeliveryStation},{req.StatusDescription},{req.CreatedAt:yyyy-MM-dd},{req.ItemCount}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"Dashboard_Report_{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        #endregion

        #region Legacy Support (for backward compatibility)

        /// <summary>
        /// Legacy method - redirects to enhanced MyRequisitions
        /// </summary>
        [HttpGet]
        [Route("Dashboard/Index")]
        public async Task<IActionResult> LegacyIndex()
        {
            return await Index();
        }

        #endregion
    }

    #region Custom Attributes and Extensions

    /// <summary>
    /// Custom action filter for dashboard analytics
    /// </summary>
    public class DashboardAnalyticsAttribute : Attribute
    {
        public string ActionType { get; set; }

        public DashboardAnalyticsAttribute(string actionType)
        {
            ActionType = actionType;
        }
    }

    /// <summary>
    /// Extension methods for dashboard operations
    /// </summary>
    public static class DashboardExtensions
    {
        public static string GetTrendIcon(this decimal trend)
        {
            return trend >= 0 ? "ri-arrow-up-line" : "ri-arrow-down-line";
        }

        public static string GetTrendColor(this decimal trend)
        {
            return trend >= 0 ? "text-success" : "text-danger";
        }

        public static string GetPriorityBadge(this string priority)
        {
            return priority?.ToLower() switch
            {
                "high" => "badge bg-danger",
                "medium" => "badge bg-warning",
                "low" => "badge bg-info",
                _ => "badge bg-secondary"
            };
        }
    }

    #endregion
}