# Login Enhancement Refactoring Plan
## Performance-Focused User Profile Caching

### Document Information
- **Version**: 2.0
- **Date**: December 2024
- **Purpose**: Login enhancement implementation plan focused on performance improvements
- **Scope**: User profile caching and session enhancement (without complex permission system)

---

## Executive Summary

### Current Problems
1. **Performance Issues**: Every request requires cross-context database calls for user context
2. **Inefficient Query Pattern**: Repeated employee lookups across all controllers
3. **Session Under-utilization**: Only storing PayrollNo instead of comprehensive user context  
4. **Repeated Cross-Context Calls**: Employee/department/station/rolegroup lookups on every request
5. **No User Context Caching**: Same user data calculated multiple times per session

### Solution Overview
Transform from **reactive runtime user context loading** to **proactive login-time user profile caching**:
- **Login-Time Profile Building**: Calculate comprehensive user profile once at login
- **Session-Based Caching**: Store rich user context in session for fast access
- **Cross-Context Data Integration**: Pre-calculate department names, station names, role information
- **Existing RoleGroup Integration**: Leverage current VisibilityAuthorizeService with cached data

### Expected Benefits
- **90%+ reduction** in user context database calls per request
- **Faster page loads** through cached user profile data
- **Improved user experience** with richer session context
- **Foundation for future enhancements** (action permissions, advanced features)
- **Better scalability** with reduced database load
- **Simplified controller code** with cached user context

---

## Architecture Overview

### Current vs. New Architecture

#### Current Architecture
```
Every Request:
├── Get PayrollNo from session
├── Query EmployeeBkp for user details  
├── Query RoleGroupMembers for user's role groups
├── Query RoleGroups for permission flags
├── Query Department for department name
├── Query Station for station name
└── Apply VisibilityAuthorizeService → Return Results

Database Calls: 5-6 per request
```

#### New Architecture  
```
Login Time (Once):
├── Calculate UserProfile from all contexts
├── Cache in Session + Memory Cache

Every Request:
├── Get UserProfile from session (0 DB calls)
├── Use VisibilityAuthorizeService with cached data
└── Return Results

Database Calls: 0 for user context
```

### User Profile Caching Model

#### UserProfile Structure (In-Memory Model, Not Database Table)
```
UserProfile:
├── BasicInfo: { PayrollNo, Name, Email, Designation, Department, Station }
├── RoleInformation: { SystemRole, RoleGroups[], IsAdmin }
├── LocationAccess: { 
│   ├── HomeDepartment: { Id, Code, Name }
│   ├── HomeStation: { Id, Name }
│   ├── AccessibleDepartmentIds[]
│   └── AccessibleStationIds[]
│   }
├── VisibilityScope: {
│   ├── CanAccessAcrossStations (from RoleGroups)
│   ├── CanAccessAcrossDepartments (from RoleGroups)
│   └── PermissionLevel (Default/Manager/Admin)
│   }
└── CacheInfo: { CreatedAt, ExpiresAt, LastRefresh, Version }
```

#### Integration with Existing VisibilityAuthorizeService
- **No changes to VisibilityAuthorizeService logic**
- **Enhanced with cached data instead of runtime lookups**
- **Same authorization rules, better performance**

---

## Phase 1: Enhanced User Profile & Login System

### Overview
Create comprehensive user profile caching system that calculates and stores all user permissions, accessible data, and context at login time.

### 1.1 User Profile Service Architecture

#### New Components
```
IUserProfileService
├── BuildUserProfileAsync(payrollNo)
├── RefreshUserProfileAsync(payrollNo)
├── GetCachedProfileAsync(payrollNo)
└── InvalidateProfileCacheAsync(payrollNo)

UserProfile (Model)
├── BasicInfo: { PayrollNo, Name, Email, Designation, Department, Station }
├── DisplayPermissions: { AccessibleDepartmentIds[], AccessibleStationIds[], PermissionLevel }
├── ReferencePermissions: { SelectableDepartmentIds[], SelectableStationIds[], CreationContexts[] }
├── ActionPermissions: { ApprovalContexts[], DispatchContexts[], AdminFlags }
├── RoleInformation: { RoleGroups[], PermissionFlags, IsAdmin }
├── LocationAccess: { HomeStation, HomeDepartment, AccessibleLocations[] }
├── WorkflowAccess: { ApprovableSteps[], DispatchableSteps[], ReceivableSteps[] }
├── ReportAccess: { AccessibleReports[], RestrictedReports[] }
├── DataSummary: { TotalRequisitions, PendingApprovals, ActiveMaterials }
└── CacheInfo: { CreatedAt, ExpiresAt, LastRefresh }
```

