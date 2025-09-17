# Management Dashboard Implementation Plan

## Overview
The Management Dashboard is a **separate, adaptive, role-based dashboard** that provides contextual organizational insights based on the user's permission level within the MRIV system. This is distinct from the "My Dashboard" which shows personal user data regardless of role.

### **Two Dashboard Approach:**
1. **My Dashboard** (`/Dashboard/MyRequisitions`) - Personal data for logged-in user (already implemented)
2. **Management Dashboard** (`/Dashboard/Management`) - Role-based organizational data (to be implemented)

The Management Dashboard dynamically adjusts content, metrics, and visualizations according to the user's role group access permissions using the fixed UserProfile and VisibilityService logic.

## Role-Based Access Matrix for Management Dashboard

| User Type | CanAccessAcrossStations | CanAccessAcrossDepartments | IsDefaultUser | Management Dashboard Access | Dashboard Context |
|-----------|:----------------------:|:-------------------------:|:-------------:|----------------------------|-------------------|
| **Default User** | false | false | **true** | ❌ **No Access** | Redirected to My Dashboard |
| **Department Manager** | FALSE | FALSE | **false** | ✅ Department at station | Department Management View |
| **Station Manager** | FALSE | TRUE | **false** | ✅ All departments at station | Station Management View |
| **General Manager** | TRUE | FALSE | **false** | ✅ Department at all stations | Cross-Station Department View |
| **Administrator** | TRUE | TRUE | **false** | ✅ All data everywhere | Organization Management View |

**Key Rule**: Default Users (not in any role group) do **NOT** get access to Management Dashboard - they are redirected to their personal "My Dashboard".

## Dashboard Architecture

### 1. Dynamic Dashboard Title & Context

#### Title Generation Logic:
```csharp
string GetDashboardTitle(UserProfile userProfile)
{
    var scope = userProfile.VisibilityScope;

    if (scope.CanAccessAcrossStations && scope.CanAccessAcrossDepartments)
        return "Organization Management Dashboard";
    else if (scope.CanAccessAcrossStations && !scope.CanAccessAcrossDepartments)
        return $"{userProfile.BasicInfo.Department} Management Dashboard - All Stations";
    else if (!scope.CanAccessAcrossStations && scope.CanAccessAcrossDepartments)
        return $"{userProfile.BasicInfo.Station} Station Management Dashboard";
    else if (userProfile.RoleInformation.RoleGroups.Any())
        return $"{userProfile.BasicInfo.Department} Department Dashboard - {userProfile.BasicInfo.Station}";
    else
        return "My Personal Dashboard";
}
```

### 2. Data Points by Role Level

#### 2.1 Default User (Personal Dashboard)
**Scope**: Own requisitions and personal activity only

**Primary Metrics:**
- My Total Requisitions
- My Pending Requisitions
- My Completed Requisitions
- My This Month Requisitions
- My Average Processing Time
- My Overdue Items

**Material Data:**
- Materials assigned to me
- Materials I've requested
- My material return reminders

**Charts:**
- My Requisition Status Distribution
- My Monthly Activity Trend
- My Request Categories

**Action Items:**
- My pending confirmations
- Items ready for my collection
- My overdue returns

#### 2.2 Department Manager (Department Dashboard)
**Scope**: Own department at own station

**Primary Metrics:**
- Department Total Requisitions
- Department Pending Actions
- Department This Month Activity
- Department Completion Rate
- Department Average Processing Time
- Department Overdue Items

**Material Data:**
- Department material inventory at station
- Department material utilization rates
- Department materials due for maintenance
- Department material value/count

**Requisition Analytics:**
- Department status distribution
- Department workflow bottlenecks
- Department top requesters
- Department spending trends

**Comparison Features:**
- None (single department view)
- Internal team performance
- Month-over-month trends

#### 2.3 Station Manager (Station Dashboard)
**Scope**: All departments at own station

**Primary Metrics:**
- Station Total Requisitions
- Station Pending Cross-Dept Actions
- Station This Month Activity
- Station Overall Completion Rate
- Station Average Processing Time
- Station Overdue Items

**Department Comparison:**
- Department performance ranking at station
- Cross-department resource sharing
- Department collaboration metrics
- Resource allocation efficiency

**Material Data:**
- Station-wide material inventory
- Department material distribution
- Cross-department material transfers
- Station material utilization

**Charts:**
- Department comparison charts
- Station workflow analysis
- Cross-departmental trends
- Resource utilization matrix

#### 2.4 General Manager (Cross-Station Department Dashboard)
**Scope**: Own department across all stations

**Primary Metrics:**
- Department Total (All Stations)
- Department Cross-Station Coordination
- Department Network Activity
- Department Global Completion Rate
- Department Cross-Station Processing Time
- Department Network Overdue Items

