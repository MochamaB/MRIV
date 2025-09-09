# Entity Visibility Matrix

## Overview
Comprehensive reference for how role group permissions affect visibility and access to different entities in the MRIV system. Each entity has specific visibility rules based on organizational hierarchy and permission flags.

---

## Core Entities

### Employee Entity
**Base Query Filter**: User can see employees based on their station and department access permissions.

| User Permission | Visible Employees |
|----------------|-------------------|
| Single Station, Single Department | Own department only |
| Single Station, Cross Department | All departments in own station |
| Cross Station, Single Department | Same department across all stations |
| Cross Station, Cross Department | All employees system-wide |

```csharp
// Employee visibility implementation
public IQueryable<Employee> ApplyEmployeeVisibilityFilter(
    IQueryable<Employee> query, 
    Employee currentUser)
{
    // Station filtering
    if (!currentUser.RoleGroup.CanAccessAcrossStations)
    {
        query = query.Where(e => e.StationId == currentUser.StationId);
    }
    
    // Department filtering
    if (!currentUser.RoleGroup.CanAccessAcrossDepartments)
    {
        query = query.Where(e => e.DepartmentId == currentUser.DepartmentId);
    }
    
    return query;
}
```

**Special Cases:**
- **Self**: User can always view their own employee record
- **Supervisors**: May have enhanced visibility to direct reports regardless of permission flags
- **HR Users**: Typically granted cross-department access within their station

---

### Material Entity
**Base Query Filter**: Materials are visible based on their current assigned location and user's access permissions.

| Material Location | User Can See If... |
|-------------------|-------------------|
| Same Department | Always visible |
| Other Department, Same Station | Has `CanAccessAcrossDepartments = true` |
| Different Station | Has `CanAccessAcrossStations = true` |
| Unassigned/Pool | Based on pool's station/department assignment |

```csharp
public IQueryable<Material> ApplyMaterialVisibilityFilter(
    IQueryable<Material> query, 
    Employee currentUser)
{
    return query.Where(m => 
        // Station check
        (currentUser.RoleGroup.CanAccessAcrossStations || 
         m.CurrentStationId == currentUser.StationId) &&
        // Department check
        (currentUser.RoleGroup.CanAccessAcrossDepartments || 
         m.CurrentDepartmentId == currentUser.DepartmentId)
    );
}
```

**Special Cases:**
- **Material History**: Users can view history for materials they currently have access to
- **Assignment History**: Full assignment trail visible if user had access at time of assignment
- **Maintenance Records**: Visible based on current material location, not historical locations

---

### Requisition Entity
**Base Query Filter**: Requisitions are visible based on requesting department and user's permissions.

| Requisition Type | Visibility Rule |
|------------------|----------------|
| Own Department Request | Always visible |
| Other Department, Same Station | Visible if `CanAccessAcrossDepartments = true` |
| Different Station Request | Visible if `CanAccessAcrossStations = true` |
| Pending Own Approval | Always visible (approval queue) |

```csharp
public IQueryable<Requisition> ApplyRequisitionVisibilityFilter(
    IQueryable<Requisition> query, 
    Employee currentUser)
{
    return query.Where(r => 
        // Created by user
        r.CreatedBy == currentUser.PayrollNo ||
        
        // Pending user's approval
        r.PendingApprovers.Contains(currentUser.PayrollNo) ||
        
        // Within user's access scope
        ((currentUser.RoleGroup.CanAccessAcrossStations || 
          r.RequestingStationId == currentUser.StationId) &&
         (currentUser.RoleGroup.CanAccessAcrossDepartments || 
          r.RequestingDepartmentId == currentUser.DepartmentId))
    );
}
```

**State-Based Visibility:**
- **Draft**: Only visible to creator
- **Submitted**: Visible to approval chain + those with department access
- **Approved/Rejected**: Visible to all with department access
- **Cancelled**: Visible to creator + approval chain

---

### MaterialAssignment Entity
**Base Query Filter**: Assignments are visible based on both assignee location and material location.

| Assignment Scenario | Visibility Requirement |
|--------------------|------------------------|
| Same Department Assignment | Always visible |
| Cross-Department Assignment | Access to BOTH assignee and material departments |
| Cross-Station Assignment | Cross-station access required |
| Historical Assignments | Based on current user permissions, not historical |