### 1.2 Comprehensive Profile Building Logic

#### Employee Context Resolution
```csharp
// Cross-context employee data gathering
var employeeData = await BuildEmployeeContextAsync(payrollNo);
// - Basic info from EmployeeBkp
// - Department details with full hierarchy
// - Station information with relationships
// - Supervisor/HOD chains
// - Role and designation information
```

#### Permission Context Calculation
```csharp
// Role group analysis
var roleGroups = await GetActiveRoleGroupsAsync(payrollNo);
var permissionFlags = AnalyzePermissionFlags(roleGroups);

// Calculate three permission tiers
var displayPermissions = CalculateDisplayPermissions(employee, roleGroups);
var referencePermissions = CalculateReferencePermissions(employee, roleGroups);  
var actionPermissions = CalculateActionPermissions(employee, roleGroups);
```

#### Operational Data Summary
```csharp
// Pre-calculate user's operational context
var operationalSummary = await BuildOperationalSummaryAsync(payrollNo);
// - Total requisitions created
// - Pending approvals assigned to user
// - Materials currently assigned to user
// - Recent activity summary
// - Notification counts
// - Dashboard data
```

### 1.3 Enhanced Login Process

#### Updated Login Flow
```csharp
// Current login
HttpContext.Session.SetString("EmployeePayrollNo", payrollNo);

// New enhanced login
var userProfile = await _userProfileService.BuildUserProfileAsync(payrollNo);
await CacheUserProfileAsync(userProfile);
HttpContext.Session.SetString("UserProfileId", userProfile.Id);

// Backward compatibility maintained
HttpContext.Session.SetString("EmployeePayrollNo", payrollNo);
```

#### Profile Caching Strategy
- **Session Storage**: Core profile data for immediate access
- **Memory Cache**: Shared profile data with 30-minute TTL
- **Database Cache**: Persistent profile cache with hourly refresh
- **Invalidation Events**: Role changes, permission updates, administrative actions

### 1.4 Implementation Details

#### New Services
```csharp
public interface IUserProfileService
{
    Task<UserProfile> BuildUserProfileAsync(string payrollNo);
    Task<UserProfile> GetCachedProfileAsync(string payrollNo);
    Task RefreshUserProfileAsync(string payrollNo);
    Task InvalidateProfileAsync(string payrollNo);
    Task<DisplayPermissions> GetDisplayPermissionsAsync(string payrollNo);
    Task<ReferencePermissions> GetReferencePermissionsAsync(string payrollNo);
    Task<ActionPermissions> GetActionPermissionsAsync(string payrollNo);
}

public interface IUserProfileCacheService  
{
    Task SetProfileAsync(UserProfile profile);
    Task<UserProfile> GetProfileAsync(string payrollNo);
    Task InvalidateAsync(string payrollNo);
    Task RefreshAsync(string payrollNo);
}
```

#### Caching Implementation
```csharp
// UserProfile is an in-memory model only - NO database table created
// Caching strategy:
// 1. Session storage: Core profile data for current user session
// 2. Memory cache: Shared profiles across requests (IMemoryCache)
// 3. No persistent database caching - profiles rebuilt from source data as needed

public class UserProfileCacheService : IUserProfileCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IHttpContextAccessor _httpContext;
    
    public async Task SetProfileAsync(UserProfile profile)
    {
        // Session storage for current user
        var session = _httpContext.HttpContext.Session;
        session.SetString($"UserProfile_{profile.BasicInfo.PayrollNo}", 
                         JsonSerializer.Serialize(profile));
        
        // Memory cache for cross-request sharing
        _memoryCache.Set($"UserProfile_{profile.BasicInfo.PayrollNo}", 
                        profile, TimeSpan.FromMinutes(30));
    }
}
```

### 1.5 Backward Compatibility Strategy

