# Material-Focused Management Dashboard Layout

## Dashboard Header Section
```
┌─────────────────────────────────────────────────────────────────────────────┐
│                       MATERIAL MANAGEMENT DASHBOARD                        │
│                         [Dynamic Title Based on Access]                    │
├─────────────────────────────────────────────────────────────────────────────┤
│ Filters: [📍 Location] [📂 Category] [📊 Status] [💰 Value] [📅 Period] [🔄] │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Primary KPI Cards Row (4 Cards)
```
┌──────────────────┬──────────────────┬──────────────────┬──────────────────┐
│   TOTAL MATERIALS│  TOTAL VALUE     │ AVAILABLE MATS │ MAINT. ALERTS    │
│                  │                  │                  │                  │
│   50       │     │     145          │       12         │
│   ↗️ +5.2%       │   ↗️ +5.2%      │   📦 Ready       │   ⚠️ Urgent      │
│                  │                  │                  │                  │
│ Widget: KPI Card │ Widget: KPI Card │ Widget: KPI Card │ Widget: KPI Card │
└──────────────────┴──────────────────┴──────────────────┴──────────────────┘
```

## Charts Row 1 (2 Charts)
```
┌─────────────────────────────────────┬─────────────────────────────────────┐
│        O            │                                     │
│                                     │         📊 Doughnut Chart          │
│    📊 Horizontal Bar Chart          │                                     │
│                                     │   • Available (45%)                │
│  HQ Office    ████████████ 120      │   • Assigned (35%)                 │
│  Factory A    ████████ 85           │   • Maintenance (15%)              │
│  Region X     ██████ 65             │   • Retired (5%)                   │
│  Warehouse    ████ 40               │                                     │
│                                     │                                     │
│ Widget: Bar Chart                   │ Widget: Doughnut Chart             │
└─────────────────────────────────────┴─────────────────────────────────────┘
```

## Charts Row 2 (Materials by Category & Status Breakdown) - ✅ IMPLEMENTED
```
┌─────────────────────────────────────┬─────────────────────────────────────┐
│     MATERIALS BY CATEGORY           │       STATUS BREAKDOWN              │
│                                     │                                     │
│    📊 Vertical Bar Chart            │    🍩 Donut Chart + Legend         │
│                                     │                                     │
│  150┤     ███                       │           45%                       │
│  100┤ ███ ███     ███               │      ●────────●                     │
│   50┤ ███ ███ ███ ███ ███           │    ●            ●                   │
│    0└─────────────────────          │  ●   Available    ●                 │
│     IT  Veh Furn Tool Safe          │    ●            ●                   │
│                                     │      ●────────●                     │
│ ApexCharts: Vertical Bar            │  ApexCharts: Donut                  │
│ - Project blue color                │  - BadgeHelper colors               │
│ - Data labels on bars               │  - Horizontal inline legend         │
│ - Tooltip with percentages          │  - Count & percentage display       │
│                                     │                                     │
│                                     │ ● Available: 25 (45%)               │
│                                     │ ● Assigned: 18 (32%)                │
│                                     │ ● Under Maintenance: 8 (14%)        │
│                                     │ ● Disposed: 5 (9%)                  │
│                                     │                                     │
│ Widget: ApexCharts Bar              │ Widget: ApexCharts Donut            │
└─────────────────────────────────────┴─────────────────────────────────────┘
```

**Implementation Status:**
- ✅ Backend: GetMaterialChartData endpoint with category/status grouping
- ✅ Frontend: ApexCharts with consistent project colors from BadgeHelper
- ✅ Filters: Charts update automatically with dashboard filters
- ✅ Responsive: Both charts have balanced heights and mobile-friendly design

## Row 3: Recent Activity & Material Insights (Proposed)
```
┌──────────────────────────────────────────────────────────────┬─────────────────┐
│                    MATERIAL INSIGHTS (3/4 WIDTH)            │  RECENT ACTIVITY│
│                                                              │   (1/4 WIDTH)   │
│  ┌─ Tabs ─────────────────────────────────────────────────┐ │                 │
│  │[Available][In Process][Expired Warranty][Lost][Overdue]│ │  📋 Last 5      │
│  └─────────────────────────────────────────────────────────┘ │  Activities     │
│                                                              │                 │
│  ┌─ Tab Content: Available Materials (Status = Available) ─┐ │ ┌─────────────┐ │
│  │Material ID   │ Name        │ Category │ Location │ Days│ │ │18-Sep│Helmet│ │
│  │MAT-001      │ Dell Laptop │ IT       │ HQ      │ 45  │ │ │17-Sep│Laptop│ │
│  │MAT-015      │ Projector   │ IT       │ Training│ 120 │ │ │16-Sep│Maint.│ │
│  │MAT-032      │ Scanner     │ Office   │ Archive │ 90  │ │ │15-Sep│Return│ │
│  │MAT-048      │ Chair       │ Furniture│ Store   │ 15  │ │ │14-Sep│Assign│ │
│  │[Link: View Details] [Link: Assign] [Link: Transfer]   │ │ └─────────────┘ │
│  └────────────────────────────────────────────────────────┘ │                 │
│                                                              │  Widget:        │
│  Widget: Tabbed Material Tables                             │  Activity List  │
│  - Available: MaterialStatus = 4 (Available)                │  - Shows last 5 │
│  - In Process: MaterialStatus = 6 (InProcess)               │  - Date, Item,  │
│  - Expired Warranty: WarrantyEndDate < Now                  │    Action type  │
│  - Lost Items: MaterialStatus = 2 (LostOrStolen)            │  - Links to     │
│  - Overdue Maintenance: NextMaintenanceDate < Now           │    material     │
│  - Each table has links to /Material/Details/{id}           │    details      │
│  - Action buttons for common operations                     │                 │
└──────────────────────────────────────────────────────────────┴─────────────────┘
```

**Implementation Details:**
- **Left Side (3/4 width)**: Tabbed Material Insights with data tables
- **Right Side (1/4 width)**: Recent Activity summary (similar to Management dashboard)
- **Data Source**: MaterialDashboard view filtered by tab criteria
- **Updates**: Tables refresh when filters change, activities every 30 seconds
- **Responsive**: Tabs stack vertically on mobile, recent activity moves below

**Material Insights Tab Definitions:**
1. **📦 Available** - Materials with Status = Available (MaterialStatus = 4)
2. **⚡ In Process** - Materials with Status = InProcess (MaterialStatus = 6)
3. **⚠️ Expired Warranty** - Materials where WarrantyEndDate < Current Date
4. **❌ Lost Items** - Materials with Status = LostOrStolen (MaterialStatus = 2)
5. **🔧 Overdue Maintenance** - Materials where NextMaintenanceDate < Current Date

**Tab Table Features:**
- **Sortable columns**: Material ID, Name, Category, Location, Days/Status
- **Action links**: View Details, Assign, Transfer, Schedule Maintenance
- **Pagination**: 10 items per page with search functionality
- **Links**: Each material name links to /Material/Details/{MaterialId}
- **Responsive**: Tables scroll horizontally on mobile devices

## Data Tables Section
```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           DETAILED MATERIAL ANALYTICS                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ Tab 1: 📋 HIGH-VALUE MATERIAL TRACKING                                    │
│ ┌─────────────────────────────────────────────────────────────────────────┐ │
│ │Material ID │ Name           │ Value   │ Location │ Status    │ Assigned │ │
│ │MAT-001     │ Dell Laptop    │ 85,000  │ HQ IT    │ Assigned  │ J.Doe    │ │
│ │MAT-002     │ Toyota Hilux   │ 2.5M    │ Field    │ Available │ -        │ │
│ │MAT-003     │ Server Rack    │ 150,000 │ Data Ctr │ Maint.    │ -        │ │
│ └─────────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
│ Tab 2: 🔧 MAINTENANCE SCHEDULE                                             │
│ ┌─────────────────────────────────────────────────────────────────────────┐ │
│ │Material      │ Last Maint. │ Next Due   │ Status   │ Location │ Action  │ │
│ │Generator     │ 15-Jan-25   │ 15-Apr-25  │ Overdue  │ Factory  │ Schedule│ │
│ │Forklift #2   │ 01-Mar-25   │ 01-Jun-25  │ Due Soon │ Warehouse│ Plan    │ │
│ └─────────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
│ Tab 3: 📦 RECENT MATERIAL MOVEMENTS                                       │
│ ┌─────────────────────────────────────────────────────────────────────────┐ │
│ │Date       │ Material       │ From      │ To        │ Type     │ Reason   │ │
│ │18-Sep-25  │ Safety Helmet  │ Warehouse │ Site A    │ Assign   │ New Emp  │ │
│ │17-Sep-25  │ Laptop #45     │ IT Dept   │ Finance   │ Transfer │ Realloc  │ │
│ └─────────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
│ Tab 4: 😴 UNDERUTILIZED ASSETS                                            │
│ ┌─────────────────────────────────────────────────────────────────────────┐ │
│ │Material      │ Days Idle │ Location  │ Value   │ Suggestion │ Action    │ │
│ │Projector #3  │ 120       │ Training  │ 45,000  │ Relocate   │ Transfer  │ │
│ │Scanner #7    │ 90        │ Archive   │ 25,000  │ Reassign   │ Available │ │
│ └─────────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
│ Widget: Tabbed Data Tables with Pagination & Search                        │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Action Items & Alerts Section
```
┌─────────────────────────────────────────────────────────────────────────────┐
│                            🚨 ACTION REQUIRED                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ Priority Alerts:                                                           │
│ • 🔴 8 materials have overdue maintenance (Generator, Crane, etc.)         │
│ • 🟡 23 material warranties expiring within 30 days                        │
│ • 🟠 5 high-value materials unassigned for >30 days                        │
│ • 🔵 12 materials ready for collection at warehouse                         │
│                                                                             │
│ Quick Actions: [Schedule Maintenance] [Assign Materials] [Transfer Assets]  │
│                                                                             │
│ Widget: Alert List with Action Buttons                                     │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Material Intelligence Section (Optional - for Admin level)
```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          📊 MATERIAL INTELLIGENCE                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ ┌─────────────────┬─────────────────┬─────────────────┬─────────────────┐   │
│ │   ROI ANALYSIS  │ USAGE PATTERNS  │ COST EFFICIENCY │  OPTIMIZATION   │   │
│ │                 │                 │                 │  OPPORTUNITIES  │   │
│ │ High ROI: 15    │ Peak: 9-11 AM   │ Cost/Day: 1,250 │ Relocate: 5     │   │
│ │ Medium: 45      │ Low: 2-4 PM     │ Efficiency: 78% │ Retire: 3       │   │
│ │ Low ROI: 8      │ Pattern: Stable │ Trend: ↗️ +5%   │ Acquire: 2      │   │
│ │                 │                 │                 │                 │   │
│ │ Widget: Metric  │ Widget: Metric  │ Widget: Metric  │ Widget: Action  │   │
│ │ Summary         │ Summary         │ Summary         │ Recommendations │   │
│ └─────────────────┴─────────────────┴─────────────────┴─────────────────┘   │
│                                                                             │
│ Widget: Intelligence Cards (4-column layout)                               │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Filter Panel Details (Collapsible Sidebar)
```
┌─────────────────┐
│   🔍 FILTERS    │
├─────────────────┤
│                 │
│ 📍 LOCATION     │
│ ☐ All Access.  │
│ ☑ Head Office  │
│ ☑ Factory A    │
│ ☐ Region X     │
│ ☐ Warehouse    │
│                 │
│ 📂 CATEGORY     │
│ ☑ IT Equipment │
│ ☐ Vehicles     │
│ ☑ Furniture    │
│ ☐ Tools        │
│                 │
│ 📊 STATUS       │
│ ☑ Available    │
│ ☑ Assigned     │
│ ☐ Maintenance  │
│ ☐ Retired      │
│                 │
│ 💰 VALUE RANGE  │
│ Min: [50,000]   │
│ Max: [500,000]  │
│                 │
│ 📅 TIME PERIOD  │
│ ○ Last 30 days │
│ ● Last 90 days │
│ ○ This Year    │
│ ○ Custom       │
│                 │
│ [Apply] [Reset] │
│                 │
│ Widget: Filter  │
│ Sidebar Panel   │
└─────────────────┘
```

