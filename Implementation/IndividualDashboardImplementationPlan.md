# Individual Dashboard Implementation Plan

## Overview
This document outlines the comprehensive enhancement of the MyRequisitions dashboard to provide a personalized, data-rich experience for individual users based on their role and permissions.

## Analysis of Theme Templates

### Widget Types Available (from widgets.html)
1. **Tile Boxes** - Metric cards with icons and trend indicators
2. **Animated Cards** - Cards with counter animations
3. **Background Color Cards** - Highlighted cards (bg-primary, bg-success, etc.)
4. **Multi-Column CRM Widget** - Wide cards with multiple metrics
5. **Icon-Left Widgets** - Cards with large icons on the left
6. **Job/Info Cards** - Content-rich informational cards

### Dashboard Layouts Analyzed
1. **Analytics Dashboard** - 4-column metric cards + charts
2. **Projects Dashboard** - Mixed layout with overview charts and project lists
3. **CRM Dashboard** - Focus on lead conversion and sales metrics

## Current Dashboard Analysis

### Existing MyRequisitions.cshtml Structure
```html
Row 1: 4 metric cards (3xl-3 columns each)
- Total Requisitions
- Pending Requisitions
- Completed Requisitions (bg-primary)
- Cancelled Requisitions (bg-secondary)

Row 2: Recent Requisitions Table
- Full width (xl-12)
- Basic table with 7 columns
- Static data display
```

### Current Limitations
1. **Static percentages** - No real month-over-month calculations
2. **Limited metrics** - Only basic counts
3. **No action alerts** - Missing urgency indicators
4. **Basic visualization** - No charts or trends
5. **Session-dependent** - Manual payroll extraction

## Enhanced Data Points Strategy

### Primary Metrics (Top Row Cards)
```csharp
// Enhanced metrics with real calculations
public class MyRequisitionsDashboardViewModel
{
    // Current metrics (enhanced)
    public int TotalRequisitions { get; set; }
    public int PendingRequisitions { get; set; }
    public int CompletedRequisitions { get; set; }
    public int CancelledRequisitions { get; set; }

    // New primary metrics
    public int AwaitingMyAction { get; set; }
    public int ThisMonthRequisitions { get; set; }
    public double AverageProcessingDays { get; set; }
    public int OverdueRequisitions { get; set; }

    // Trend calculations
    public decimal TotalRequisitionsTrend { get; set; }
    public decimal PendingRequisitionsTrend { get; set; }
    public decimal CompletedRequisitionsTrend { get; set; }
    public decimal ThisMonthTrend { get; set; }
}
```

### Action Required Section (Priority Alerts)
```csharp
public class ActionRequiredSection
{
    public int PendingReceiptConfirmation { get; set; }
    public int RequiringClarification { get; set; }
    public int OverdueItems { get; set; }
    public int ReadyForCollection { get; set; }
    public List<UrgentRequisition> UrgentRequisitions { get; set; }
}

public class UrgentRequisition
{
    public int RequisitionId { get; set; }
    public string Description { get; set; }
    public int DaysOverdue { get; set; }
    public string ActionRequired { get; set; }
    public string Priority { get; set; } // High, Medium, Low
}
```

### Trend Analysis Data
```csharp
public class TrendAnalysisData
{
    public List<MonthlyRequisitionData> RequisitionsByMonth { get; set; }
    public Dictionary<string, int> RequisitionsByCategory { get; set; }
    public List<TopRequestedItem> TopRequestedItems { get; set; }
    public Dictionary<string, int> StatusDistribution { get; set; }
    public List<ProcessingTimeData> ProcessingTimes { get; set; }
}

public class MonthlyRequisitionData
{
    public string Month { get; set; }
    public int Year { get; set; }
    public int Count { get; set; }
    public int Completed { get; set; }
    public int Pending { get; set; }
    public int Cancelled { get; set; }
}

public class TopRequestedItem
{
    public string ItemCode { get; set; }
    public string ItemName { get; set; }
    public string Category { get; set; }
    public int RequestCount { get; set; }
    public int TotalQuantity { get; set; }
}
```

## Proposed Dashboard Layout