#### Gradual Migration Approach
```csharp
// Phase 1: Enhanced login with fallback
public async Task<UserProfile> GetUserContextAsync(string payrollNo)
{
    // Try new enhanced profile first
    var profile = await _userProfileService.GetCachedProfileAsync(payrollNo);
    if (profile != null && !profile.IsExpired)
        return profile;
    
    // Fallback to current approach for compatibility
    return await BuildLegacyUserContextAsync(payrollNo);
}
```

#### Service Wrapper Pattern
```csharp
// Wrapper service to handle both old and new approaches
public class CompatibilityAuthorizationService : IVisibilityAuthorizeService
{
    private readonly IUserProfileService _userProfileService;
    private readonly ILegacyVisibilityService _legacyService;
    
    public async Task<IQueryable<T>> ApplyVisibilityScopeAsync<T>(IQueryable<T> query, string userPayrollNo)
    {
        if (_featureFlags.UseEnhancedAuthorization)
        {
            var profile = await _userProfileService.GetCachedProfileAsync(userPayrollNo);
            return ApplyEnhancedVisibilityAsync(query, profile);
        }
        
        return await _legacyService.ApplyVisibilityScopeAsync(query, userPayrollNo);
    }
}
```

### 1.6 Testing & Validation

#### Profile Building Tests
- Validate permission calculation accuracy
- Test cross-context data consistency  
- Verify caching performance improvements
- Confirm backward compatibility

#### Performance Benchmarks
- Measure login time increase vs. request time decrease
- Track cache hit rates and effectiveness
- Monitor database load reduction
- Validate memory usage patterns

---

## Phase 2: Service Layer Refactoring

### Overview
Refactor all services to utilize cached user profiles while maintaining backward compatibility through feature flags and wrapper patterns.

### 2.1 LocationService Refactoring

#### Current Issues
```csharp
// Current: Returns ALL departments/stations
public async Task<SelectList> GetDepartmentsSelectListAsync(string? selectedId = null)
{
    var departments = await _ktdaContext.Departments.OrderBy(d => d.DepartmentName).ToListAsync();
    return new SelectList(departments, "DepartmentId", "DepartmentName", selectedId);
}
```

#### New Dual-Method Approach
```csharp
// New: Separate methods for different permission contexts
public async Task<SelectList> GetVisibleDepartmentsSelectListAsync(string payrollNo, string? selectedId = null)
{
    var profile = await _userProfileService.GetCachedProfileAsync(payrollNo);
    var departments = await _ktdaContext.Departments
        .Where(d => profile.DisplayPermissions.AccessibleDepartmentIds.Contains(d.DepartmentCode))
        .OrderBy(d => d.DepartmentName)
        .ToListAsync();
    return new SelectList(departments, "DepartmentId", "DepartmentName", selectedId);
}

public async Task<SelectList> GetSelectableDepartmentsSelectListAsync(string payrollNo, string? selectedId = null)
{
    var profile = await _userProfileService.GetCachedProfileAsync(payrollNo);
    var departments = await _ktdaContext.Departments
        .Where(d => profile.ReferencePermissions.SelectableDepartmentIds.Contains(d.DepartmentCode))
        .OrderBy(d => d.DepartmentName)
        .ToListAsync();
    return new SelectList(departments, "DepartmentId", "DepartmentName", selectedId);
}

// Backward compatibility wrapper
public async Task<SelectList> GetDepartmentsSelectListAsync(string? selectedId = null)
{
    if (_featureFlags.UseEnhancedAuthorization && _httpContext.User.Identity.IsAuthenticated)
    {
        var payrollNo = _httpContext.Session.GetString("EmployeePayrollNo");
        return await GetSelectableDepartmentsSelectListAsync(payrollNo, selectedId);
    }
    
    // Legacy implementation
    var departments = await _ktdaContext.Departments.OrderBy(d => d.DepartmentName).ToListAsync();
    return new SelectList(departments, "DepartmentId", "DepartmentName", selectedId);
}
```

### 2.2 VisibilityAuthorizeService Simplification