---

## Widget/Chart Summary by Type:

### **KPI Cards (4):**
1. Total Material Value - Metric card with trend
2. Available Materials - Count card with status
3. Utilization Rate - Percentage card with indicator
4. Maintenance Alerts - Alert count card

### **Charts (4):**
1. Material Distribution by Location - Horizontal Bar Chart
2. Status Breakdown - Doughnut Chart
3. Material Movement Trends - Multi-series Line Chart
4. Category Utilization - Horizontal Bar Chart (Heatmap style)

### **Status Cards (3):**
1. Warranty Status - Multi-status breakdown
2. Maintenance Due - Status indicators
3. High Value Tracking - Summary metrics

### **Data Tables (4 Tabs):**
1. High-Value Material Tracking - Sortable table
2. Maintenance Schedule - Action-oriented table
3. Recent Material Movements - Activity log
4. Underutilized Assets - Optimization table

### **Intelligence Cards (4 - Admin only):**
1. ROI Analysis - Performance metrics
2. Usage Patterns - Behavioral insights
3. Cost Efficiency - Financial metrics
4. Optimization Opportunities - Action recommendations

### **Interactive Elements:**
- Filter sidebar with multi-select options
- Action buttons for quick operations
- Tabbed data views with search/pagination
- Responsive layout adapting to screen size