### Layout Structure (Enhanced)
```html
<!-- Enhanced 4-card top row -->
<div class="row">
    <!-- Card 1: Total Requisitions (Enhanced with real trends) -->
    <div class="col-xl-3 col-md-6">
        <div class="card card-animate">
            <!-- Icon: clipboard, Real trend calculation -->
        </div>
    </div>

    <!-- Card 2: Awaiting My Action (New - High Priority) -->
    <div class="col-xl-3 col-md-6">
        <div class="card card-animate bg-warning">
            <!-- Icon: alert-circle, Action count -->
        </div>
    </div>

    <!-- Card 3: This Month's Requests (New) -->
    <div class="col-xl-3 col-md-6">
        <div class="card card-animate bg-primary">
            <!-- Icon: calendar, Month count with trend -->
        </div>
    </div>

    <!-- Card 4: Average Processing Time (New) -->
    <div class="col-xl-3 col-md-6">
        <div class="card card-animate">
            <!-- Icon: clock, Average days -->
        </div>
    </div>
</div>

<!-- Action Required Section (New Priority Alert) -->
<div class="row" id="action-required-section" style="display:none;">
    <div class="col-12">
        <div class="alert alert-warning border-0 rounded-0 m-0 d-flex align-items-center">
            <i class="ri-alert-triangle-line text-warning me-2 icon-sm"></i>
            <div class="flex-grow-1">
                You have <strong>@Model.AwaitingMyAction</strong> requisitions requiring your attention.
            </div>
            <div class="flex-shrink-0">
                <button class="btn btn-sm btn-warning" onclick="showActionItems()">View Actions</button>
            </div>
        </div>
    </div>
</div>

<!-- Quick Stats Grid (Enhanced CRM-style widget) -->
<div class="row">
    <div class="col-xl-12">
        <div class="card crm-widget">
            <div class="card-body p-0">
                <div class="row row-cols-md-6 row-cols-1">
                    <div class="col col-lg border-end">
                        <div class="py-4 px-3">
                            <h5 class="text-muted text-uppercase fs-13">Completed This Month</h5>
                            <div class="d-flex align-items-center">
                                <div class="flex-shrink-0">
                                    <i class="ri-checkbox-circle-line display-6 text-success"></i>
                                </div>
                                <div class="flex-grow-1 ms-3">
                                    <h2 class="mb-0"><span class="counter-value" data-target="@Model.CompletedThisMonth">0</span></h2>
                                </div>
                            </div>
                        </div>
                    </div>
                    <!-- Similar pattern for 5 more stats -->
                </div>
            </div>
        </div>
    </div>
</div>

<!-- Two-column layout: Recent Requisitions + Charts -->
<div class="row">
    <!-- Recent Requisitions (Enhanced) -->
    <div class="col-xl-8">
        <div class="card">
            <div class="card-header align-items-center d-flex">
                <h4 class="card-title mb-0 flex-grow-1">Recent Requisitions</h4>
                <div class="flex-shrink-0">
                    <button type="button" class="btn btn-soft-primary btn-sm">
                        <i class="ri-filter-line"></i> Filter
                    </button>
                </div>
            </div>
            <div class="card-body">
                <!-- Enhanced table with action buttons and status timeline -->
            </div>
        </div>
    </div>

    <!-- Charts & Quick Actions -->
    <div class="col-xl-4">
        <!-- Status Distribution Chart -->
        <div class="card">
            <div class="card-header">
                <h5 class="card-title">Status Distribution</h5>
            </div>
            <div class="card-body">
                <canvas id="statusChart"></canvas>
            </div>
        </div>

        <!-- Quick Actions -->
        <div class="card">
            <div class="card-header">
                <h5 class="card-title">Quick Actions</h5>
            </div>
            <div class="card-body">
                <div class="d-grid gap-2">
                    <a href="@Url.Action("Create", "Requisitions")" class="btn btn-primary">
                        <i class="ri-add-line"></i> New Requisition
                    </a>
                    <a href="@Url.Action("Index", "Requisitions")" class="btn btn-outline-primary">
                        <i class="ri-list-check"></i> View All
                    </a>
                    <button class="btn btn-outline-secondary" onclick="downloadReport()">
                        <i class="ri-download-line"></i> Download Report
                    </button>
                </div>
            </div>
        </div>
    </div>
</div>

<!-- Monthly Trend Chart -->
<div class="row">
    <div class="col-xl-12">
        <div class="card">
            <div class="card-header border-0 align-items-center d-flex">
                <h4 class="card-title mb-0 flex-grow-1">Requisition Trends</h4>
                <div>
                    <button type="button" class="btn btn-soft-secondary btn-sm">3M</button>
                    <button type="button" class="btn btn-soft-secondary btn-sm">6M</button>
                    <button type="button" class="btn btn-soft-primary btn-sm">1Y</button>
                </div>
            </div>
            <div class="card-body">
                <canvas id="trendChart"></canvas>
            </div>
        </div>
    </div>
</div>
```

