# UserProfile + VisibilityService Architecture Pattern

## Overview

This document outlines the **correct architecture** for implementing role-based access control using UserProfile (cached user context) and VisibilityService (query filtering logic) working together.

## Core Principles

### 1. **Separation of Concerns**
- **UserProfile**: User context data (cached at login)
- **VisibilityService**: Query filtering logic (stateless operations)

### 2. **Performance Optimization**
- **One-time caching**: All user access data loaded at login
- **Zero additional DB calls**: During request processing for user context
- **Efficient filtering**: Applied directly to IQueryable before execution

### 3. **Consistency**
- **Single source of truth**: UserProfile for all user access data
- **Unified filtering logic**: VisibilityService handles all access control
- **Predictable behavior**: Same pattern across all controllers/services

---

## Architecture Components

### UserProfile (Login-time Caching)

**Purpose**: Cache all user access data in memory/session to avoid repeated database calls.

**Structure**:
```csharp
UserProfile
├── BasicInfo (Name, PayrollNo, Email, Department, Station)
├── RoleInformation (IsAdmin, SystemRole, RoleGroups)
├── LocationAccess
│   ├── HomeDepartment/HomeStation (user's primary location)
│   ├── AccessibleDepartments/AccessibleStations (full objects)
│   └── AccessibleDepartmentIds/AccessibleStationIds (for queries)
├── VisibilityScope (CanAccessAcrossStations, IsDefaultUser, PermissionLevel)
└── CacheInfo (Expiration, Version)
```

**Key Properties**:
- `VisibilityScope.IsAdmin` - Can see all data
- `VisibilityScope.IsDefaultUser` - Limited to own data only
- `VisibilityScope.CanAccessAcrossStations` - Role group permissions
- `LocationAccess.AccessibleStationIds` - Pre-calculated accessible locations

### VisibilityService (Query Filtering)

**Purpose**: Apply consistent access control logic to any IQueryable based on UserProfile.

**Key Methods**:
```csharp
// Enhanced method using cached UserProfile (RECOMMENDED)
IQueryable<T> ApplyVisibilityScopeWithProfile<T>(IQueryable<T> query, UserProfile userProfile)

// Legacy method (avoid when possible - makes DB calls)
Task<IQueryable<T>> ApplyVisibilityScopeAsync<T>(IQueryable<T> query, string userPayrollNo)

// Helper methods for dropdowns/filters
Task<List<Department>> GetVisibleDepartmentsAsync(string userPayrollNo)
Task<List<Station>> GetVisibleStationsAsync(string userPayrollNo)
```

---

## Implementation Pattern

### ✅ **CORRECT Implementation Pattern**

```csharp
public async Task<List<Requisition>> GetUserRequisitions()
{
    // Step 1: Get cached UserProfile (fast - no DB calls)
    var userProfile = await _userProfileService.GetCurrentUserProfileAsync();

    // Step 2: Create base query
    var query = _context.Requisitions
        .Include(r => r.RequisitionItems)
        .Include(r => r.Approvals);

    // Step 3: Apply access control using VisibilityService
    var filteredQuery = _visibilityService.ApplyVisibilityScopeWithProfile(query, userProfile);

    // Step 4: Add business filters
    filteredQuery = filteredQuery.Where(r => r.Status != RequisitionStatus.Cancelled);

    // Step 5: Execute query
    return await filteredQuery.ToListAsync();
}
```

### ❌ **INCORRECT Anti-Patterns**

**Anti-Pattern 1: Manual access control logic**
```csharp
// DON'T DO THIS - Duplicates logic everywhere
var userProfile = await _userProfileService.GetCurrentUserProfileAsync();
if (userProfile.RoleInformation.IsAdmin)
    query = _context.Requisitions; // Admin sees all
else if (userProfile.VisibilityScope.IsDefaultUser)
    query = _context.Requisitions.Where(r => r.PayrollNo == userProfile.BasicInfo.PayrollNo);
// ... more complex logic duplicated
```

**Anti-Pattern 2: Mixing cached and non-cached approaches**
```csharp
// DON'T DO THIS - Inconsistent and inefficient
var userProfile = await _userProfileService.GetCurrentUserProfileAsync();
var visibleStations = await _visibilityService.GetVisibleStationsAsync(userProfile.BasicInfo.PayrollNo); // Extra DB call!
var stationIds = userProfile.LocationAccess.AccessibleStationIds; // Cached data available!
```

**Anti-Pattern 3: Legacy method usage**
```csharp
// AVOID THIS - Makes unnecessary DB calls for user context
var payrollNo = HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
var filteredQuery = await _visibilityService.ApplyVisibilityScopeAsync(_context.Requisitions, payrollNo);
```

---

## Access Control Logic Flow

### 1. **Admin Users** (`userProfile.RoleInformation.IsAdmin = true`)
- **Access**: Everything
- **Query Filter**: No filtering applied
- **Use Case**: System administrators, management

### 2. **Default Users** (`userProfile.VisibilityScope.IsDefaultUser = true`)
- **Access**: Only their own data
- **Query Filter**: `WHERE PayrollNo = userProfile.BasicInfo.PayrollNo`
- **Use Case**: Regular employees with no special roles

### 3. **Role Group Users** (In role groups like "Station Support", "Department Manager")
- **Access**: Based on accessible stations/departments
- **Query Filter**: `WHERE StationId IN (userProfile.LocationAccess.AccessibleStationIds)`
- **Use Case**: Managers, supervisors, support staff

---

## Example: Requisition Access Control