This layout prioritizes **material asset management** over requisition processing, providing actionable insights for optimizing material utilization across the organization.

---

## Material Dashboard Implementation Plan

### Phase 1: Database Foundation
**Status: ✅ Complete**

1. **Model Analysis Complete**:
   - Material.cs: Status uses MaterialStatus enum (Available=4, Assigned=5, UnderMaintenance=1, LostOrStolen=2, Disposed=3, InProcess=6)
   - MaterialAssignment.cs: Uses AssignmentDate, RequisitionType enum for AssignmentType, StationId/DepartmentId (int?)
   - MaterialCondition.cs: Separate condition tracking with Condition, FunctionalStatus, CosmeticStatus enums

2. **Database Views Created**:
   - `vw_MaterialDashboard`: Main view leveraging existing LocationHierarchyView for cross-context data
   - `vw_MaterialUtilizationSummary`: Aggregated KPI metrics with station category grouping
   - `vw_MaterialMovementTrends`: 12-month movement analytics using existing views
   - **Key Architecture**: Uses `vw_LocationHierarchy` instead of direct table joins to solve cross-database context issues

### Phase 2: Backend Services Extension

1. **Create Business Logic Service** (`Services/MaterialBusinessLogicService.cs`):
   ✅ **Complete** - Handles all status calculations and business rules
   - Configurable business rules (warranty alerts, maintenance schedules, value thresholds)
   - Extensible enum handling - **zero hardcoded values in database views**
   - Raw data interpretation methods for summary statistics
   - Easy to modify business logic without database changes