## UserProfile Service Integration

### Leveraging UserProfile for Dashboard
```csharp
public async Task<MyRequisitionsDashboardViewModel> GetMyRequisitionsDashboardAsync(HttpContext httpContext)
{
    // Use UserProfile service instead of manual session extraction
    var userProfile = await _userProfileService.GetCurrentUserProfileAsync();

    if (userProfile?.BasicInfo?.PayrollNo == null)
    {
        return new MyRequisitionsDashboardViewModel();
    }

    var viewModel = new MyRequisitionsDashboardViewModel
    {
        // User context from profile
        UserInfo = new UserDashboardInfo
        {
            Name = userProfile.BasicInfo.Name,
            Department = userProfile.BasicInfo.Department,
            Station = userProfile.BasicInfo.Station,
            Role = userProfile.BasicInfo.Role,
            LastLogin = userProfile.CacheInfo.LastRefresh
        },

        // Role-based visibility
        CanAccessDepartmentData = userProfile.VisibilityScope.CanAccessAcrossDepartments,
        CanAccessStationData = userProfile.VisibilityScope.CanAccessAcrossStations,
        PermissionLevel = userProfile.VisibilityScope.PermissionLevel
    };

    // Rest of implementation...
}
```

### Role-Based Data Filtering
```csharp
// Filter requisitions based on user's access scope
IQueryable<Requisition> requisitionsQuery = _context.Requisitions;

if (!userProfile.VisibilityScope.CanAccessAcrossDepartments)
{
    // Restrict to user's department only
    requisitionsQuery = requisitionsQuery.Where(r =>
        r.DepartmentId == userProfile.LocationAccess.HomeDepartment.Id ||
        userProfile.LocationAccess.AccessibleDepartmentIds.Contains(r.DepartmentId));
}

if (!userProfile.VisibilityScope.CanAccessAcrossStations)
{
    // Restrict to user's accessible stations
    requisitionsQuery = requisitionsQuery.Where(r =>
        userProfile.LocationAccess.AccessibleStationIds.Contains(r.IssueStationId) ||
        userProfile.LocationAccess.AccessibleStationIds.Contains(r.DeliveryStationId));
}
```

## Enhanced Service Methods

