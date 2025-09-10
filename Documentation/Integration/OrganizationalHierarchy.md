# Organizational Hierarchy Integration

## Overview
This document details how the MRIV authorization system integrates with the organizational hierarchy. It covers the relationship between Stations, Departments, Employees, and how these structures affect data visibility and permissions.

---

## Hierarchical Structure

### Organizational Levels
```
Organization (KTDA)
├── Station 1
│   ├── Department A
│   │   ├── Employee 1 (PayrollNo: 12345678)
│   │   ├── Employee 2 (PayrollNo: 87654321)
│   │   └── Employee N...
│   ├── Department B
│   │   ├── Employee 3
│   │   └── Employee N...
│   └── Department N...
├── Station 2
│   ├── Department C
│   └── Department N...
└── Station N...
```

### Entity Relationships
Based on your actual models:

#### Station Entity (`Models\Station.cs`)
```csharp
public class Station
{
    public int StationId { get; set; }      // Primary Key
    public string StationName { get; set; } // Station name
    public string? StationCode { get; set; } // Optional station code
    
    // Navigation Properties
    public virtual ICollection<Department> Departments { get; set; }
    public virtual ICollection<EmployeeBkp> Employees { get; set; }
}
```

#### Department Entity (`Models\Department.cs`)
```csharp
public class Department
{
    public int DepartmentId { get; set; }     // Primary Key
    public string DepartmentName { get; set; } // Department name
    public int StationId { get; set; }        // Foreign Key to Station
    public string DepartmentHd { get; set; }  // Department Head PayrollNo
    
    // Navigation Properties
    public virtual Station Station { get; set; }
    public virtual ICollection<EmployeeBkp> Employees { get; set; }
}
```

#### Employee Entity (`Models\EmployeeBkp.cs`)
```csharp
public class EmployeeBkp
{
    public string PayrollNo { get; set; }    // Primary Key (8 chars)
    public string RollNo { get; set; }       // Secondary Key (5 chars)
    public string? SurName { get; set; }     // Last name
    public string? OtherNames { get; set; }  // First/middle names
    public string? Department { get; set; }  // Department name (string)
    public string? Station { get; set; }     // Station name (string)
    public string? Designation { get; set; } // Job title
    public string? Hod { get; set; }         // Head of Department PayrollNo
    public string? Supervisor { get; set; }  // Direct supervisor PayrollNo
    public string? Role { get; set; }        // Role designation
    public string? EmailAddress { get; set; } // Email
    public DateTime? HireDate { get; set; }  // Employment start date
    public int? EmpisCurrActive { get; set; } // Active status
    public string Fullname { get; set; }     // Computed: SurName + OtherNames
}
```

---

## Data Integration Patterns

### Employee-Department-Station Relationship
The current implementation uses **string-based references** rather than foreign keys:

```csharp
// Current pattern in EmployeeBkp
public class EmployeeBkp
{
    public string? Department { get; set; }  // Department NAME, not ID
    public string? Station { get; set; }     // Station NAME, not ID
}

// But authorization logic expects integer IDs
// Integration requires name-to-ID mapping
```

### Authorization Integration Challenges
```csharp
public async Task<EmployeeBkp> GetEmployeeWithHierarchyInfo(string payrollNo)
{
    var employee = await _context.EmployeeBkps
        .FirstOrDefaultAsync(e => e.PayrollNo == payrollNo);
    
    if (employee == null) return null;
    
    // Map string names to IDs for authorization
    var station = await _context.Stations
        .FirstOrDefaultAsync(s => s.StationName == employee.Station);
    
    var department = await _context.Departments
        .FirstOrDefaultAsync(d => d.DepartmentName == employee.Department 
                                 && d.StationId == station?.StationId);
    
    // Create authorization-compatible employee object
    return new Employee
    {
        PayrollNo = employee.PayrollNo,
        StationId = station?.StationId ?? 0,
        DepartmentId = department?.DepartmentId ?? 0,
        Name = employee.Fullname,
        // ... other properties
    };
}
```

---

## Hierarchy-Based Authorization