#### Enhanced Implementation
```csharp
public async Task<IQueryable<T>> ApplyVisibilityScopeAsync<T>(IQueryable<T> query, string userPayrollNo) where T : class
{
    var profile = await _userProfileService.GetCachedProfileAsync(userPayrollNo);
    if (profile == null) return query.Take(0); // Fail secure
    
    // Use cached permissions instead of runtime calculation
    return ApplyPermissionsToQuery(query, profile.DisplayPermissions);
}

private IQueryable<T> ApplyPermissionsToQuery<T>(IQueryable<T> query, DisplayPermissions permissions) where T : class
{
    if (permissions.IsAdmin) return query;
    
    if (typeof(T) == typeof(Requisition))
    {
        var requisitions = query.Cast<Requisition>();
        return requisitions.Where(r => 
            permissions.AccessibleDepartmentIds.Contains(r.DepartmentId) &&
            (permissions.AccessibleStationIds.Contains(r.IssueStationId) || 
             permissions.AccessibleStationIds.Contains(r.DeliveryStationId))
        ).Cast<T>();
    }
    
    // Similar patterns for other entities...
    return query;
}
```

### 2.3 EmployeeService Optimization

#### Cached Lookups Implementation
```csharp
public class EnhancedEmployeeService : IEmployeeService
{
    public async Task<EmployeeBkp> GetEmployeeByPayrollAsync(string payrollNo)
    {
        // Check cache first
        var cachedEmployee = await _cache.GetAsync($"employee_{payrollNo}");
        if (cachedEmployee != null) return cachedEmployee;
        
        // Database lookup with caching
        var employee = await _context.EmployeeBkps.FirstOrDefaultAsync(e => e.PayrollNo == payrollNo);
        if (employee != null)
        {
            await _cache.SetAsync($"employee_{payrollNo}", employee, TimeSpan.FromMinutes(30));
        }
        
        return employee;
    }
    
    public async Task<IEnumerable<EmployeeBkp>> GetEmployeesByLocationAsync(int stationId, int departmentId, string[] roles = null)
    {
        // Use cached location-based lookups from user profile
        var cacheKey = $"employees_{stationId}_{departmentId}_{string.Join("_", roles ?? Array.Empty<string>())}";
        
        var cachedEmployees = await _cache.GetAsync<IEnumerable<EmployeeBkp>>(cacheKey);
        if (cachedEmployees != null) return cachedEmployees;
        
        // Database query with caching
        var employees = await ExecuteLocationQuery(stationId, departmentId, roles);
        await _cache.SetAsync(cacheKey, employees, TimeSpan.FromMinutes(15));
        
        return employees;
    }
}
```

### 2.4 ApprovalService Enhancement

#### Pre-calculated Approval Contexts
```csharp
public async Task<List<ApprovalStepViewModel>> GetUserApprovalsAsync(string payrollNo)
{
    var profile = await _userProfileService.GetCachedProfileAsync(payrollNo);
    
    // Use cached approval contexts instead of runtime calculation
    var approvableSteps = profile.WorkflowAccess.ApprovableSteps;
    
    var approvals = await _context.Approvals
        .Where(a => approvableSteps.Contains(a.StepConfigId.Value))
        .Where(a => profile.DisplayPermissions.AccessibleDepartmentIds.Contains(a.DepartmentId))
        .Include(a => a.Requisition)
        .ToListAsync();
    
    return await ConvertToViewModelsAsync(approvals);
}
```

### 2.5 Incremental Rollout Strategy

#### Feature Flag Implementation
```csharp
public class AuthorizationFeatureFlags
{
    public bool UseEnhancedAuthorization { get; set; } = false;
    public bool UseCachedEmployeeLookups { get; set; } = false;
    public bool UseProfileBasedDropdowns { get; set; } = false;
    public bool EnablePerformanceMonitoring { get; set; } = true;
    public List<string> BetaUsers { get; set; } = new();
}

// Controller-level feature flag usage
[HttpGet]
public async Task<IActionResult> Index()
{
    if (_featureFlags.UseEnhancedAuthorization && 
        (_featureFlags.BetaUsers.Contains(User.Identity.Name) || _featureFlags.UseEnhancedAuthorization))
    {
        return await IndexEnhanced();
    }
    
    return await IndexLegacy();
}
```

#### Service-by-Service Migration
1. **Week 1**: LocationService - Deploy with beta users
2. **Week 2**: EmployeeService - Gradual rollout to departments
3. **Week 3**: VisibilityAuthorizeService - Station-by-station rollout
4. **Week 4**: ApprovalService - Full deployment with monitoring