### Primary Dashboard Data Method
```csharp
public async Task<MyRequisitionsDashboardViewModel> GetMyRequisitionsDashboardAsync(HttpContext httpContext)
{
    var userProfile = await _userProfileService.GetCurrentUserProfileAsync();
    if (userProfile?.BasicInfo?.PayrollNo == null)
        return new MyRequisitionsDashboardViewModel();

    var payrollNo = userProfile.BasicInfo.PayrollNo;
    var viewModel = new MyRequisitionsDashboardViewModel();

    // Get user's requisitions with applied filters
    var userRequisitions = await GetFilteredRequisitionsAsync(userProfile);

    // Calculate primary metrics
    await CalculatePrimaryMetricsAsync(viewModel, userRequisitions, payrollNo);

    // Calculate trend data
    await CalculateTrendDataAsync(viewModel, userProfile);

    // Get action required items
    await GetActionRequiredItemsAsync(viewModel, userRequisitions, payrollNo);

    // Get recent requisitions with enhanced data
    await GetRecentRequisitionsAsync(viewModel, userRequisitions);

    // Get chart data
    await GetChartDataAsync(viewModel, userRequisitions);

    return viewModel;
}

private async Task CalculatePrimaryMetricsAsync(MyRequisitionsDashboardViewModel viewModel,
    List<Requisition> userRequisitions, string payrollNo)
{
    var now = DateTime.UtcNow;
    var thisMonth = new DateTime(now.Year, now.Month, 1);
    var lastMonth = thisMonth.AddMonths(-1);

    // Current month metrics
    var thisMonthRequisitions = userRequisitions.Where(r => r.CreatedAt >= thisMonth).ToList();
    var lastMonthRequisitions = userRequisitions.Where(r =>
        r.CreatedAt >= lastMonth && r.CreatedAt < thisMonth).ToList();

    // Primary counts
    viewModel.TotalRequisitions = userRequisitions.Count;
    viewModel.ThisMonthRequisitions = thisMonthRequisitions.Count;
    viewModel.PendingRequisitions = userRequisitions.Count(r =>
        r.Status == RequisitionStatus.NotStarted ||
        r.Status == RequisitionStatus.PendingDispatch ||
        r.Status == RequisitionStatus.PendingReceipt);
    viewModel.CompletedRequisitions = userRequisitions.Count(r =>
        r.Status == RequisitionStatus.Completed);

    // Action required count
    viewModel.AwaitingMyAction = userRequisitions.Count(r =>
        r.Status == RequisitionStatus.PendingReceipt && r.PayrollNo == payrollNo);

    // Calculate trends (month-over-month)
    viewModel.ThisMonthTrend = CalculatePercentageChange(thisMonthRequisitions.Count, lastMonthRequisitions.Count);
    viewModel.PendingRequisitionsTrend = CalculatePercentageChange(
        thisMonthRequisitions.Count(r => r.Status == RequisitionStatus.PendingDispatch),
        lastMonthRequisitions.Count(r => r.Status == RequisitionStatus.PendingDispatch));

    // Average processing time
    var completedRequisitions = userRequisitions.Where(r =>
        r.Status == RequisitionStatus.Completed && r.CompletedAt.HasValue).ToList();

    if (completedRequisitions.Any())
    {
        viewModel.AverageProcessingDays = completedRequisitions
            .Average(r => (r.CompletedAt.Value - r.CreatedAt).TotalDays);
    }
}

private async Task GetActionRequiredItemsAsync(MyRequisitionsDashboardViewModel viewModel,
    List<Requisition> userRequisitions, string payrollNo)
{
    viewModel.ActionRequired = new ActionRequiredSection
    {
        PendingReceiptConfirmation = userRequisitions.Count(r =>
            r.Status == RequisitionStatus.PendingReceipt && r.PayrollNo == payrollNo),
        OverdueItems = userRequisitions.Count(r =>
            r.ExpectedDeliveryDate.HasValue && r.ExpectedDeliveryDate < DateTime.UtcNow &&
            r.Status != RequisitionStatus.Completed),
        UrgentRequisitions = userRequisitions
            .Where(r => r.Priority == "High" && r.Status != RequisitionStatus.Completed)
            .Select(r => new UrgentRequisition
            {
                RequisitionId = r.Id,
                Description = $"Requisition #{r.Id}",
                DaysOverdue = r.ExpectedDeliveryDate.HasValue ?
                    (int)(DateTime.UtcNow - r.ExpectedDeliveryDate.Value).TotalDays : 0,
                ActionRequired = GetActionRequired(r.Status),
                Priority = r.Priority ?? "Medium"
            }).ToList()
    };
}

private async Task GetChartDataAsync(MyRequisitionsDashboardViewModel viewModel,
    List<Requisition> userRequisitions)
{
    // Status distribution for donut chart
    var statusCounts = userRequisitions
        .GroupBy(r => r.Status)
        .ToDictionary(g => g.Key?.GetDescription() ?? "Unknown", g => g.Count());

    viewModel.StatusDistributionData = statusCounts;

    // Monthly trend data for line chart
    var monthlyData = userRequisitions
        .Where(r => r.CreatedAt >= DateTime.UtcNow.AddMonths(-12))
        .GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month })
        .Select(g => new MonthlyRequisitionData
        {
            Month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM"),
            Year = g.Key.Year,
            Count = g.Count(),
            Completed = g.Count(r => r.Status == RequisitionStatus.Completed),
            Pending = g.Count(r => r.Status != RequisitionStatus.Completed && r.Status != RequisitionStatus.Cancelled),
            Cancelled = g.Count(r => r.Status == RequisitionStatus.Cancelled)
        })
        .OrderBy(d => d.Year).ThenBy(d => d.Month)
        .ToList();

    viewModel.TrendData = new TrendAnalysisData
    {
        RequisitionsByMonth = monthlyData
    };
}

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
```

## Controller Enhancements