2. **Extend IDashboardService** (`Services/IDashboardService.cs`):
   ```csharp
   // Add to existing interface
   Task<MaterialDashboardViewModel> GetMaterialDashboardAsync(HttpContext httpContext);
   Task<MaterialKPIViewModel> GetMaterialKPIsAsync(string? stationFilter = null, string? departmentFilter = null, string? categoryFilter = null);
   Task<List<MaterialChartDataViewModel>> GetMaterialDistributionDataAsync(string groupBy = "location");
   Task<List<MaterialMovementTrendViewModel>> GetMaterialMovementTrendsAsync(int months = 6);
   Task<List<MaterialAlertViewModel>> GetMaterialAlertsAsync();
   ```

3. **Implement in DashboardService** (`Services/DashboardService.cs`):
   - Use existing UserProfile and VisibilityService for role-based filtering
   - Leverage vw_MaterialDashboard view for raw data retrieval
   - Apply MaterialBusinessLogicService for all status calculations
   - Support filtering by visible stations/departments only

### Phase 3: ViewModels Creation

Create new ViewModels in `ViewModels/DashboardViewModel.cs`:

```csharp
public class MaterialDashboardViewModel
{
    public MaterialKPIViewModel KPIs { get; set; }
    public List<MaterialChartDataViewModel> LocationDistribution { get; set; }
    public List<MaterialChartDataViewModel> StatusBreakdown { get; set; }
    public List<MaterialMovementTrendViewModel> MovementTrends { get; set; }
    public List<MaterialTableDataViewModel> HighValueMaterials { get; set; }
    public List<MaterialTableDataViewModel> MaintenanceSchedule { get; set; }
    public List<MaterialMovementTableViewModel> RecentMovements { get; set; }
    public List<MaterialTableDataViewModel> UnderutilizedAssets { get; set; }
    public List<MaterialAlertViewModel> Alerts { get; set; }
    public MaterialFilterOptionsViewModel FilterOptions { get; set; }
    public UserProfileViewModel UserProfile { get; set; }
}

public class MaterialKPIViewModel
{
    public decimal TotalValue { get; set; }
    public string TotalValueFormatted { get; set; }
    public decimal ValueTrend { get; set; }
    public int AvailableMaterials { get; set; }
    public int TotalMaterials { get; set; }
    public decimal UtilizationRate { get; set; }
    public string UtilizationStatus { get; set; }
    public int MaintenanceAlerts { get; set; }
    public string AlertLevel { get; set; }
}

public class MaterialFilterOptionsViewModel
{
    public List<FilterOptionViewModel> Stations { get; set; }
    public List<FilterOptionViewModel> Departments { get; set; }
    public List<FilterOptionViewModel> Categories { get; set; }
    public List<FilterOptionViewModel> StatusOptions { get; set; }
}
```