---

## Phase 3: Controller & UI Integration

### Overview
Update all controllers to use appropriate permission contexts and implement enhanced UI features while maintaining full backward compatibility.

### 3.1 Controller Architecture Updates

#### Enhanced Base Controller
```csharp
public abstract class EnhancedBaseController : Controller
{
    protected readonly IUserProfileService _userProfileService;
    protected readonly AuthorizationFeatureFlags _featureFlags;
    
    protected async Task<UserProfile> GetCurrentUserProfileAsync()
    {
        var payrollNo = HttpContext.Session.GetString("EmployeePayrollNo");
        return await _userProfileService.GetCachedProfileAsync(payrollNo);
    }
    
    protected async Task<bool> CanUserPerformActionAsync(string action, object entity)
    {
        var profile = await GetCurrentUserProfileAsync();
        return _authorizationHelper.CheckActionPermission(profile, action, entity);
    }
    
    protected async Task<IQueryable<T>> ApplyDisplayVisibilityAsync<T>(IQueryable<T> query) where T : class
    {
        if (!_featureFlags.UseEnhancedAuthorization)
        {
            return await _legacyVisibilityService.ApplyVisibilityScopeAsync(query, GetCurrentPayrollNo());
        }
        
        var profile = await GetCurrentUserProfileAsync();
        return _enhancedVisibilityService.ApplyDisplayVisibility(query, profile);
    }
}
```

#### RequisitionsController Refactoring
```csharp
public class RequisitionsController : EnhancedBaseController
{
    public async Task<IActionResult> Index(string tab = "pendingreceipt", int page = 1, int pageSize = 10, string searchTerm = "")
    {
        if (_featureFlags.UseEnhancedAuthorization)
        {
            return await IndexEnhanced(tab, page, pageSize, searchTerm);
        }
        
        return await IndexLegacy(tab, page, pageSize, searchTerm);
    }
    
    private async Task<IActionResult> IndexEnhanced(string tab, int page, int pageSize, string searchTerm)
    {
        var profile = await GetCurrentUserProfileAsync();
        
        // Direct filtered query using cached permissions
        var query = _context.Requisitions
            .Where(r => profile.DisplayPermissions.AccessibleDepartmentIds.Contains(r.DepartmentId))
            .Where(r => profile.DisplayPermissions.AccessibleStationIds.Contains(r.IssueStationId) || 
                       profile.DisplayPermissions.AccessibleStationIds.Contains(r.DeliveryStationId));
        
        // Apply tab filtering
        query = ApplyTabFilter(query, tab);
        
        // Apply search
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = ApplySearchFilter(query, searchTerm);
        }
        
        var totalItems = await query.CountAsync();
        var requisitions = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        // Create view models (simplified - no repeated employee lookups)
        var viewModels = await CreateViewModelsFromCache(requisitions, profile);
        
        return View(viewModels);
    }
    
    // Backward compatibility method
    private async Task<IActionResult> IndexLegacy(string tab, int page, int pageSize, string searchTerm)
    {
        // Original implementation preserved
        var userPayrollNo = HttpContext.Session.GetString("EmployeePayrollNo");
        var query = _context.Requisitions.AsQueryable();
        query = await _visibilityService.ApplyVisibilityScopeAsync(query, userPayrollNo);
        
        // Rest of original logic...
        return View(results);
    }
}
```

### 3.2 Enhanced Form Controls

#### Smart Dropdown Components
```csharp
// New dropdown helper that uses appropriate permission context
public static class EnhancedDropdownHelper
{
    public static async Task<SelectList> GetContextualDepartmentDropdownAsync(
        IUserProfileService profileService, 
        string payrollNo,
        DropdownContext context,
        string selectedId = null)
    {
        var profile = await profileService.GetCachedProfileAsync(payrollNo);
        
        return context switch
        {
            DropdownContext.Display => CreateDropdown(profile.DisplayPermissions.AccessibleDepartmentIds, selectedId),
            DropdownContext.Selection => CreateDropdown(profile.ReferencePermissions.SelectableDepartmentIds, selectedId),
            DropdownContext.Admin => CreateDropdown(profile.GetAllDepartmentIds(), selectedId),
            _ => throw new ArgumentException("Invalid dropdown context")
        };
    }
}

// Usage in views
@Html.DropDownListFor(m => m.DepartmentId, 
    await EnhancedDropdownHelper.GetContextualDepartmentDropdownAsync(
        ViewBag.UserProfileService, 
        ViewBag.CurrentPayrollNo, 
        DropdownContext.Selection),
    "Select Department", 
    new { @class = "form-control" })
```