### Enhanced DashboardController
```csharp
[CustomAuthorize]
public class DashboardController : Controller
{
    private readonly RequisitionContext _context;
    private readonly IEmployeeService _employeeService;
    private readonly IDashboardService _dashboardService;
    private readonly IDepartmentService _departmentService;
    private readonly IUserProfileService _userProfileService; // New dependency

    public DashboardController(
        RequisitionContext context,
        IEmployeeService employeeService,
        IDashboardService dashboardService,
        IDepartmentService departmentService,
        IUserProfileService userProfileService) // Inject UserProfile service
    {
        _context = context;
        _employeeService = employeeService;
        _dashboardService = dashboardService;
        _departmentService = departmentService;
        _userProfileService = userProfileService;
    }

    // Enhanced default dashboard with user profile integration
    public async Task<IActionResult> Index()
    {
        try
        {
            var userProfile = await _userProfileService.GetCurrentUserProfileAsync();

            // Route based on user role and permissions
            if (userProfile?.VisibilityScope?.PermissionLevel >= PermissionLevel.Manager &&
                userProfile.VisibilityScope.CanAccessAcrossDepartments)
            {
                // Redirect managers to department dashboard
                return RedirectToAction("Department");
            }

            // Default to personal dashboard
            var viewModel = await _dashboardService.GetMyRequisitionsDashboardAsync(HttpContext);

            // Add user profile context
            ViewBag.UserProfile = userProfile;
            ViewBag.CanAccessDepartmentDashboard = userProfile?.VisibilityScope?.CanAccessAcrossDepartments ?? false;

            return View("MyRequisitions", viewModel);
        }
        catch (Exception ex)
        {
            // Log error and return basic dashboard
            TempData["Error"] = "Unable to load personalized dashboard. Showing basic view.";
            return View("MyRequisitions", new MyRequisitionsDashboardViewModel());
        }
    }

    // Enhanced My Requisitions Dashboard
    public async Task<IActionResult> MyRequisitions()
    {
        var viewModel = await _dashboardService.GetMyRequisitionsDashboardAsync(HttpContext);
        var userProfile = await _userProfileService.GetCurrentUserProfileAsync();

        ViewBag.UserProfile = userProfile;
        ViewBag.ShowActionAlerts = viewModel.AwaitingMyAction > 0;

        return View(viewModel);
    }

    // Department Dashboard (with role checking)
    [HttpGet]
    public async Task<IActionResult> Department()
    {
        var userProfile = await _userProfileService.GetCurrentUserProfileAsync();

        // Check if user has department access
        if (userProfile?.VisibilityScope?.CanAccessAcrossDepartments != true)
        {
            TempData["Error"] = "You don't have permission to access department dashboard.";
            return RedirectToAction("MyRequisitions");
        }

        var viewModel = await _dashboardService.GetDepartmentDashboardAsync(HttpContext);
        ViewBag.UserProfile = userProfile;

        return View(viewModel);
    }

    // API endpoint for dashboard data refresh
    [HttpGet]
    public async Task<JsonResult> GetDashboardData()
    {
        try
        {
            var viewModel = await _dashboardService.GetMyRequisitionsDashboardAsync(HttpContext);

            return Json(new {
                success = true,
                data = new {
                    totalRequisitions = viewModel.TotalRequisitions,
                    pendingRequisitions = viewModel.PendingRequisitions,
                    awaitingAction = viewModel.AwaitingMyAction,
                    thisMonth = viewModel.ThisMonthRequisitions,
                    trends = new {
                        totalTrend = viewModel.TotalRequisitionsTrend,
                        pendingTrend = viewModel.PendingRequisitionsTrend,
                        monthlyTrend = viewModel.ThisMonthTrend
                    },
                    chartData = viewModel.StatusDistributionData,
                    trendData = viewModel.TrendData?.RequisitionsByMonth
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
    public async Task<JsonResult> GetActionRequiredItems()
    {
        try
        {
            var viewModel = await _dashboardService.GetMyRequisitionsDashboardAsync(HttpContext);

            return Json(new {
                success = true,
                data = viewModel.ActionRequired
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Failed to load action items" });
        }
    }
}
```

## Front-end Enhancements

