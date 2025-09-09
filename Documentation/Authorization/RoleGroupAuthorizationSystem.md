# Role Group Authorization System - Master Reference

## Overview

The MRIV (Material Requisition and Inventory Verification) system implements a sophisticated hierarchical authorization system that controls data access based on organizational structure and flexible role groups. This document serves as the master reference for understanding, implementing, and maintaining the authorization system.

## System Architecture

### Core Components

1. **Organizational Hierarchy**
   - Stations (top-level locations)
   - Departments (within stations)
   - Employees (within departments)

2. **Role Group System**
   - RoleGroup entities with permission flags
   - RoleGroupMember assignments
   - Dynamic permission evaluation

3. **Authorization Service**
   - VisibilityAuthorizeService (core implementation)
   - Query filtering and entity access control
   - Integration with workflow system

## Authorization Decision Matrix

### Permission Flags

The system uses two critical boolean flags to determine access scope:

| Flag | Description | Impact |
|------|-------------|---------|
| `CanAccessAcrossStations` | User can access their department's data across ALL stations | Horizontal expansion of access |
| `CanAccessAcrossDepartments` | User can access ALL departments' data within their station scope | Vertical expansion of access |

### Complete Authorization Matrix

| User Type | CanAccessAcrossStations | CanAccessAcrossDepartments | Access Scope | Use Cases |
|-----------|:-----------------------:|:--------------------------:|--------------|-----------|
| **Default User** (no role group) | ❌ NO | ❌ NO | Own department at own station | Regular employees, contractors |
| **Department Manager** | ❌ NO | ❌ NO | Own department at own station | Department heads, supervisors |
| **Station Support/Manager** | ❌ NO | ✅ YES | All departments at own station | Factory managers, station IT support |
| **Group/General Manager** | ✅ YES | ❌ NO | Own department at all stations | Regional managers, departmental GMs |
| **Administrator** | ✅ YES | ✅ YES | All data everywhere | System admins, super users |

## Decision Flow Logic

### Authorization Evaluation Process

```mermaid
graph TD
    A[User Request] --> B{User is Admin?}
    B -->|Yes| C[Grant Full Access]
    B -->|No| D{User in Role Group?}
    D -->|No| E[Default User Access Only]
    D -->|Yes| F{Check Permission Flags}
    F --> G{CanAccessAcrossStations?}
    F --> H{CanAccessAcrossDepartments?}
    G -->|No| I{CanAccessAcrossDepartments?}
    G -->|Yes| J{CanAccessAcrossDepartments?}
    I -->|No| K[Department at Station]
    I -->|Yes| L[All Departments at Station]
    J -->|No| M[Department at All Stations]
    J -->|Yes| N[All Data Everywhere]
```

### Detailed Logic Implementation

```csharp
IF user.Role == "Admin"
    RETURN GrantFullAccess()

var roleGroups = GetActiveRoleGroups(user.PayrollNo)
IF roleGroups.Empty()
    RETURN RestrictToOwnData()

var canAccessAcrossStations = roleGroups.Any(g => g.CanAccessAcrossStations)
var canAccessAcrossDepartments = roleGroups.Any(g => g.CanAccessAcrossDepartments)

SWITCH (canAccessAcrossStations, canAccessAcrossDepartments)
    CASE (false, false): RETURN RestrictToDepartmentAtStation()
    CASE (false, true):  RETURN RestrictToAllDepartmentsAtStation()
    CASE (true, false):  RETURN RestrictToDepartmentAtAllStations()
    CASE (true, true):   RETURN GrantFullAccess()
```

## Data Normalization Rules

### Station Code Normalization

| Input | Normalized Output | Description |
|-------|-------------------|-------------|
| "HQ" | "0" | Headquarters special case |
| "0" | "0" | Already normalized HQ |
| "1" | "001" | Single digit to 3-digit format |
| "12" | "012" | Two digit to 3-digit format |
| "123" | "123" | Already 3-digit format |

### Department Code Normalization

- Stored as integer values in database
- String representation with trimming applied
- Cross-context consistency between KtdaleaveContext and RequisitionContext

## Integration Points with System Modules

### 1. Requisition Management
- **Creation**: Filter available materials by user's access scope
- **Approval**: Show only requisitions user can approve
- **Status Updates**: Restrict updates based on visibility rules