### 3.3 Dynamic UI Based on Permissions

#### View-Level Permission Checks
```html
@{
    var userProfile = ViewBag.UserProfile as UserProfile;
    var canCreateRequisitions = userProfile?.ActionPermissions.CanCreateRequisitions ?? false;
    var canApprove = userProfile?.ActionPermissions.ApprovalContexts.Any() ?? false;
}

<div class="card-header">
    <div class="d-flex justify-content-between align-items-center">
        <h5>Requisitions</h5>
        
        @if (canCreateRequisitions)
        {
            <a href="@Url.Action("Create")" class="btn btn-primary">
                <i class="ri-add-line"></i> New Requisition
            </a>
        }
    </div>
</div>

<!-- Approval actions shown only for authorized users -->
@if (canApprove)
{
    <div class="approval-actions">
        <button class="btn btn-success" onclick="approveSelected()">Approve Selected</button>
        <button class="btn btn-danger" onclick="rejectSelected()">Reject Selected</button>
    </div>
}
```

#### JavaScript Context Integration
```javascript
// Pass user permissions to client-side
var userPermissions = @Html.Raw(Json.Serialize(userProfile.ActionPermissions));

function canUserPerformAction(action, entityType) {
    return userPermissions.hasPermission(action, entityType);
}

// Dynamic button enabling/disabling
$('.action-button').each(function() {
    var action = $(this).data('action');
    var entityType = $(this).data('entity-type');
    
    if (!canUserPerformAction(action, entityType)) {
        $(this).prop('disabled', true).addClass('disabled');
    }
});
```

### 3.4 Enhanced Filtering & Search

#### Profile-Based Filter Options
```csharp
public async Task<IActionResult> GetFilterOptions()
{
    var profile = await GetCurrentUserProfileAsync();
    
    var filterModel = new FilterOptionsViewModel
    {
        Departments = profile.DisplayPermissions.AccessibleDepartments
            .Select(d => new { d.Id, d.Name })
            .ToList(),
        Stations = profile.DisplayPermissions.AccessibleStations
            .Select(s => new { s.Id, s.Name })
            .ToList(),
        StatusOptions = GetStatusOptionsForUser(profile),
        DateRanges = GetDateRangeOptionsForUser(profile)
    };
    
    return Json(filterModel);
}
```

### 3.5 Performance Monitoring Integration

#### Request Performance Tracking
```csharp
public class PerformanceMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();
        var authorizationCalls = 0;
        var cacheHits = 0;
        
        // Track performance metrics
        context.Items["PerformanceTracker"] = new PerformanceTracker();
        
        await next(context);
        
        stopwatch.Stop();
        
        // Log performance improvements
        _logger.LogInformation(
            "Request {Path} completed in {ElapsedMs}ms. " +
            "Authorization calls: {AuthCalls}, Cache hits: {CacheHits}",
            context.Request.Path,
            stopwatch.ElapsedMilliseconds,
            authorizationCalls,
            cacheHits
        );
    }
}
```

### 3.6 Incremental Deployment Strategy

#### A/B Testing Framework
```csharp
public class FeatureToggleService
{
    public bool ShouldUseEnhancedFeature(string featureName, string userPayrollNo)
    {
        // Check user-specific feature flags
        if (_betaUsers.Contains(userPayrollNo))
            return true;
        
        // Check percentage rollout
        var rolloutPercentage = _configuration.GetValue<int>($"Features:{featureName}:RolloutPercentage");
        var hash = userPayrollNo.GetHashCode();
        return Math.Abs(hash) % 100 < rolloutPercentage;
    }
}

// Usage in controllers
public async Task<IActionResult> Index()
{
    if (_featureToggle.ShouldUseEnhancedFeature("EnhancedAuthorization", GetCurrentPayrollNo()))
    {
        return await IndexEnhanced();
    }
    
    return await IndexLegacy();
}
```