### Enhanced JavaScript for Interactivity
```javascript
// Dashboard interactivity and real-time updates
class DashboardManager {
    constructor() {
        this.refreshInterval = null;
        this.charts = {};
        this.init();
    }

    init() {
        this.initCounterAnimations();
        this.initCharts();
        this.initActionAlerts();
        this.initRefreshTimer();
        this.bindEvents();
    }

    initCounterAnimations() {
        // Enhanced counter animation with easing
        document.querySelectorAll('.counter-value').forEach(element => {
            const target = parseInt(element.getAttribute('data-target'));
            const duration = 2000;
            const startTime = performance.now();

            const animate = (currentTime) => {
                const elapsed = currentTime - startTime;
                const progress = Math.min(elapsed / duration, 1);

                // Easing function
                const easeOutQuart = 1 - Math.pow(1 - progress, 4);
                const currentValue = Math.floor(easeOutQuart * target);

                element.textContent = currentValue.toLocaleString();

                if (progress < 1) {
                    requestAnimationFrame(animate);
                }
            };

            requestAnimationFrame(animate);
        });
    }

    initCharts() {
        this.initStatusChart();
        this.initTrendChart();
    }

    initStatusChart() {
        const ctx = document.getElementById('statusChart');
        if (!ctx) return;

        // Fetch status distribution data
        this.charts.status = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: Object.keys(window.dashboardData.statusDistribution),
                datasets: [{
                    data: Object.values(window.dashboardData.statusDistribution),
                    backgroundColor: [
                        '#038edc', // Primary
                        '#f7b84b', // Warning
                        '#51d28c', // Success
                        '#f34e4e', // Danger
                        '#564ab1'  // Secondary
                    ]
                }]
            },
            options: {
                responsive: true,
                plugins: {
                    legend: {
                        position: 'bottom'
                    }
                }
            }
        });
    }

    initTrendChart() {
        const ctx = document.getElementById('trendChart');
        if (!ctx) return;

        const trendData = window.dashboardData.trendData;

        this.charts.trend = new Chart(ctx, {
            type: 'line',
            data: {
                labels: trendData.map(d => d.month),
                datasets: [{
                    label: 'Total Requisitions',
                    data: trendData.map(d => d.count),
                    borderColor: '#038edc',
                    backgroundColor: 'rgba(3, 142, 220, 0.1)',
                    fill: true
                }, {
                    label: 'Completed',
                    data: trendData.map(d => d.completed),
                    borderColor: '#51d28c',
                    backgroundColor: 'rgba(81, 210, 140, 0.1)',
                    fill: false
                }]
            },
            options: {
                responsive: true,
                scales: {
                    y: {
                        beginAtZero: true
                    }
                }
            }
        });
    }

    initActionAlerts() {
        const actionSection = document.getElementById('action-required-section');
        if (window.dashboardData.awaitingAction > 0) {
            actionSection.style.display = 'block';
            this.showActionBadge();
        }
    }

    showActionBadge() {
        // Add pulsing animation to action required items
        const badge = document.querySelector('.action-badge');
        if (badge) {
            badge.classList.add('pulse');
        }
    }

    initRefreshTimer() {
        // Auto-refresh dashboard data every 5 minutes
        this.refreshInterval = setInterval(() => {
            this.refreshDashboardData();
        }, 5 * 60 * 1000);
    }

    async refreshDashboardData() {
        try {
            const response = await fetch('/Dashboard/GetDashboardData');
            const result = await response.json();

            if (result.success) {
                this.updateMetrics(result.data);
                this.updateCharts(result.data);
            }
        } catch (error) {
            console.error('Failed to refresh dashboard data:', error);
        }
    }

    updateMetrics(data) {
        // Update counter values
        this.updateCounter('.total-requisitions .counter-value', data.totalRequisitions);
        this.updateCounter('.pending-requisitions .counter-value', data.pendingRequisitions);
        this.updateCounter('.awaiting-action .counter-value', data.awaitingAction);
        this.updateCounter('.this-month .counter-value', data.thisMonth);

        // Update trend indicators
        this.updateTrend('.total-trend', data.trends.totalTrend);
        this.updateTrend('.pending-trend', data.trends.pendingTrend);
    }

    updateCounter(selector, newValue) {
        const element = document.querySelector(selector);
        if (element) {
            const currentValue = parseInt(element.textContent);
            if (currentValue !== newValue) {
                this.animateCounterChange(element, currentValue, newValue);
            }
        }
    }

    animateCounterChange(element, from, to) {
        const duration = 1000;
        const startTime = performance.now();

        const animate = (currentTime) => {
            const elapsed = currentTime - startTime;
            const progress = Math.min(elapsed / duration, 1);

            const currentValue = Math.floor(from + (to - from) * progress);
            element.textContent = currentValue.toLocaleString();

            if (progress < 1) {
                requestAnimationFrame(animate);
            }
        };

        requestAnimationFrame(animate);
    }

    updateTrend(selector, trendValue) {
        const element = document.querySelector(selector);
        if (element) {
            const isPositive = trendValue >= 0;
            const icon = isPositive ? 'ri-arrow-up-line' : 'ri-arrow-down-line';
            const colorClass = isPositive ? 'text-success' : 'text-danger';

            element.innerHTML = `<i class="${icon} align-middle"></i> ${Math.abs(trendValue).toFixed(2)}%`;
            element.className = `badge bg-light ${colorClass} mb-0`;
        }
    }

    bindEvents() {
        // Filter button functionality
        document.querySelector('.filter-btn')?.addEventListener('click', () => {
            this.showFilterModal();
        });

        // Quick action buttons
        document.querySelector('.quick-actions')?.addEventListener('click', (e) => {
            if (e.target.matches('[data-action]')) {
                this.handleQuickAction(e.target.dataset.action);
            }
        });

        // Action required modal
        window.showActionItems = () => {
            this.showActionRequiredModal();
        };
    }

    async showActionRequiredModal() {
        try {
            const response = await fetch('/Dashboard/GetActionRequiredItems');
            const result = await response.json();

            if (result.success) {
                this.displayActionModal(result.data);
            }
        } catch (error) {
            console.error('Failed to load action items:', error);
        }
    }

    displayActionModal(actionData) {
        // Create and show modal with action required items
        const modalHtml = `
            <div class="modal fade" id="actionModal" tabindex="-1">
                <div class="modal-dialog modal-lg">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title">Action Required</h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body">
                            ${this.generateActionList(actionData)}
                        </div>
                    </div>
                </div>
            </div>
        `;

        document.body.insertAdjacentHTML('beforeend', modalHtml);
        const modal = new bootstrap.Modal(document.getElementById('actionModal'));
        modal.show();
    }

    generateActionList(actionData) {
        let html = '<div class="list-group">';

        actionData.urgentRequisitions.forEach(item => {
            html += `
                <div class="list-group-item d-flex justify-content-between align-items-center">
                    <div>
                        <h6 class="mb-1">${item.description}</h6>
                        <p class="mb-1 text-muted">${item.actionRequired}</p>
                        ${item.daysOverdue > 0 ? `<small class="text-danger">${item.daysOverdue} days overdue</small>` : ''}
                    </div>
                    <div>
                        <span class="badge bg-${item.priority.toLowerCase() === 'high' ? 'danger' : 'warning'}">${item.priority}</span>
                        <a href="/Requisitions/Details/${item.requisitionId}" class="btn btn-sm btn-outline-primary ms-2">View</a>
                    </div>
                </div>
            `;
        });

        html += '</div>';
        return html;
    }

    destroy() {
        if (this.refreshInterval) {
            clearInterval(this.refreshInterval);
        }

        Object.values(this.charts).forEach(chart => {
            if (chart) chart.destroy();
        });
    }
}

// Initialize dashboard when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    window.dashboardManager = new DashboardManager();
});

// Cleanup on page unload
window.addEventListener('beforeunload', () => {
    if (window.dashboardManager) {
        window.dashboardManager.destroy();
    }
});
```