### 2. Material Assignment
- **Assignment Creation**: Validate user can assign materials in target department/station
- **Transfer Requests**: Verify both source and destination access
- **Return Processing**: Confirm user can process returns for materials

### 3. Workflow Engine
- **Approval Queues**: Filter approvals by role group permissions
- **Notification Routing**: Send notifications only to authorized recipients
- **Escalation Logic**: Respect permission boundaries in escalation paths

### 4. Reporting System
- **Data Filtering**: Apply visibility scope to all reports
- **Dashboard Widgets**: Show only authorized data in widgets
- **Export Functions**: Restrict export data to user's scope

## Security Implications and Best Practices

### Security Considerations

#### ⚠️ **Critical Security Issues**

1. **Default Permission Values**
   ```csharp
   // SECURITY RISK: RoleGroup model defaults
   public bool CanAccessAcrossStations { get; set; } = true;  // ❌ TOO PERMISSIVE
   public bool CanAccessAcrossDepartments { get; set; } = true; // ❌ TOO PERMISSIVE
   ```
   **Recommendation**: Default to `false` for principle of least privilege

2. **Admin Bypass**
   - Admin role bypasses all role group restrictions
   - No audit trail for admin access decisions
   - Consider implementing admin justification logging

3. **Role Group Flag Logic**
   - ANY role group with flag=true grants permission (OR logic)
   - Could lead to privilege escalation if not carefully managed

#### ✅ **Security Best Practices**

1. **Regular Audits**
   - Monthly review of role group memberships
   - Quarterly validation of permission flag settings
   - Annual comprehensive access review

2. **Principle of Least Privilege**
   - Start with minimal permissions and expand as needed
   - Regular cleanup of inactive role group memberships
   - Time-bound role assignments where appropriate

3. **Monitoring and Logging**
   - Log all authorization decisions (currently missing)
   - Alert on suspicious access patterns
   - Track admin override usage

### Implementation Guidelines

#### Creating New Role Groups

```csharp
// ✅ CORRECT: Start with restrictive permissions
var newRoleGroup = new RoleGroup
{
    Name = "Department Supervisor",
    Description = "Supervises specific department operations",
    CanAccessAcrossStations = false,    // ✅ Restrictive by default
    CanAccessAcrossDepartments = false, // ✅ Restrictive by default
    IsActive = true
};
```

#### Adding Users to Role Groups

```csharp
// ✅ CORRECT: Validate business justification
var membership = new RoleGroupMember
{
    RoleGroupId = roleGroupId,
    PayrollNo = userPayrollNo,
    IsActive = true,
    CreatedAt = DateTime.Now
    // Consider adding: JustificationReason, ApprovedBy, ExpiryDate
};
```

## Performance Considerations

### Query Optimization

1. **Index Strategy**
   ```sql
   -- Critical indexes for performance
   CREATE INDEX IX_RoleGroupMembers_PayrollNo_IsActive ON RoleGroupMembers (PayrollNo, IsActive);
   CREATE INDEX IX_RoleGroups_IsActive ON RoleGroups (IsActive);
   CREATE INDEX IX_MaterialAssignments_PayrollNo_IsActive ON MaterialAssignments (PayrollNo, IsActive);
   ```

2. **Caching Strategy**
   - Cache user role group memberships (TTL: 15 minutes)
   - Cache department/station lookups (TTL: 1 hour)
   - Invalidate cache on role group changes

3. **Query Pattern Optimization**
   ```csharp
   // ✅ EFFICIENT: Single query with joins
   var authorizedRequisitions = context.Requisitions
       .Where(r => authorizedDepartments.Contains(r.DepartmentId))
       .Where(r => authorizedStations.Contains(r.IssueStationId));
   
   // ❌ INEFFICIENT: Multiple database calls
   foreach (var requisition in allRequisitions)
   {
       if (await CanUserAccessAsync(requisition, userPayrollNo)) // DB call per item
       {
           // Process requisition
       }
   }
   ```

## Common Pitfalls and Solutions

### Pitfall 1: Cross-Context Data Inconsistency

**Problem**: Employee data in KtdaleaveContext doesn't match RequisitionContext expectations