**Station Comparison:**
- Department performance by station
- Station ranking for department
- Cross-station resource optimization
- Geographic performance analysis

**Material Data:**
- Department materials across all stations
- Station-wise material distribution
- Cross-station material transfers
- Department global inventory

**Charts:**
- Station comparison for department
- Geographic performance heat map
- Cross-station trend analysis
- Resource distribution mapping

#### 2.5 Administrator (Organization Dashboard)
**Scope**: All data everywhere

**Primary Metrics:**
- Organization Total Requisitions
- Organization Pending Critical Actions
- Organization This Month Activity
- Organization Overall Performance
- Organization Average Efficiency
- Organization Critical Issues

**Full Analytics:**
- Department × Station matrix
- Top performing entities
- Organization-wide bottlenecks
- Strategic resource allocation

**Material Data:**
- Complete organization inventory
- Global material distribution
- Organization asset utilization
- Strategic material planning

**Executive Charts:**
- Organization performance matrix
- Strategic trend analysis
- Resource optimization opportunities
- Executive KPI dashboard

### 3. Implementation Strategy: Extend Existing Services

Instead of creating new services, we will **extend the existing DashboardService and DashboardController** to handle the Management Dashboard while keeping the "My Dashboard" logic separate.

#### 3.1 Backend Components

**Extend IDashboardService Interface:**
```csharp
public interface IDashboardService
{
    // Existing methods (unchanged)
    Task<MyRequisitionsDashboardViewModel> GetMyRequisitionsDashboardAsync(HttpContext httpContext);
    Task<DepartmentDashboardViewModel> GetDepartmentDashboardAsync(HttpContext httpContext);

    // NEW: Management Dashboard method
    Task<ManagementDashboardViewModel> GetManagementDashboardAsync(HttpContext httpContext);

    // Existing enhanced methods (unchanged)
    Task<TrendAnalysisData> GetTrendDataAsync(string payrollNo);
    Task<ActionRequiredSection> GetActionRequiredAsync(string payrollNo);
    Task<QuickStatsData> GetQuickStatsAsync(string payrollNo);
}
```

**New ViewModel (Add to existing ViewModels):**
```csharp
public class ManagementDashboardViewModel
{
    // User context
    public UserDashboardInfo UserInfo { get; set; }
    public string DashboardTitle { get; set; }
    public string DashboardContext { get; set; }
    public string AccessLevel { get; set; } // "Department", "Station", "Cross-Station", "Organization"

    // Permission-based metrics
    public ScopeMetrics PrimaryMetrics { get; set; }
    public Dictionary<string, int> StatusDistribution { get; set; }
    public List<TrendDataPoint> TrendAnalysis { get; set; }

    // Comparison data (if applicable)
    public List<ComparisonEntity> ComparisonData { get; set; }
    public bool HasComparisonData => ComparisonData?.Any() == true;

    // Material data
    public MaterialScopeData MaterialData { get; set; }

    // Action items
    public List<ActionItem> ActionRequired { get; set; }

    // Recent activity
    public List<RecentActivity> RecentActivity { get; set; }

    // Access control
    public bool IsDefaultUser { get; set; }
    public bool CanAccessManagementDashboard => !IsDefaultUser;
}

public class ScopeMetrics
{
    public int TotalRequisitions { get; set; }
    public int PendingActions { get; set; }
    public int ThisMonthActivity { get; set; }
    public decimal CompletionRate { get; set; }
    public double AverageProcessingTime { get; set; }
    public int OverdueItems { get; set; }
}

public class ComparisonEntity
{
    public string Name { get; set; }
    public string Type { get; set; } // "Department" or "Station"
    public ScopeMetrics Metrics { get; set; }
    public string PerformanceIndicator { get; set; }
}

public class MaterialScopeData
{
    public int TotalMaterials { get; set; }
    public decimal TotalValue { get; set; }
    public Dictionary<string, int> MaterialByCategory { get; set; }
    public Dictionary<string, int> MaterialByCondition { get; set; }
    public List<MaterialAlert> Alerts { get; set; }
}
```