### Management Chain Resolution
```csharp
public class HierarchyService
{
    public async Task<List<EmployeeBkp>> GetManagementChain(string payrollNo)
    {
        var chain = new List<EmployeeBkp>();
        var employee = await GetEmployeeByPayrollAsync(payrollNo);
        
        while (employee != null)
        {
            chain.Add(employee);
            
            // Follow supervisor chain
            if (!string.IsNullOrEmpty(employee.Supervisor))
            {
                employee = await GetEmployeeByPayrollAsync(employee.Supervisor);
            }
            else if (!string.IsNullOrEmpty(employee.Hod) && employee.Hod != employee.PayrollNo)
            {
                // If no supervisor, go to HOD
                employee = await GetEmployeeByPayrollAsync(employee.Hod);
            }
            else
            {
                break; // Top of chain reached
            }
        }
        
        return chain;
    }
}
```

### Department Head Identification
```csharp
public async Task<bool> IsHeadOfDepartment(string payrollNo, string departmentName)
{
    var department = await _context.Departments
        .FirstOrDefaultAsync(d => d.DepartmentName == departmentName);
    
    return department?.DepartmentHd == payrollNo;
}

public async Task<List<string>> GetDepartmentHeadResponsibilities(string payrollNo)
{
    var departments = await _context.Departments
        .Where(d => d.DepartmentHd == payrollNo)
        .Select(d => d.DepartmentName)
        .ToListAsync();
    
    return departments;
}
```

---

## Cross-Context Data Integration

### Employee Data Synchronization
The system integrates two database contexts:

#### KtdaleaveContext (Primary Employee Data)
- **Source**: HR/Leave management system
- **Contains**: Employee master data, organizational structure
- **Usage**: Authentication, employee lookups, hierarchy resolution

#### RequisitionContext (Application Data)
- **Source**: MRIV application database
- **Contains**: Materials, requisitions, assignments, role groups
- **Usage**: Business logic, authorization, workflow management

### Integration Service Pattern
```csharp
public class EmployeeIntegrationService
{
    private readonly KtdaleaveContext _hrContext;
    private readonly RequisitionContext _appContext;
    
    public async Task<Employee> GetIntegratedEmployee(string payrollNo)
    {
        // Get base employee data from HR system
        var hrEmployee = await _hrContext.EmployeeBkps
            .FirstOrDefaultAsync(e => e.PayrollNo == payrollNo);
        
        if (hrEmployee == null) return null;
        
        // Get role group assignment from app system
        var roleGroupMember = await _appContext.RoleGroupMembers
            .Include(m => m.RoleGroup)
            .FirstOrDefaultAsync(m => m.PayrollNo == payrollNo && m.IsActive);
        
        // Resolve organizational IDs
        var stationId = await ResolveStationId(hrEmployee.Station);
        var departmentId = await ResolveDepartmentId(hrEmployee.Department, stationId);
        
        return new Employee
        {
            PayrollNo = hrEmployee.PayrollNo,
            FullName = hrEmployee.Fullname,
            StationId = stationId,
            DepartmentId = departmentId,
            RoleGroup = roleGroupMember?.RoleGroup,
            Supervisor = hrEmployee.Supervisor,
            IsActive = hrEmployee.EmpisCurrActive == 1
        };
    }
}
```

---

## Hierarchy-Based Visibility Rules

### Supervisory Access
```csharp
public async Task<IQueryable<EmployeeBkp>> GetSupervisoryAccess(string supervisorPayrollNo)
{
    var directReports = _context.EmployeeBkps
        .Where(e => e.Supervisor == supervisorPayrollNo);
    
    var departmentReports = _context.EmployeeBkps
        .Where(e => e.Hod == supervisorPayrollNo);
    
    return directReports.Union(departmentReports);
}
```

### Departmental Boundaries
```csharp
public async Task<bool> CanAccessDepartment(string userPayrollNo, string targetDepartment)
{
    var user = await GetIntegratedEmployee(userPayrollNo);
    
    // Same department - always allowed
    if (user.Department == targetDepartment)
        return true;
    
    // Cross-department permission required
    if (user.RoleGroup?.CanAccessAcrossDepartments == true)
    {
        // Check if departments are in same station (if user can't cross stations)
        if (!user.RoleGroup.CanAccessAcrossStations)
        {
            var targetDeptStation = await GetStationForDepartment(targetDepartment);
            return targetDeptStation == user.Station;
        }
        return true;
    }
    
    // Check if user is HOD of target department
    return await IsHeadOfDepartment(userPayrollNo, targetDepartment);
}
```

---

## Station-Level Operations