#### Gradual Feature Rollout
1. **Week 1**: Enable for 10% of users, monitor performance
2. **Week 2**: Increase to 25%, collect feedback
3. **Week 3**: Deploy to 50%, validate functionality
4. **Week 4**: Full deployment with legacy fallback maintained

---

## Backward Compatibility Strategy

### 1. Service Interface Preservation
```csharp
// All existing interfaces maintained
public interface IVisibilityAuthorizeService
{
    // Existing methods preserved
    Task<IQueryable<T>> ApplyVisibilityScopeAsync<T>(IQueryable<T> query, string userPayrollNo) where T : class;
    
    // Enhanced methods added
    Task<IQueryable<T>> ApplyDisplayVisibilityAsync<T>(IQueryable<T> query, UserProfile profile) where T : class;
    Task<IQueryable<T>> ApplyReferenceVisibilityAsync<T>(IQueryable<T> query, UserProfile profile) where T : class;
}
```

### 2. Gradual Migration Pattern
```csharp
public class MigrationCompatibleService
{
    public async Task<TResult> ProcessRequestAsync<TResult>(Func<UserProfile, Task<TResult>> enhanced, 
                                                           Func<string, Task<TResult>> legacy,
                                                           string payrollNo)
    {
        if (_featureFlags.UseEnhancedAuthorization)
        {
            try
            {
                var profile = await _userProfileService.GetCachedProfileAsync(payrollNo);
                if (profile != null)
                {
                    return await enhanced(profile);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Enhanced authorization failed, falling back to legacy");
            }
        }
        
        return await legacy(payrollNo);
    }
}
```

### 3. Data Consistency Validation
```csharp
public class ConsistencyValidator
{
    public async Task<ValidationResult> ValidateEnhancedVsLegacyAsync(string payrollNo, string entityType)
    {
        var legacyResults = await _legacyService.GetVisibleEntitiesAsync(payrollNo, entityType);
        var enhancedResults = await _enhancedService.GetVisibleEntitiesAsync(payrollNo, entityType);
        
        return CompareResults(legacyResults, enhancedResults);
    }
}
```

---

## Testing Strategy

### 1. Unit Testing
```csharp
[TestClass]
public class UserProfileServiceTests
{
    [TestMethod]
    public async Task BuildUserProfile_ShouldCalculateCorrectPermissions()
    {
        // Arrange
        var payrollNo = "TEST001";
        var mockEmployee = CreateMockEmployee(payrollNo);
        var mockRoleGroups = CreateMockRoleGroups();
        
        // Act
        var profile = await _userProfileService.BuildUserProfileAsync(payrollNo);
        
        // Assert
        Assert.IsNotNull(profile);
        Assert.AreEqual(payrollNo, profile.BasicInfo.PayrollNo);
        Assert.IsTrue(profile.DisplayPermissions.AccessibleDepartmentIds.Any());
    }
}
```

### 2. Integration Testing
```csharp
[TestClass]
public class AuthorizationIntegrationTests
{
    [TestMethod]
    public async Task EnhancedAuthorization_ShouldMatchLegacyResults()
    {
        // Test that enhanced authorization produces same results as legacy
        var testUsers = GetTestUsers();
        
        foreach (var user in testUsers)
        {
            var legacyResults = await _legacyService.GetVisibleRequisitionsAsync(user.PayrollNo);
            var enhancedResults = await _enhancedService.GetVisibleRequisitionsAsync(user.PayrollNo);
            
            CollectionAssert.AreEquivalent(legacyResults, enhancedResults);
        }
    }
}
```

### 3. Performance Testing
```csharp
[TestMethod]
public async Task PerformanceComparison_EnhancedVsLegacy()
{
    var iterations = 100;
    var testUser = "PERF001";
    
    // Legacy performance
    var legacyStopwatch = Stopwatch.StartNew();
    for (int i = 0; i < iterations; i++)
    {
        await _legacyService.GetVisibleRequisitionsAsync(testUser);
    }
    legacyStopwatch.Stop();
    
    // Enhanced performance  
    var enhancedStopwatch = Stopwatch.StartNew();
    for (int i = 0; i < iterations; i++)
    {
        await _enhancedService.GetVisibleRequisitionsAsync(testUser);
    }
    enhancedStopwatch.Stop();
    
    // Assert significant improvement
    Assert.IsTrue(enhancedStopwatch.ElapsedMilliseconds < legacyStopwatch.ElapsedMilliseconds * 0.5);
}
```