```csharp
public IQueryable<MaterialAssignment> ApplyAssignmentVisibilityFilter(
    IQueryable<MaterialAssignment> query, 
    Employee currentUser)
{
    return query.Where(ma => 
        // User can see the assignee
        ((currentUser.RoleGroup.CanAccessAcrossStations || 
          ma.Employee.StationId == currentUser.StationId) &&
         (currentUser.RoleGroup.CanAccessAcrossDepartments || 
          ma.Employee.DepartmentId == currentUser.DepartmentId)) ||
          
        // User can see the material
        ((currentUser.RoleGroup.CanAccessAcrossStations || 
          ma.Material.CurrentStationId == currentUser.StationId) &&
         (currentUser.RoleGroup.CanAccessAcrossDepartments || 
          ma.Material.CurrentDepartmentId == currentUser.DepartmentId))
    );
}
```

---

### Department Entity
**Base Query Filter**: Departments are visible based on user's station and cross-department permissions.

| User Access Level | Visible Departments |
|-------------------|-------------------|
| Single Department | Own department only |
| Cross-Department, Single Station | All departments in own station |
| Cross-Station, Single Department | Same department across all stations |
| Cross-Station, Cross-Department | All departments system-wide |

```csharp
public IQueryable<Department> ApplyDepartmentVisibilityFilter(
    IQueryable<Department> query, 
    Employee currentUser)
{
    if (!currentUser.RoleGroup.CanAccessAcrossStations)
    {
        query = query.Where(d => d.StationId == currentUser.StationId);
    }
    
    if (!currentUser.RoleGroup.CanAccessAcrossDepartments)
    {
        query = query.Where(d => d.DepartmentId == currentUser.DepartmentId);
    }
    
    return query;
}
```

---

### Station Entity
**Base Query Filter**: Stations are visible based on user's cross-station permissions.

| User Access Level | Visible Stations |
|-------------------|-----------------|
| Single Station | Own station only |
| Cross-Station | All stations |

```csharp
public IQueryable<Station> ApplyStationVisibilityFilter(
    IQueryable<Station> query, 
    Employee currentUser)
{
    if (!currentUser.RoleGroup.CanAccessAcrossStations)
    {
        query = query.Where(s => s.StationId == currentUser.StationId);
    }
    
    return query;
}
```

---

## Supporting Entities

### RoleGroup Entity
**Visibility Rule**: Users can only see role groups that are equal or lower in hierarchy than their own.

```csharp
public IQueryable<RoleGroup> ApplyRoleGroupVisibilityFilter(
    IQueryable<RoleGroup> query, 
    Employee currentUser)
{
    // Users cannot see role groups with higher permissions than their own
    return query.Where(rg => 
        (!rg.CanAccessAcrossStations || currentUser.RoleGroup.CanAccessAcrossStations) &&
        (!rg.CanAccessAcrossDepartments || currentUser.RoleGroup.CanAccessAcrossDepartments)
    );
}
```

### MaterialCategory/Subcategory Entities
**Visibility Rule**: Categories are globally visible but may have department-specific restrictions.

```csharp
// Generally visible to all users, but may have department restrictions
public IQueryable<MaterialCategory> ApplyMaterialCategoryVisibilityFilter(
    IQueryable<MaterialCategory> query, 
    Employee currentUser)
{
    return query.Where(mc => 
        mc.IsGloballyVisible || 
        mc.RestrictedToDepartments.Contains(currentUser.DepartmentId)
    );
}
```

---

## Cross-Context Entities

### Leave Applications (KtdaleaveContext)
**Visibility Rule**: Users can see leave applications for employees they have access to.

```csharp
public async Task<IQueryable<LeaveApplication>> GetAccessibleLeaveApplications(
    Employee currentUser)
{
    var accessibleEmployeeIds = await _context.Employees
        .Where(e => 
            (currentUser.RoleGroup.CanAccessAcrossStations || 
             e.StationId == currentUser.StationId) &&
            (currentUser.RoleGroup.CanAccessAcrossDepartments || 
             e.DepartmentId == currentUser.DepartmentId))
        .Select(e => e.PayrollNo)
        .ToListAsync();
    
    using var leaveContext = new KtdaleaveContext();
    return leaveContext.LeaveApplications
        .Where(la => accessibleEmployeeIds.Contains(la.PayrollNo));
}
```

---

## Field-Level Visibility