### Multi-Station Management
```csharp
public class StationManagementService
{
    public async Task<List<Station>> GetAccessibleStations(string userPayrollNo)
    {
        var user = await GetIntegratedEmployee(userPayrollNo);
        
        if (user.RoleGroup?.CanAccessAcrossStations == true)
        {
            // User can see all stations
            return await _context.Stations.ToListAsync();
        }
        
        // User can only see their own station
        var userStation = await _context.Stations
            .FirstOrDefaultAsync(s => s.StationName == user.Station);
        
        return userStation != null ? new List<Station> { userStation } : new List<Station>();
    }
    
    public async Task<bool> CanManageStation(string userPayrollNo, int stationId)
    {
        var user = await GetIntegratedEmployee(userPayrollNo);
        
        // System admin can manage all stations
        if (user.RoleGroup?.CanAccessAcrossStations == true && 
            user.RoleGroup?.CanAccessAcrossDepartments == true)
            return true;
        
        // Station managers can manage their own station
        var station = await _context.Stations.FindAsync(stationId);
        return station?.StationName == user.Station && 
               user.RoleGroup?.CanAccessAcrossDepartments == true;
    }
}
```

---

## Data Consistency Patterns

### Name-to-ID Resolution
```csharp
public class HierarchyMappingService
{
    private readonly IMemoryCache _cache;
    
    public async Task<int> ResolveStationId(string stationName)
    {
        if (string.IsNullOrEmpty(stationName)) return 0;
        
        var cacheKey = $"station_id_{stationName}";
        if (_cache.TryGetValue(cacheKey, out int cachedId))
            return cachedId;
        
        var station = await _context.Stations
            .FirstOrDefaultAsync(s => s.StationName.Equals(stationName, 
                StringComparison.OrdinalIgnoreCase));
        
        var stationId = station?.StationId ?? 0;
        _cache.Set(cacheKey, stationId, TimeSpan.FromMinutes(30));
        
        return stationId;
    }
    
    public async Task<int> ResolveDepartmentId(string departmentName, int stationId)
    {
        if (string.IsNullOrEmpty(departmentName) || stationId == 0) return 0;
        
        var cacheKey = $"dept_id_{departmentName}_{stationId}";
        if (_cache.TryGetValue(cacheKey, out int cachedId))
            return cachedId;
        
        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.DepartmentName.Equals(departmentName, 
                StringComparison.OrdinalIgnoreCase) && d.StationId == stationId);
        
        var departmentId = department?.DepartmentId ?? 0;
        _cache.Set(cacheKey, departmentId, TimeSpan.FromMinutes(30));
        
        return departmentId;
    }
}
```

### Synchronization Strategies
```csharp
public class HierarchySyncService
{
    public async Task ValidateHierarchyConsistency()
    {
        var inconsistencies = new List<string>();
        
        // Check for employees with invalid station names
        var invalidStations = await _hrContext.EmployeeBkps
            .Where(e => !string.IsNullOrEmpty(e.Station))
            .Select(e => e.Station)
            .Distinct()
            .Where(station => !_appContext.Stations
                .Any(s => s.StationName == station))
            .ToListAsync();
        
        if (invalidStations.Any())
        {
            inconsistencies.Add($"Stations not found in master data: {string.Join(", ", invalidStations)}");
        }
        
        // Check for employees with invalid department names
        var invalidDepartments = await _hrContext.EmployeeBkps
            .Where(e => !string.IsNullOrEmpty(e.Department))
            .Select(e => new { e.Department, e.Station })
            .Distinct()
            .Where(ed => !_appContext.Departments
                .Any(d => d.DepartmentName == ed.Department &&
                         d.Station.StationName == ed.Station))
            .ToListAsync();
        
        if (invalidDepartments.Any())
        {
            inconsistencies.Add($"Invalid department-station combinations found: {invalidDepartments.Count} records");
        }
        
        // Log inconsistencies for resolution
        foreach (var issue in inconsistencies)
        {
            _logger.LogWarning("Hierarchy Consistency Issue: {Issue}", issue);
        }
    }
}
```

---

## Performance Optimization