### Phase 4: Controller Extension

Add to `Controllers/DashboardController.cs`:

```csharp
[Route("Dashboard/Material")]
public async Task<IActionResult> Material()
{
    try
    {
        var userProfile = await _userProfileService.GetUserProfileAsync(HttpContext);

        // Check if user can access material dashboard (same logic as Management)
        if (userProfile.RoleInformation.IsDefaultUser &&
            !userProfile.RoleInformation.CanAccessAcrossStations)
        {
            return View("AccessDenied");
        }

        var materialDashboard = await _dashboardService.GetMaterialDashboardAsync(HttpContext);
        return View(materialDashboard);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error loading Material Dashboard");
        return View("Error");
    }
}

// API endpoints for filtering and real-time updates
[HttpGet("api/material-dashboard/kpis")]
public async Task<IActionResult> GetMaterialKPIs(string? station = null, string? department = null, string? category = null)
{
    var kpis = await _dashboardService.GetMaterialKPIsAsync(station, department, category);
    return Json(kpis);
}

[HttpGet("api/material-dashboard/charts/{chartType}")]
public async Task<IActionResult> GetMaterialChartData(string chartType, string? filters = null)
{
    var data = await _dashboardService.GetMaterialDistributionDataAsync(chartType);
    return Json(data);
}
```

### Phase 5: Frontend Implementation

