# MRIV System Role Group Access Service
# Leveraging Role Groups for Location-Based Authorization

## Table of Contents
1. [RoleGroupAccessService](#rolegroupaccessservice)
2. [Integration with VisibilityAuthorizeService](#integration-with-visibilityauthorizeservice)
3. [Role Group Structure for Location Access](#role-group-structure-for-location-access)

## RoleGroupAccessService

Create a new service to leverage the RoleGroup system for location-based authorization:

```csharp
using Microsoft.EntityFrameworkCore;
using MRIV.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MRIV.Services
{
    public interface IRoleGroupAccessService
    {
        // Check if user has special access through role groups
        Task<bool> HasFullDepartmentAccessAsync(string payrollNo);
        Task<bool> HasFullStationAccessAsync(string payrollNo);
        
        // Get all role groups for a user
        Task<List<RoleGroup>> GetUserRoleGroupsAsync(string payrollNo);
        
        // Check if user is in a specific role group
        Task<bool> IsUserInRoleGroupAsync(string payrollNo, int roleGroupId);
        
        // Get users with specific access rights
        Task<List<string>> GetUsersWithFullDepartmentAccessAsync();
        Task<List<string>> GetUsersWithFullStationAccessAsync();
    }

    public class RoleGroupAccessService : IRoleGroupAccessService
    {
        private readonly RequisitionContext _context;
        private readonly ILogger<RoleGroupAccessService> _logger;

        public RoleGroupAccessService(
            RequisitionContext context,
            ILogger<RoleGroupAccessService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> HasFullDepartmentAccessAsync(string payrollNo)
        {
            if (string.IsNullOrEmpty(payrollNo))
                return false;

            // Get all active role groups the user belongs to
            var roleGroups = await GetUserRoleGroupsAsync(payrollNo);
            
            // Check if any of the role groups has full department access
            return roleGroups.Any(rg => rg.HasFullDepartmentAccess);
        }

        public async Task<bool> HasFullStationAccessAsync(string payrollNo)
        {
            if (string.IsNullOrEmpty(payrollNo))
                return false;

            // Get all active role groups the user belongs to
            var roleGroups = await GetUserRoleGroupsAsync(payrollNo);
            
            // Check if any of the role groups has full station access
            return roleGroups.Any(rg => rg.HasFullStationAccess);
        }

        public async Task<List<RoleGroup>> GetUserRoleGroupsAsync(string payrollNo)
        {
            if (string.IsNullOrEmpty(payrollNo))
                return new List<RoleGroup>();

            // Get all active role groups the user belongs to
            var roleGroups = await _context.RoleGroupMembers
                .Where(m => m.PayrollNo == payrollNo && m.IsActive)
                .Include(m => m.RoleGroup)
                .Where(m => m.RoleGroup.IsActive)
                .Select(m => m.RoleGroup)
                .ToListAsync();
                
            return roleGroups;
        }

        public async Task<bool> IsUserInRoleGroupAsync(string payrollNo, int roleGroupId)
        {
            if (string.IsNullOrEmpty(payrollNo))
                return false;

            // Check if user is an active member of the specified role group
            return await _context.RoleGroupMembers
                .AnyAsync(m => m.PayrollNo == payrollNo && 
                               m.RoleGroupId == roleGroupId && 
                               m.IsActive && 
                               m.RoleGroup.IsActive);
        }

        public async Task<List<string>> GetUsersWithFullDepartmentAccessAsync()
        {
            // Get all active users who belong to role groups with full department access
            var users = await _context.RoleGroupMembers
                .Where(m => m.IsActive && m.RoleGroup.IsActive && m.RoleGroup.HasFullDepartmentAccess)
                .Select(m => m.PayrollNo)
                .Distinct()
                .ToListAsync();
                
            return users;
        }

        public async Task<List<string>> GetUsersWithFullStationAccessAsync()
        {
            // Get all active users who belong to role groups with full station access
            var users = await _context.RoleGroupMembers
                .Where(m => m.IsActive && m.RoleGroup.IsActive && m.RoleGroup.HasFullStationAccess)
                .Select(m => m.PayrollNo)
                .Distinct()
                .ToListAsync();
                
            return users;
        }
    }
}
```

## Integration with VisibilityAuthorizeService

The RoleGroupAccessService is integrated with the VisibilityAuthorizeService to provide enhanced authorization capabilities:

```csharp
// In VisibilityAuthorizeService constructor
public VisibilityAuthorizeService(
    IEmployeeService employeeService,
    ILocationService locationService,
    IRoleGroupAccessService roleGroupService,
    ILogger<VisibilityAuthorizeService> logger)
{
    _employeeService = employeeService;
    _locationService = locationService;
    _roleGroupService = roleGroupService;
    _logger = logger;
}

// In ApplyLocationScopeAsync method
public async Task<IQueryable<T>> ApplyLocationScopeAsync<T>(
    IQueryable<T> query, 
    string userPayrollNo,
    bool checkDepartment = true,
    bool checkStation = true) where T : class
{
    // ... existing code ...

    // Check if user has special access through role groups
    bool hasFullDepartmentAccess = await _roleGroupService.HasFullDepartmentAccessAsync(userPayrollNo);
    bool hasFullStationAccess = await _roleGroupService.HasFullStationAccessAsync(userPayrollNo);

    // If user has full access, no need to filter
    if (hasFullDepartmentAccess && hasFullStationAccess)
        return query;

    // ... rest of the method ...
}
```

## Role Group Structure for Location Access

To implement the location-based authorization requirements, create the following role groups:

### 1. Department-Based Groups

Create role groups for departments with default access settings:

```
Name: "ICT Department"
Description: "Members of the ICT Department"
HasFullDepartmentAccess: false (default)
HasFullStationAccess: false (default)
```

Members of these groups can only see approvals and requisitions for their department.

### 2. Station-Based Groups

Create role groups for stations with station-wide access:

```
Name: "Factory A Staff"
Description: "Staff at Factory A who need access to all departments within the factory"
HasFullDepartmentAccess: true
HasFullStationAccess: true
```

Members of these groups can see all approvals and requisitions for their station, regardless of department.

### 3. Cross-Departmental Groups

Create role groups for special access needs:

```
Name: "ICT Services Team"
Description: "ICT support staff who need access across departments"
HasFullDepartmentAccess: true
HasFullStationAccess: false
```

Members of these groups can see approvals and requisitions across departments but only within their station.

### 4. Global Access Groups

Create role groups for administrators:

```
Name: "System Administrators"
Description: "Administrators with global access"
HasFullDepartmentAccess: true
HasFullStationAccess: true
```

Members of these groups can see all approvals and requisitions regardless of department or station.

### Example Use Cases

#### ICT Staff at Factory Receiving Items

1. Create a "Factory Receivers" role group:
   ```
   Name: "Factory A Receivers"
   Description: "Staff responsible for receiving items at Factory A"
   HasFullDepartmentAccess: false
   HasFullStationAccess: true
   ```

2. Add relevant ICT staff to this group
3. Set the `VisibilityScope` of receiving approvals to `VisibilityScope.Station`
4. ICT staff will see all receiving approvals for their factory regardless of department

#### Department-Specific Visibility at HQ

1. Keep users in department-specific groups with default settings
2. Set the `VisibilityScope` of HQ approvals to `VisibilityScope.Department`
3. Users will only see approvals for their department at HQ

#### Cross-Departmental Access for ICT Support

1. Create an "ICT Support" role group:
   ```
   Name: "ICT Support Team"
   Description: "ICT staff providing support across departments"
   HasFullDepartmentAccess: true
   HasFullStationAccess: false
   ```

2. Add ICT support staff to this group
3. They'll see approvals across departments for support purposes