---

## Performance Monitoring

### 1. Key Metrics to Track
- **Authorization Call Reduction**: Measure decrease in database calls per request
- **Response Time Improvement**: Track page load time improvements
- **Cache Hit Rates**: Monitor profile cache effectiveness  
- **Memory Usage**: Ensure caching doesn't cause memory issues
- **Database Load**: Measure reduction in database pressure

### 2. Monitoring Implementation
```csharp
public class AuthorizationPerformanceMonitor
{
    private readonly IMetricsCollector _metrics;
    
    public async Task<T> TrackAuthorizationPerformance<T>(string operation, Func<Task<T>> action)
    {
        using var timer = _metrics.StartTimer($"authorization_{operation}");
        
        try
        {
            var result = await action();
            _metrics.IncrementCounter($"authorization_{operation}_success");
            return result;
        }
        catch (Exception ex)
        {
            _metrics.IncrementCounter($"authorization_{operation}_error");
            throw;
        }
    }
}
```

### 3. Dashboard Metrics
- Real-time performance comparison (Enhanced vs Legacy)
- User adoption rates by department/station
- Error rates and fallback usage
- Cache efficiency and invalidation rates

---

## Risk Management

### 1. Technical Risks
| Risk | Impact | Mitigation |
|------|--------|------------|
| Cache inconsistency | High | Implement cache validation and automatic refresh |
| Memory pressure from caching | Medium | Implement TTL and memory limits |
| Profile build performance | Medium | Optimize with background refresh and progressive loading |
| Legacy compatibility breaks | High | Comprehensive testing and gradual rollout |

### 2. Business Risks
| Risk | Impact | Mitigation |
|------|--------|------------|
| Changed authorization behavior | High | Extensive validation against current behavior |
| User training on new features | Medium | Gradual UI changes and user communication |
| Operational disruption | High | Feature flags and instant rollback capability |

### 3. Security Risks
| Risk | Impact | Mitigation |
|------|--------|------------|
| Permission calculation errors | High | Dual validation and audit logging |
| Cache tampering | Medium | Secure cache implementation and validation |
| Privilege escalation | High | Default to restrictive permissions and thorough testing |

---

## Success Criteria

### 1. Performance Targets
- **90%+ reduction** in authorization-related database calls
- **50%+ improvement** in page load times for data-heavy pages
- **<100ms** profile cache retrieval time
- **>95%** cache hit rate for user profiles

### 2. Functionality Goals
- **100%** backward compatibility maintained during transition
- **Zero** security regressions or permission escalations
- **Seamless** user experience with enhanced capabilities
- **Full** operational flexibility for complex workflows

### 3. Operational Objectives
- **Incremental deployment** with rollback capability
- **Comprehensive monitoring** and alerting
- **User adoption** without additional training requirements
- **Maintainable** codebase with clear separation of concerns

---

## Timeline & Milestones

### Phase 1: Enhanced User Profile (4 weeks)
- Week 1: UserProfile model and service design
- Week 2: Profile building and caching implementation
- Week 3: Enhanced login integration
- Week 4: Testing and validation

### Phase 2: Service Layer Refactoring (6 weeks)
- Week 1-2: LocationService refactoring
- Week 3-4: VisibilityAuthorizeService enhancement
- Week 5: EmployeeService optimization
- Week 6: ApprovalService integration

### Phase 3: Controller & UI Integration (4 weeks)
- Week 1-2: Controller updates and enhanced base classes
- Week 3: UI enhancements and dynamic permissions
- Week 4: Performance monitoring and final testing

### Total Timeline: 14 weeks with overlap and testing periods

---

## Conclusion

This comprehensive refactoring plan transforms the MRIV authorization system from a reactive, performance-heavy approach to a proactive, cached-based system with clear separation of concerns. The dual-permission model addresses the operational complexity while maintaining security, and the incremental implementation ensures business continuity throughout the transition.

The enhanced user profile system serves as the foundation for future system improvements, providing a scalable architecture that can easily accommodate new features and business requirements while maintaining optimal performance characteristics.

---

*This document should be reviewed and updated as implementation progresses and requirements evolve.*