### Sensitive Information
Some fields may be hidden based on permission levels even when entity is visible:

| Field Type | Visibility Rule |
|------------|----------------|
| **Personal Information** | Own record + HR + direct supervisor |
| **Salary Information** | HR + station manager + system admin |
| **Performance Data** | Direct supervisor + HR + station manager |
| **Contact Information** | Own record + direct supervisor + HR |

```csharp
public EmployeeDto FilterSensitiveEmployeeData(
    Employee employee, 
    Employee currentUser)
{
    var dto = new EmployeeDto
    {
        PayrollNo = employee.PayrollNo,
        Name = employee.Name,
        DepartmentId = employee.DepartmentId,
        StationId = employee.StationId
    };
    
    // Add sensitive data based on permissions
    if (IsHRUser(currentUser) || 
        IsDirectSupervisor(employee, currentUser) || 
        employee.PayrollNo == currentUser.PayrollNo)
    {
        dto.PersonalEmail = employee.PersonalEmail;
        dto.PhoneNumber = employee.PhoneNumber;
    }
    
    if (IsHRUser(currentUser) || IsStationManager(currentUser))
    {
        dto.SalaryGrade = employee.SalaryGrade;
    }
    
    return dto;
}
```

---

## Visibility Caching Strategy

### Cache Keys
```csharp
public static class CacheKeys
{
    public static string UserVisibleEmployees(string payrollNo) 
        => $"visible_employees_{payrollNo}";
    
    public static string UserVisibleMaterials(string payrollNo) 
        => $"visible_materials_{payrollNo}";
    
    public static string UserVisibleRequisitions(string payrollNo) 
        => $"visible_requisitions_{payrollNo}";
}
```

### Cache Invalidation Rules
- **Role Group Change**: Invalidate all visibility caches for affected user
- **Organization Change**: Invalidate caches for users in affected station/department
- **Assignment Change**: Invalidate material and assignment caches for involved users
- **Time-Based**: Auto-expire caches after 1 hour to handle edge cases

---

## Performance Considerations

### Query Optimization
1. **Index Strategy**: Ensure indexes on `StationId`, `DepartmentId` for all entities
2. **Query Filtering**: Apply visibility filters before other query operations
3. **Batch Loading**: Use `Include()` statements for related data within visibility scope
4. **Projection**: Select only necessary fields for visibility-filtered queries

### Memory Usage
1. **Lazy Loading**: Disable for visibility-filtered queries to prevent N+1 problems
2. **Result Limiting**: Apply `Take()` operations after visibility filtering
3. **Streaming**: Use `IAsyncEnumerable` for large result sets

### Database Load
1. **Connection Pooling**: Reuse connections for multiple visibility queries
2. **Query Plan Caching**: Use parameterized queries for consistent execution plans
3. **Statistics Updates**: Keep table statistics current for optimal query plans

---

## Security Implications

### Data Leakage Prevention
- **Query Interception**: All queries automatically filtered through visibility service
- **API Response Filtering**: Additional filtering at API layer for defense in depth
- **Logging**: Log attempts to access restricted entities for security monitoring

### Audit Requirements
- **Access Logging**: Log all entity access attempts with permission context
- **Query Auditing**: Maintain audit trail of who accessed what entities when
- **Permission Changes**: Log all changes to role groups and their impact on visibility

---

## Testing Matrix

### Unit Tests
- **Permission Combinations**: Test all 4 permission combinations for each entity
- **Edge Cases**: Test boundary conditions (empty results, single records)
- **Performance**: Verify query performance with large datasets

### Integration Tests
- **Cross-Entity**: Test visibility across related entities (employee → assignments → materials)
- **Multi-Context**: Test visibility across database contexts
- **Caching**: Test cache invalidation and refresh scenarios

### User Acceptance Tests
- **Role-Based Scenarios**: Test common user workflows with different permission levels
- **Data Consistency**: Verify users see consistent data across different views
- **Error Handling**: Test user experience when accessing restricted data

---

*Related Documentation:*
- *[PermissionMatrix_QuickRef.md](../Authorization/PermissionMatrix_QuickRef.md) - Quick permission reference*
- *[WorkflowAuthorization.md](WorkflowAuthorization.md) - Workflow-specific authorization rules*
- *[VisibilityAuthorizeService_Reference.md](../Services/VisibilityAuthorizeService_Reference.md) - Service implementation*