**Extended DashboardService Implementation:**
```csharp
public class DashboardService : IDashboardService
{
    // Existing constructor and fields unchanged...

    // NEW: Management Dashboard Implementation
    public async Task<ManagementDashboardViewModel> GetManagementDashboardAsync(HttpContext httpContext)
    {
        var userProfile = await _userProfileService.GetCurrentUserProfileAsync();

        // Default users cannot access Management Dashboard
        if (userProfile.VisibilityScope.IsDefaultUser)
        {
            return new ManagementDashboardViewModel
            {
                IsDefaultUser = true,
                DashboardTitle = "Access Denied",
                DashboardContext = "Default users can only access personal dashboard"
            };
        }

        // Admin bypass - see everything
        if (userProfile.RoleInformation.IsAdmin)
            return await BuildOrganizationDashboard(userProfile);

        // Role-based dashboard building
        if (userProfile.VisibilityScope.CanAccessAcrossStations && userProfile.VisibilityScope.CanAccessAcrossDepartments)
            return await BuildOrganizationDashboard(userProfile);
        else if (userProfile.VisibilityScope.CanAccessAcrossStations)
            return await BuildCrossStationDepartmentDashboard(userProfile);
        else if (userProfile.VisibilityScope.CanAccessAcrossDepartments)
            return await BuildStationDashboard(userProfile);
        else
            return await BuildDepartmentDashboard(userProfile);
    }

    // Helper methods for building different dashboard views
    private async Task<ManagementDashboardViewModel> BuildDepartmentDashboard(UserProfile userProfile) { ... }
    private async Task<ManagementDashboardViewModel> BuildStationDashboard(UserProfile userProfile) { ... }
    private async Task<ManagementDashboardViewModel> BuildCrossStationDepartmentDashboard(UserProfile userProfile) { ... }
    private async Task<ManagementDashboardViewModel> BuildOrganizationDashboard(UserProfile userProfile) { ... }
}
```

#### 3.2 Controller Extensions

**Extend Existing DashboardController:**
```csharp
public class DashboardController : Controller
{
    // Existing methods unchanged...

    // NEW: Management Dashboard Action
    [HttpGet]
    [Route("Dashboard/Management")]
    public async Task<IActionResult> Management()
    {
        try
        {
            var userProfile = await _userProfileService.GetCurrentUserProfileAsync();

            // Default users cannot access Management Dashboard
            if (userProfile.VisibilityScope.IsDefaultUser)
            {
                TempData["Warning"] = "You don't have access to Management Dashboard. Showing your personal dashboard instead.";
                return RedirectToAction("MyRequisitions");
            }

            var viewModel = await _dashboardService.GetManagementDashboardAsync(HttpContext);

            // Set ViewBag data for breadcrumbs and context
            ViewBag.UserProfile = userProfile;
            ViewBag.PageTitle = viewModel.DashboardTitle;
            ViewBag.AccessLevel = viewModel.AccessLevel;

            ViewBag.Breadcrumbs = new[]
            {
                new { Name = "Home", Url = "/" },
                new { Name = "Management Dashboard", Url = "/Dashboard/Management" }
            };

            return View("Management", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Management Dashboard");
            TempData["Error"] = "Unable to load management dashboard.";
            return RedirectToAction("MyRequisitions");
        }
    }

    // Enhanced Index routing
    public async Task<IActionResult> Index()
    {
        var userProfile = await _userProfileService.GetCurrentUserProfileAsync();

        // Route based on user capabilities
        if (userProfile.VisibilityScope.IsDefaultUser)
        {
            // Default users only see personal dashboard
            return RedirectToAction("MyRequisitions");
        }
        else if (userProfile.VisibilityScope.CanAccessAcrossDepartments || userProfile.VisibilityScope.CanAccessAcrossStations || userProfile.RoleInformation.IsAdmin)
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
}
```

#### 3.3 Frontend Components

**New Management Dashboard View (`Views/Dashboard/Management.cshtml`):**
- Adaptive layout based on `@Model.AccessLevel`
- Role-specific chart configurations
- Conditional comparison sections
- Dynamic data visualizations

**Navigation Updates:**
- Add Management Dashboard link (conditional on permissions)
- Separate routing for `/Dashboard/MyRequisitions` and `/Dashboard/Management`
- Role-based menu visibility

### 4. Technical Implementation Steps

#### Phase 1: Backend Extensions (Using Existing Infrastructure)
1. **Extend IDashboardService** - Add `GetManagementDashboardAsync()` method
2. **Create ManagementDashboardViewModel** - Add to existing ViewModels folder
3. **Implement dashboard builders** in DashboardService:
   - `BuildDepartmentDashboard()` - Department managers
   - `BuildStationDashboard()` - Station managers
   - `BuildCrossStationDepartmentDashboard()` - General managers
   - `BuildOrganizationDashboard()` - Administrators
4. **Leverage fixed VisibilityService** - Use `ApplyVisibilityScopeWithProfile()` for data filtering
5. **Integrate material data** - Extend existing material service integration
6. **Add comparison logic** - Multi-entity comparison for managers

