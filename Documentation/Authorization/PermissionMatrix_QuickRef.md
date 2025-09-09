# Permission Matrix Quick Reference

## Overview
Fast reference guide for role group permissions in the MRIV system. This matrix shows what each permission combination allows users to access.

---

## Permission Flag Combinations

### Station-Level Access (CanAccessAcrossStations = false)
| Department Access | Description | Access Scope |
|------------------|-------------|--------------|
| ✅ Cross-Department | Can access all data within assigned station | Station → All Departments → All Employees |
| ❌ Single Department | Can access only own department data | Station → Own Department → All Employees |

### Multi-Station Access (CanAccessAcrossStations = true)
| Department Access | Description | Access Scope |
|------------------|-------------|--------------|
| ✅ Cross-Department | **FULL SYSTEM ACCESS** - All stations, departments, employees | System-wide |
| ❌ Single Department | Can access same department across all stations | All Stations → Own Department → All Employees |

---

## Quick Decision Matrix

```
┌─────────────────────────┬──────────────────┬───────────────────┐
│     Access Pattern      │   Station Flag   │  Department Flag  │
├─────────────────────────┼──────────────────┼───────────────────┤
│ Own Department Only     │      FALSE       │       FALSE       │
│ Own Station All Depts   │      FALSE       │       TRUE        │
│ Same Dept All Stations │      TRUE        │       FALSE       │
│ Full System Access     │      TRUE        │       TRUE        │
└─────────────────────────┴──────────────────┴───────────────────┘
```

---

## Common Access Patterns

### 🔴 Most Restrictive
**Single Department, Single Station**
- Flags: `CanAccessAcrossStations = false, CanAccessAcrossDepartments = false`
- Access: Only their own department within their assigned station
- Use Case: Regular employees, department-specific roles

### 🟡 Station Manager
**All Departments, Single Station**
- Flags: `CanAccessAcrossStations = false, CanAccessAcrossDepartments = true`
- Access: All departments within their assigned station
- Use Case: Station supervisors, station managers

### 🟠 Department Head (Multi-Station)
**Same Department, All Stations**
- Flags: `CanAccessAcrossStations = true, CanAccessAcrossDepartments = false`
- Access: Same department across all stations
- Use Case: Department heads managing multiple locations

### 🟢 System Administrator
**Full Access**
- Flags: `CanAccessAcrossStations = true, CanAccessAcrossDepartments = true`
- Access: Complete system access
- Use Case: System admins, senior management

---

## Entity Visibility Examples

### Employee Access
```csharp
// User with Station=1, Department=HR
CanAccessAcrossStations = false, CanAccessAcrossDepartments = false
→ Can see: Employees in Station 1, HR Department only

CanAccessAcrossStations = false, CanAccessAcrossDepartments = true  
→ Can see: Employees in Station 1, All Departments

CanAccessAcrossStations = true, CanAccessAcrossDepartments = false
→ Can see: Employees in HR Department, All Stations

CanAccessAcrossStations = true, CanAccessAcrossDepartments = true
→ Can see: All Employees, All Stations, All Departments
```

### Material Requisition Access
```csharp
// Requisitions follow same pattern as employees
// User's StationId and DepartmentId determine base access
// Permission flags expand or restrict visibility accordingly
```

---

## Field-Level Permission Matrix

| Entity Field | Access Level | Permission Required |
|-------------|--------------|-------------------|
| `StationId` | View | User's station OR `CanAccessAcrossStations = true` |
| `DepartmentId` | View | User's department OR `CanAccessAcrossDepartments = true` |
| `PayrollNo` | View | Employee in accessible scope |
| `CreatedBy` | Edit | Entity creator OR sufficient permissions |
| `LastModifiedBy` | View | Always visible if entity is accessible |

---

## Authorization Decision Tree

```
START: User requests entity access
    │
    ├─ Is user's station = entity.station?
    │   ├─ YES → Check department access
    │   └─ NO → Check CanAccessAcrossStations
    │       ├─ TRUE → Check department access
    │       └─ FALSE → DENY ACCESS
    │
    └─ Department Access Check:
        ├─ Is user's department = entity.department?
        │   ├─ YES → GRANT ACCESS
        │   └─ NO → Check CanAccessAcrossDepartments
        │       ├─ TRUE → GRANT ACCESS
        │       └─ FALSE → DENY ACCESS
```

---

## Performance Optimization Notes

### Query Filters
- **Station Filter**: Applied first when `CanAccessAcrossStations = false`
- **Department Filter**: Applied second when `CanAccessAcrossDepartments = false`
- **Index Usage**: Ensure indexes on `StationId` and `DepartmentId` for optimal performance

### Caching Strategy
- Cache permission flags per user session
- Invalidate cache on role group changes
- Consider query result caching for frequently accessed data

---

## Security Considerations

### ⚠️ High-Risk Combinations
- **Full Access Roles**: Monitor and audit `CanAccessAcrossStations = true, CanAccessAcrossDepartments = true`
- **Cross-Station Access**: Review users with `CanAccessAcrossStations = true` regularly
- **Privilege Escalation**: Prevent unauthorized permission flag modifications

### 🛡️ Best Practices
1. **Principle of Least Privilege**: Grant minimum necessary permissions
2. **Regular Audits**: Review role assignments quarterly
3. **Activity Logging**: Log all permission-sensitive operations
4. **Change Management**: Require approval for permission modifications

---

## Troubleshooting Common Issues

### "Access Denied" Errors
1. **Check User's Station Assignment**: Verify `Employee.StationId`
2. **Check User's Department Assignment**: Verify `Employee.DepartmentId` 
3. **Verify Role Group Flags**: Check `CanAccessAcrossStations` and `CanAccessAcrossDepartments`
4. **Review Entity Ownership**: Check `CreatedBy` field for entity-specific access

### Performance Issues
1. **Missing Indexes**: Ensure indexes on `StationId`, `DepartmentId`
2. **Query Complexity**: Use `VisibilityAuthorizeService` for optimized queries
3. **Cache Misses**: Verify permission caching is working correctly

### Inconsistent Access
1. **Session State**: Check if user permissions cached correctly
2. **Database Sync**: Verify role group changes propagated
3. **Context Issues**: Ensure correct database context usage

---

## Quick Reference Commands

### Check User Permissions
```csharp
var canCrossStation = user.RoleGroup.CanAccessAcrossStations;
var canCrossDept = user.RoleGroup.CanAccessAcrossDepartments;
Console.WriteLine($"Station Access: {(canCrossStation ? "Multi" : "Single")}");
Console.WriteLine($"Department Access: {(canCrossDept ? "Multi" : "Single")}");
```

### Apply Filters to Query
```csharp
var query = _visibilityService.ApplyEmployeeVisibilityFilter(
    dbContext.Employees, 
    currentUser
);
```

### Check Entity Access
```csharp
var hasAccess = _visibilityService.CanUserAccessEntity(
    entity.StationId, 
    entity.DepartmentId, 
    currentUser
);
```

---

*For detailed implementation examples, see [VisibilityAuthorizeService_Reference.md](../Services/VisibilityAuthorizeService_Reference.md)*

*For comprehensive test scenarios, see [AuthorizationTestMatrix.md](../Testing/AuthorizationTestMatrix.md)*