## Implementation Steps

### Phase 1: Service Layer Enhancement
1. ✅ **Update IDashboardService interface** with new methods
2. ✅ **Enhance DashboardService implementation** with UserProfile integration
3. ✅ **Add trend calculation methods**
4. ✅ **Create action required data methods**
5. ✅ **Add chart data generation methods**

### Phase 2: ViewModel Enhancement
1. ✅ **Extend MyRequisitionsDashboardViewModel** with new properties
2. ✅ **Create ActionRequiredSection class**
3. ✅ **Create TrendAnalysisData classes**
4. ✅ **Add UserDashboardInfo class**

### Phase 3: Controller Enhancement
1. ✅ **Inject IUserProfileService** into DashboardController
2. ✅ **Update Index action** with role-based routing
3. ✅ **Enhance MyRequisitions action** with user profile context
4. ✅ **Add API endpoints** for AJAX data loading
5. ✅ **Add error handling** and fallback logic

### Phase 4: View Enhancement
1. **Update MyRequisitions.cshtml** with new layout
2. **Add action required alert section**
3. **Implement enhanced metric cards**
4. **Add CRM-style quick stats widget**
5. **Create two-column layout** with charts
6. **Add monthly trend chart section**

### Phase 5: Frontend Enhancement
1. **Create DashboardManager JavaScript class**
2. **Implement Chart.js integration**
3. **Add real-time data refresh**
4. **Create action required modal**
5. **Add interactive filtering**
6. **Implement responsive animations**