1. **Create Material Dashboard View** (`Views/Dashboard/Material.cshtml`):
   - Use same layout structure as Management.cshtml
   - Implement the ASCII wireframe layout from above
   - Use existing Chart.js path: `~/vendors/chart.js/Chart.min.js`
   - Create MaterialDashboard JavaScript class for interactivity

2. **Add Sidebar Link** (`Views/Shared/_Sidebar.cshtml`):
   ```html
   <li class="nav-item">
       <a href="/Dashboard/Material" class="nav-link @(currentController == "Dashboard" && currentAction == "Material" ? "active" : "")" data-key="t-material-dashboard">Material Assets</a>
   </li>
   ```

3. **JavaScript Implementation**:
   - MaterialDashboard class with filter management
   - Real-time chart updates via API endpoints
   - Advanced filtering with multi-select dropdowns
   - Responsive layout for different screen sizes

### Phase 6: Key Features Implementation

1. **Advanced Filtering System**:
   - **Location-based filtering**: Uses `vw_LocationHierarchy` and `vw_StationDetails` for filter options
   - **Hierarchical filtering**: Station category → Station → Department cascade
   - **Cross-context support**: Leverages existing views that solve KTDALeave vs Requisition context issues
   - Category and subcategory filtering from MaterialCategories
   - Status-based filtering (Available, Assigned, Maintenance, etc.)
   - Value range filtering (Low, Medium, High value materials)
   - Time period filtering for trends

2. **Smart Data Analytics**:
   - Utilization scoring (0-100) based on movement frequency
   - ROI analysis for high-value assets
   - Maintenance scheduling optimization
   - Underutilization detection (idle assets >30 days)

3. **Actionable Insights**:
   - Maintenance alerts (overdue, due soon)
   - Warranty expiration warnings
   - Underutilized asset recommendations
   - High-value asset tracking

4. **Role-Based Access Control**:
   - Same UserProfile and VisibilityService integration
   - Admin: Full organizational view
   - Role Groups: Cross-station/department access based on permissions
   - Default Users: Own station/department materials only

### Technical Implementation Notes

1. **Database Performance**:
   - Use existing indexes on MaterialAssignments and Materials
   - Views are optimized with proper JOINs and CTEs
   - Consider adding specific indexes for dashboard queries

2. **Caching Strategy**:
   - Cache filter options (categories, stations, departments)
   - Cache KPI calculations for 5-10 minutes
   - Real-time updates only for alerts and movement data

3. **Error Handling**:
   - Graceful fallbacks for missing data
   - Proper error logging and user feedback
   - Access denied handling for unauthorized users

4. **Mobile Responsiveness**:
   - Collapsible filter sidebar
   - Stacked chart layout on mobile
   - Touch-friendly interactions
   - Optimized data tables with horizontal scroll

This implementation extends the existing dashboard infrastructure while focusing specifically on material asset management rather than requisition processing.

---

## Configuration Example (appsettings.json)

```json
{
  "MaterialRules": {
    "WarrantyAlertDays": 30,
    "MaintenanceAlertDays": 7,
    "UnderutilizedDays": 30,
    "HighValueThreshold": 100000,
    "MediumValueThreshold": 10000,
    "NewAssetMonths": 12,
    "ModerateAssetYears": 3
  }
}
```

## Key Benefits of New Architecture

### ✅ **Extensibility**
- **Zero hardcoded enum values** in database views - completely eliminated!
- Adding new MaterialStatus enum values requires no database changes
- Business rules are configurable via appsettings.json
- Easy to modify thresholds without code deployment
- Raw data approach allows unlimited business logic evolution

### ✅ **Maintainability**
- All business logic centralized in MaterialBusinessLogicService
- Database views contain only raw data - no hardcoded logic
- Clear separation of concerns

### ✅ **Testability**
- Business logic can be unit tested independently
- Configurable rules make testing different scenarios easy
- No database dependencies for business rule testing

### ✅ **Performance**
- Database views optimized for data retrieval only
- Business logic calculated in memory (faster than SQL CASE statements)
- Cacheable business rule configurations