### Hierarchy Caching
```csharp
public class HierarchyCacheService
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(30);
    
    public async Task<EmployeeBkp> GetCachedEmployee(string payrollNo)
    {
        var cacheKey = $"employee_{payrollNo}";
        
        if (_cache.TryGetValue(cacheKey, out EmployeeBkp cachedEmployee))
            return cachedEmployee;
        
        var employee = await _hrContext.EmployeeBkps
            .FirstOrDefaultAsync(e => e.PayrollNo == payrollNo);
        
        if (employee != null)
        {
            _cache.Set(cacheKey, employee, _cacheExpiry);
        }
        
        return employee;
    }
    
    public async Task<List<EmployeeBkp>> GetDepartmentEmployees(string departmentName)
    {
        var cacheKey = $"dept_employees_{departmentName}";
        
        if (_cache.TryGetValue(cacheKey, out List<EmployeeBkp> cachedEmployees))
            return cachedEmployees;
        
        var employees = await _hrContext.EmployeeBkps
            .Where(e => e.Department == departmentName && e.EmpisCurrActive == 1)
            .ToListAsync();
        
        _cache.Set(cacheKey, employees, _cacheExpiry);
        
        return employees;
    }
}
```

### Query Optimization
```csharp
public class OptimizedHierarchyQueries
{
    // Pre-compiled queries for better performance
    private static readonly Func<KtdaleaveContext, string, Task<EmployeeBkp>> GetEmployeeQuery =
        EF.CompileAsyncQuery((KtdaleaveContext context, string payrollNo) =>
            context.EmployeeBkps.FirstOrDefault(e => e.PayrollNo == payrollNo));
    
    private static readonly Func<KtdaleaveContext, string, IEnumerable<EmployeeBkp>> GetDepartmentEmployeesQuery =
        EF.CompileQuery((KtdaleaveContext context, string department) =>
            context.EmployeeBkps.Where(e => e.Department == department && e.EmpisCurrActive == 1));
    
    public async Task<EmployeeBkp> GetEmployee(string payrollNo)
    {
        return await GetEmployeeQuery(_context, payrollNo);
    }
    
    public IEnumerable<EmployeeBkp> GetDepartmentEmployees(string department)
    {
        return GetDepartmentEmployeesQuery(_context, department);
    }
}
```

---

## Integration Testing

### Hierarchy Resolution Tests
```csharp
[Test]
public async Task Should_Resolve_Employee_Hierarchy_Correctly()
{
    // Arrange
    var testEmployee = CreateTestEmployee();
    var testStation = CreateTestStation("Test Station");
    var testDepartment = CreateTestDepartment("Test Department", testStation.StationId);
    
    // Act
    var resolvedEmployee = await _hierarchyService.GetIntegratedEmployee(testEmployee.PayrollNo);
    
    // Assert
    Assert.That(resolvedEmployee.StationId, Is.EqualTo(testStation.StationId));
    Assert.That(resolvedEmployee.DepartmentId, Is.EqualTo(testDepartment.DepartmentId));
}

[Test]
public async Task Should_Handle_Missing_Hierarchy_Data_Gracefully()
{
    // Arrange
    var employeeWithInvalidStation = new EmployeeBkp
    {
        PayrollNo = "TEST0001",
        Station = "Non-Existent Station",
        Department = "Non-Existent Department"
    };
    
    // Act
    var resolvedEmployee = await _hierarchyService.GetIntegratedEmployee(employeeWithInvalidStation.PayrollNo);
    
    // Assert
    Assert.That(resolvedEmployee.StationId, Is.EqualTo(0));
    Assert.That(resolvedEmployee.DepartmentId, Is.EqualTo(0));
}
```

---

## Migration and Data Cleanup

### Hierarchy Standardization
```csharp
public class HierarchyStandardizationService
{
    public async Task StandardizeStationNames()
    {
        // Get all unique station names from employee data
        var employeeStations = await _hrContext.EmployeeBkps
            .Where(e => !string.IsNullOrEmpty(e.Station))
            .Select(e => e.Station.Trim())
            .Distinct()
            .ToListAsync();
        
        // Get all station names from master data
        var masterStations = await _appContext.Stations
            .Select(s => s.StationName.Trim())
            .ToListAsync();
        
        // Find stations that need standardization
        var stationsNeedingUpdate = employeeStations
            .Where(es => !masterStations.Any(ms => 
                string.Equals(ms, es, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        
        // Log for manual review
        foreach (var station in stationsNeedingUpdate)
        {
            _logger.LogWarning("Station name requires standardization: {Station}", station);
        }
    }
}
```

---

*Related Documentation:*
- *[RoleGroupAuthorizationSystem.md](../Authorization/RoleGroupAuthorizationSystem.md) - Core authorization framework*
- *[EntityVisibilityMatrix.md](EntityVisibilityMatrix.md) - Entity-specific visibility rules*
- *[WorkflowAuthorization.md](WorkflowAuthorization.md) - Workflow integration patterns*