**Solution**:
```csharp
// Implement cross-context validation
public async Task<bool> ValidateEmployeeDataConsistency(string payrollNo)
{
    var ktdaEmployee = await _ktdaleaveContext.EmployeeBkps
        .FirstOrDefaultAsync(e => e.PayrollNo == payrollNo);
    
    var requisitionEmployee = await _requisitionContext.Employees
        .FirstOrDefaultAsync(e => e.PayrollNo == payrollNo);
    
    return ktdaEmployee?.Department == requisitionEmployee?.Department &&
           ktdaEmployee?.Station == requisitionEmployee?.Station;
}
```

### Pitfall 2: Forgotten Permission Updates

**Problem**: New features don't implement authorization checks

**Solution**: Implement authorization checklist for code reviews
```csharp
// Template for new controller actions
[HttpGet]
public async Task<IActionResult> NewFeature()
{
    // ✅ REQUIRED: Apply authorization filtering
    var authorizedData = await _visibilityService
        .ApplyVisibilityScopeAsync(query, User.Identity.Name);
    
    return View(authorizedData);
}
```

### Pitfall 3: Testing Without Proper Data Setup

**Problem**: Tests pass with admin users but fail with restricted users

**Solution**: Use standardized test data (see AuthorizationTestMatrix.md)

## Backward Compatibility Considerations

### Database Schema Changes

1. **Adding New Permission Flags**
   ```sql
   -- Add new permission with safe default
   ALTER TABLE RoleGroups 
   ADD CanAccessAcrossProjects BIT DEFAULT 0; -- ✅ Restrictive default
   ```

2. **Changing Default Values**
   ```sql
   -- Update existing records first, then change default
   UPDATE RoleGroups 
   SET CanAccessAcrossStations = 0 
   WHERE CanAccessAcrossStations = 1 AND Name != 'Administrator';
   
   ALTER TABLE RoleGroups 
   ALTER COLUMN CanAccessAcrossStations BIT DEFAULT 0;
   ```

### Code Migration Patterns

```csharp
// Version compatibility wrapper
public async Task<bool> HasPermissionAsync(string payrollNo, string permission)
{
    // Support both old and new permission systems
    if (await IsLegacyUser(payrollNo))
    {
        return await CheckLegacyPermission(payrollNo, permission);
    }
    
    return await CheckRoleGroupPermission(payrollNo, permission);
}
```

## Troubleshooting Common Issues

### Issue 1: User Can't See Expected Data

**Diagnostic Steps**:
1. Verify user's role group membership: `SELECT * FROM RoleGroupMembers WHERE PayrollNo = 'XXX' AND IsActive = 1`
2. Check role group permissions: `SELECT * FROM RoleGroups WHERE Id IN (...)`
3. Validate user's department/station: `SELECT Department, Station FROM EmployeeBkp WHERE PayrollNo = 'XXX'`
4. Test authorization logic in isolation

**Common Causes**:
- Inactive role group membership
- Incorrect department/station assignment
- Role group with insufficient permissions
- Data normalization mismatches

### Issue 2: Performance Degradation

**Diagnostic Steps**:
1. Check query execution plans
2. Validate index usage
3. Monitor cache hit rates
4. Profile authorization service calls

**Optimization Strategies**:
- Batch authorization checks
- Implement result caching
- Use projection queries to reduce data transfer
- Consider denormalization for frequently accessed data

## Related Documentation

- [VisibilityAuthorizeService Reference](../Services/VisibilityAuthorizeService_Reference.md) - Detailed service documentation
- [Authorization Test Matrix](../Testing/AuthorizationTestMatrix.md) - Comprehensive testing scenarios
- [Permission Matrix Quick Reference](../QuickReference/PermissionMatrix_QuickRef.md) - One-page reference
- [Extending Authorization](../Development/ExtendingAuthorization.md) - Developer implementation guide
- [Role Group Management](../Operations/RoleGroupManagement.md) - Administrative procedures

## Version History

| Version | Date | Changes | Author |
|---------|------|---------|--------|
| 1.0 | 2024-12 | Initial comprehensive documentation | System |
| | | | |

---

*This document should be reviewed quarterly and updated whenever authorization logic changes are made.*