#### Phase 2: Controller Extensions (Extend Existing DashboardController)
1. **Add Management action** - New `/Dashboard/Management` route
2. **Update Index routing** - Smart routing based on user permissions
3. **Add authorization checks** - Prevent default user access
4. **Implement error handling** - Graceful fallbacks to personal dashboard
5. **Add API endpoints** - Real-time data refresh for management dashboard

#### Phase 3: Frontend Implementation (New View + Updates)
1. **Create Management.cshtml view** - Adaptive layout for different access levels
2. **Update navigation** - Conditional management dashboard link
3. **Implement role-based charts** - Different visualizations per access level
4. **Add comparison components** - Department/station comparison widgets
5. **Update layout** - Shared components between dashboards

#### Phase 4: Integration & Testing
1. **Integration testing** - Test all role combinations
2. **Performance optimization** - Cache management data
3. **Error handling** - Comprehensive error scenarios
4. **Documentation** - Usage guides for different roles

### 5. Security Considerations

- **Default User Protection**: Default users automatically redirected away from Management Dashboard
- **VisibilityService Integration**: All data queries use `ApplyVisibilityScopeWithProfile()` with fixed logic
- **Role-based UI**: Management dashboard link only shown to users with role groups
- **API Security**: Management endpoints validate `!IsDefaultUser` before processing
- **Data Isolation**: Each access level sees only their permitted scope
- **Graceful Degradation**: Unauthorized access redirects to personal dashboard with notification

### 6. Key Benefits of Extended Services Approach

#### 6.1 **Reuse & Consistency**
- Leverages existing `UserProfileService` and `VisibilityService` with our fixes
- Maintains consistent patterns with `MyRequisitions` dashboard
- Uses same DI container, logging, error handling

#### 6.2 **Clear Separation of Concerns**
- **My Dashboard** (`/Dashboard/MyRequisitions`) = Personal data, all users
- **Management Dashboard** (`/Dashboard/Management`) = Organizational data, role groups only
- Both use same underlying services but different data filtering logic

#### 6.3 **Performance & Maintenance**
- Single codebase for dashboard logic
- Shared ViewModels and utilities
- Consistent caching strategy
- Easier testing and debugging

#### 6.4 **User Experience**
- Intuitive routing based on permissions
- Consistent UI/UX between dashboards
- Clear access control messaging
- Seamless navigation between personal and management views

### 6. Testing Strategy

#### Unit Tests:
- Permission-based data filtering
- Dashboard title generation logic
- Scope metrics calculation
- Comparison data generation

#### Integration Tests:
- End-to-end role-based access
- Data consistency across permission levels
- Performance with large datasets
- Material data integration

#### User Acceptance Tests:
- Role-specific dashboard experience
- Data accuracy for each permission level
- UI responsiveness and usability
- Export and sharing functionality

### 7. Performance Considerations

- Implement caching for frequently accessed data
- Use UserProfile cache for permission checks
- Optimize queries for large datasets
- Implement pagination for comparison views
- Use lazy loading for non-critical components

### 8. Routes & Navigation Summary

#### 8.1 **Dashboard Routes**
```
/Dashboard/                    → Smart routing based on user permissions
/Dashboard/MyRequisitions      → Personal dashboard (all users)
/Dashboard/Management          → Management dashboard (role groups only)
/Dashboard/Department          → Legacy department view (may be deprecated)
```

#### 8.2 **Navigation Logic**
```csharp
// In DashboardController.Index()
if (userProfile.VisibilityScope.IsDefaultUser)
    return RedirectToAction("MyRequisitions");
else if (hasManagementCapabilities)
    return RedirectToAction("Management");
else
    return RedirectToAction("MyRequisitions");
```

#### 8.3 **Access Control Summary**
| User Type | My Dashboard Access | Management Dashboard Access | Default Route |
|-----------|:-------------------:|:---------------------------:|:-------------:|
| Default User | ✅ Always | ❌ Redirect to My Dashboard | `/MyRequisitions` |
| Department Manager | ✅ Always | ✅ Department View | `/MyRequisitions` |
| Station Manager | ✅ Always | ✅ Station View | `/Management` |
| General Manager | ✅ Always | ✅ Cross-Station View | `/Management` |
| Administrator | ✅ Always | ✅ Organization View | `/Management` |

### 9. Implementation Priority

**Phase 1 (High Priority)**: Backend service extension and basic Management Dashboard
**Phase 2 (Medium Priority)**: Enhanced UI and comparison features
**Phase 3 (Low Priority)**: Advanced analytics and reporting features

## Conclusion

The Management Dashboard extends existing services to provide role-aware organizational insights while maintaining clear separation from personal "My Dashboard" functionality. By leveraging the fixed UserProfile and VisibilityService logic, it ensures proper security boundaries while delivering contextually appropriate management data for each user's organizational responsibilities.