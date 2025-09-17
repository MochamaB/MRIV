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
        private readonly IDashboardService _dashboardService;
        private readonly IDepartmentService _departmentService;
        private readonly IUserProfileService _userProfileService;

        public DashboardController(
            RequisitionContext context,
            IDashboardService dashboardService,
            IDepartmentService departmentService,
            IUserProfileService userProfileService)
        {
            _context = context;
            _dashboardService = dashboardService;
            _departmentService = departmentService;
            _userProfileService = userProfileService;
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

        // Determine if user should see department dashboard by default
        private bool ShouldShowDepartmentDashboard(UserProfile userProfile)
        {
            return userProfile.VisibilityScope.CanAccessAcrossDepartments &&
                   userProfile.RoleInformation.RoleGroups.Any(rg =>
                       rg.Name.Contains("Manager") || rg.Name.Contains("Supervisor"));
        }

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
    }
}