Let's trace through how a **Department Manager** would see requisitions:

### User Profile (Department Manager):
```csharp
UserProfile {
    BasicInfo: { PayrollNo: "MGR001", Department: "IT" }
    RoleInformation: { IsAdmin: false }
    VisibilityScope: {
        IsDefaultUser: false,
        CanAccessAcrossStations: false,
        CanAccessAcrossDepartments: true
    }
    LocationAccess: {
        HomeDepartment: { Id: 5, Name: "IT" }
        HomeStation: { Id: 1, Name: "Head Office" }
        AccessibleDepartmentIds: [5, 12, 15] // IT + sub-departments
        AccessibleStationIds: [1] // Head Office only
    }
}
```

### Query Filtering:
```csharp
// Original query
var query = _context.Requisitions.Include(r => r.RequisitionItems);

// After VisibilityService filtering
var filteredQuery = query.Where(r =>
    new[] { 5, 12, 15 }.Contains(r.DepartmentId)); // IT departments only

// Final results: Department Manager sees all requisitions from IT department and its sub-departments
```

---

## Filter Options Pattern

For dropdown/filter populations, use cached UserProfile data:

### ✅ **CORRECT: Use Cached Data**
```csharp
public async Task<FilterOptionsViewModel> GetFilterOptions()
{
    var userProfile = await _userProfileService.GetCurrentUserProfileAsync();

    return new FilterOptionsViewModel
    {
        Stations = userProfile.LocationAccess.AccessibleStations
            .Select(s => new FilterOption { Id = s.Id.ToString(), Name = s.Name })
            .ToList(),

        Departments = userProfile.LocationAccess.AccessibleDepartments
            .Select(d => new FilterOption { Id = d.Id.ToString(), Name = d.Name })
            .ToList()
    };
}
```

### ❌ **INCORRECT: Additional DB Calls**
```csharp
// DON'T DO THIS - Makes unnecessary DB calls
var visibleStations = await _visibilityService.GetVisibleStationsAsync(userPayrollNo);
var visibleDepartments = await _visibilityService.GetVisibleDepartmentsAsync(userPayrollNo);
```

---

## Performance Benefits

### Before (Multiple DB Calls Per Request)
1. Get user context: 3-5 DB calls
2. Get accessible locations: 2-3 DB calls
3. Apply filters: Complex joins
4. **Total**: 5-8 DB calls per request

### After (Cached UserProfile)
1. Get cached UserProfile: 0 DB calls
2. Apply pre-calculated filters: Direct WHERE clauses
3. **Total**: 0 additional DB calls for access control

---

## Testing Strategy

### Unit Tests
```csharp
[Test]
public void ApplyVisibilityScopeWithProfile_AdminUser_ReturnsAllData()
{
    // Arrange
    var adminProfile = CreateAdminUserProfile();
    var query = CreateTestRequisitions().AsQueryable();

    // Act
    var result = _visibilityService.ApplyVisibilityScopeWithProfile(query, adminProfile);

    // Assert
    Assert.AreEqual(query.Count(), result.Count()); // No filtering for admin
}

[Test]
public void ApplyVisibilityScopeWithProfile_DefaultUser_ReturnsOnlyUserData()
{
    // Arrange
    var defaultProfile = CreateDefaultUserProfile("EMP001");
    var query = CreateTestRequisitions().AsQueryable();

    // Act
    var result = _visibilityService.ApplyVisibilityScopeWithProfile(query, defaultProfile);

    // Assert
    Assert.IsTrue(result.All(r => r.PayrollNo == "EMP001"));
}
```

---

## Migration Strategy

### Phase 1: Identify Current Anti-Patterns
1. Search for manual access control logic
2. Find direct VisibilityService.GetVisible* calls
3. Locate UserProfile property access issues

### Phase 2: Standardize Pattern
1. Replace manual logic with ApplyVisibilityScopeWithProfile
2. Use cached UserProfile data for filters
3. Remove unnecessary DB calls

### Phase 3: Extend VisibilityService
1. Add support for new entity types (Materials, etc.)
2. Implement consistent filtering logic
3. Add comprehensive unit tests

---

## Common Pitfalls

### 1. **Property Access Confusion**
```csharp
// WRONG - These properties are in different objects
userProfile.RoleInformation.IsDefaultUser          // ❌ Wrong object
userProfile.RoleInformation.CanAccessAcrossStations // ❌ Wrong object

// CORRECT - Check the UserProfile structure
userProfile.VisibilityScope.IsDefaultUser          // ✅ Correct
userProfile.VisibilityScope.CanAccessAcrossStations // ✅ Correct
userProfile.RoleInformation.IsAdmin                // ✅ Correct
```

### 2. **Namespace Issues**
```csharp
// Add required using statements
using MRIV.Models;  // For UserProfile
using MRIV.Services; // For IVisibilityAuthorizeService
```

### 3. **Method Name Confusion**
```csharp
// OLD - Makes DB calls
await _userProfileService.GetUserProfileAsync(httpContext)

// NEW - Uses cached data
await _userProfileService.GetCurrentUserProfileAsync()
```

---

## Next Steps

This pattern should be applied consistently across:
1. **All Controllers** - Replace manual access control logic
2. **All Services** - Use cached UserProfile data
3. **Dashboard Services** - Apply to Material Dashboard, Management Dashboard
4. **Report Services** - Consistent filtering across reports
5. **API Endpoints** - Same pattern for API access control

The goal is **one consistent pattern** used everywhere, with **zero duplicate access control logic** and **optimal performance** through caching.