### Phase 6: Testing & Optimization
1. **Test with different user roles**
2. **Verify UserProfile integration**
3. **Test trend calculations**
4. **Optimize database queries**
5. **Test responsive design**
6. **Performance testing**

## Role-Based Dashboard Features

### Regular Employee (Default Permission)
- Personal requisition metrics only
- Limited to own requisitions
- Basic action alerts
- Standard charts and trends

### Department Manager (Manager Permission + CanAccessAcrossDepartments)
- Department-wide metrics
- Team member requisitions visibility
- Enhanced action management
- Department trend analysis

### Station Supervisor (Manager Permission + CanAccessAcrossStations)
- Multi-station visibility
- Cross-station requisition tracking
- Station-level analytics
- Resource allocation insights

### System Administrator (Administrator Permission)
- Full system visibility
- Advanced analytics
- System-wide trends
- Administrative controls

## Performance Considerations

### Caching Strategy
```csharp
// Service-level caching for dashboard data
[ResponseCache(Duration = 300, VaryByHeader = "User-Agent")] // 5 minutes
public async Task<IActionResult> MyRequisitions()
{
    var cacheKey = $"dashboard_{User.Identity.Name}_{DateTime.UtcNow:yyyyMMddHH}";
    var cachedData = await _cache.GetAsync(cacheKey);

    if (cachedData == null)
    {
        var viewModel = await _dashboardService.GetMyRequisitionsDashboardAsync(HttpContext);
        await _cache.SetAsync(cacheKey, viewModel, TimeSpan.FromMinutes(5));
        return View(viewModel);
    }

    return View(cachedData);
}
```

### Database Optimization
```csharp
// Optimized queries with proper indexing
var userRequisitions = await _context.Requisitions
    .Where(r => r.PayrollNo == payrollNo)
    .Include(r => r.RequisitionItems.Take(5)) // Limit related data
    .Select(r => new { // Project only needed fields
        r.Id,
        r.Status,
        r.CreatedAt,
        r.CompletedAt,
        r.ExpectedDeliveryDate,
        r.Priority,
        ItemCount = r.RequisitionItems.Count()
    })
    .OrderByDescending(r => r.CreatedAt)
    .Take(100) // Reasonable limit
    .ToListAsync();
```

### Progressive Loading
```javascript
// Load charts and secondary data after main metrics
document.addEventListener('DOMContentLoaded', async () => {
    // Load primary metrics immediately
    initializePrimaryMetrics();

    // Load charts after a short delay
    setTimeout(async () => {
        await loadChartData();
        initializeCharts();
    }, 500);

    // Load action items in background
    setTimeout(async () => {
        await loadActionItems();
    }, 1000);
});
```

## Security Considerations

### Data Access Control
```csharp
// Ensure users can only see their authorized data
private async Task<List<Requisition>> GetFilteredRequisitionsAsync(UserProfile userProfile)
{
    var query = _context.Requisitions.AsQueryable();

    // Apply role-based filtering
    if (userProfile.VisibilityScope.PermissionLevel == PermissionLevel.Default)
    {
        // Regular users: only their own requisitions
        query = query.Where(r => r.PayrollNo == userProfile.BasicInfo.PayrollNo);
    }
    else if (userProfile.VisibilityScope.PermissionLevel == PermissionLevel.Manager)
    {
        // Managers: their department's requisitions
        if (!userProfile.VisibilityScope.CanAccessAcrossDepartments)
        {
            query = query.Where(r =>
                userProfile.LocationAccess.AccessibleDepartmentIds.Contains(r.DepartmentId));
        }
    }

    return await query.ToListAsync();
}
```

### API Endpoint Security
```csharp
[HttpGet]
[ValidateAntiForgeryToken]
public async Task<JsonResult> GetDashboardData()
{
    // Verify user session and permissions
    var userProfile = await _userProfileService.GetCurrentUserProfileAsync();
    if (userProfile == null)
    {
        return Json(new { success = false, message = "Unauthorized" });
    }

    // Rate limiting could be added here
    // Return filtered data based on user's access level
}
```

This comprehensive implementation plan provides a modern, interactive, and secure individual dashboard that leverages the existing UserProfile service architecture while providing rich visualizations and real-time updates based on user roles and permissions.