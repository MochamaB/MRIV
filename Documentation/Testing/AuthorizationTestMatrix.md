# Authorization Test Matrix - Master Test Scenarios

## Overview

This document provides comprehensive test scenarios for all authorization combinations in the MRIV system. Use this as a reference for creating test cases, validating system behavior, and ensuring complete test coverage for role group permissions.

## Test Environment Setup

### Prerequisites

1. **Test Database**: Isolated test database with clean data
2. **Test Users**: Standardized test accounts for each role type
3. **Test Data**: Sample organizational structure and role groups
4. **Test Tools**: Unit test framework and integration test setup

### Model Integration Notes

The MRIV system has a unique architecture where employee data comes from `KtdaleaveContext` while authorization data resides in `RequisitionContext`. This creates specific testing challenges:

#### Key Model Relationships
- **EmployeeBkp** (from KtdaleaveContext): Uses string-based `Department` and `Station` names
- **Departments/Stations** (from RequisitionContext): Uses integer IDs (`DepartmentId`, `StationId`)
- **RoleGroups/RoleGroupMembers** (from RequisitionContext): Authorization data linked by `PayrollNo`

#### Testing Integration Points
```csharp
// Employee data comes from HR system (string-based)
var employee = await _ktdaContext.EmployeeBkps
    .FirstOrDefaultAsync(e => e.PayrollNo == "TST00001");
// employee.Department = "Information Technology" (string)
// employee.Station = "Head Office" (string)

// Authorization requires ID-based mapping
var department = await _appContext.Departments
    .FirstOrDefaultAsync(d => d.DepartmentName == employee.Department);
// department.DepartmentId = 1 (integer for authorization logic)
```

#### Test Data Consistency Requirements
1. **Name-ID Mapping**: Department/Station names in EmployeeBkp must match master data
2. **PayrollNo Format**: 8-character format (e.g., "TST00001") 
3. **RollNo Requirement**: 5-character secondary key required for EmployeeBkp
4. **Active Status**: Use `EmpisCurrActive = 1` for active employees

### Standard Test Data Structure

#### Test Stations
```sql
-- Standard test stations (using actual Stations table)
INSERT INTO Stations (StationId, StationName) VALUES
(1, 'Head Office'),
(2, 'Factory A'),
(3, 'Factory B'),
(4, 'Region Central');
```

#### Test Departments
```sql
-- Standard test departments (using actual Departments table)
INSERT INTO Departments (DepartmentId, DepartmentName, StationId, DepartmentHd) VALUES
(1, 'Information Technology', 1, 'HOD0001'),
(2, 'Human Resources', 1, 'HOD0002'),
(3, 'Finance', 1, 'HOD0003'),
(4, 'Operations', 2, 'HOD0004'),
(5, 'Maintenance', 2, 'HOD0005');
```

#### Test Employees
```sql
-- Test users for each authorization scenario (using actual EmployeeBkp table)
INSERT INTO Employee_bkp (PayrollNo, RollNo, SurName, OtherNames, Department, Station, Role, EmpisCurrActive) VALUES
('TST00001', 'R0001', 'Default', 'User', 'Information Technology', 'Head Office', 'User', 1),           -- Default User
('TST00002', 'R0002', 'Dept', 'Manager', 'Information Technology', 'Head Office', 'Manager', 1),        -- Department Manager
('TST00003', 'R0003', 'Station', 'Support', 'Information Technology', 'Head Office', 'Support', 1),     -- Station Support
('TST00004', 'R0004', 'Group', 'Manager', 'Information Technology', 'Head Office', 'Manager', 1),       -- Group Manager
('TST00005', 'R0005', 'System', 'Admin', 'Information Technology', 'Head Office', 'Admin', 1),          -- Administrator
('TST00006', 'R0006', 'Cross', 'Dept', 'Human Resources', 'Head Office', 'User', 1),                    -- Different Department
('TST00007', 'R0007', 'Cross', 'Station', 'Information Technology', 'Factory A', 'User', 1);           -- Different Station
```

## Test Role Groups

### Standard Test Role Groups
```sql
-- Role groups for testing all permission combinations
INSERT INTO RoleGroups (Name, Description, CanAccessAcrossStations, CanAccessAcrossDepartments, IsActive) VALUES
('Test_Department_Manager', 'Department level access only', 0, 0, 1),
('Test_Station_Support', 'Station level access', 0, 1, 1),
('Test_Group_Manager', 'Cross-station department access', 1, 0, 1),
('Test_Administrator', 'Full system access', 1, 1, 1);

-- Role group memberships (using actual PayrollNo format)
INSERT INTO RoleGroupMembers (RoleGroupId, PayrollNo, IsActive, CreatedAt) VALUES
(1, 'TST00002', 1, GETDATE()), -- Department Manager
(2, 'TST00003', 1, GETDATE()), -- Station Support
(3, 'TST00004', 1, GETDATE()), -- Group Manager
(4, 'TST00005', 1, GETDATE()); -- Administrator
-- TST00001 has no role group (Default User)
```

### Test Sample Data
```sql
-- Sample requisitions for testing visibility (using actual Requisitions table columns)
INSERT INTO Requisitions (TicketId, DepartmentId, PayrollNo, RequisitionType, IssueStationCategory, IssueStationId, DeliveryStationCategory, DeliveryStationId, Remarks, Status, CreatedAt) VALUES
(1001, 1, 'TST00001', 0, 'Internal', 1, 'Internal', 1, 'Default user requisition', 0, GETDATE()),
(1002, 2, 'TST00006', 0, 'Internal', 1, 'Internal', 1, 'Different department requisition', 0, GETDATE()),
(1003, 1, 'TST00007', 0, 'Internal', 2, 'Internal', 2, 'Different station requisition', 0, GETDATE()),
(1004, 1, 'TST00002', 0, 'Internal', 1, 'Internal', 1, 'Department manager requisition', 0, GETDATE());

-- Sample approvals for testing (using actual Approvals table columns)
INSERT INTO Approvals (RequisitionId, ApprovalStep, PayrollNo, Status, CreatedAt) VALUES
(1, 1, 'TST00001', 'Pending', GETDATE()),
(2, 1, 'TST00006', 'Pending', GETDATE()),
(3, 1, 'TST00007', 'Pending', GETDATE());
```

## Comprehensive Test Matrix

### Test Scenario Categories

1. **Permission Combination Tests** - All 4 permission flag combinations
2. **Cross-Department Tests** - Access across different departments
3. **Cross-Station Tests** - Access across different stations
4. **Edge Case Tests** - Boundary conditions and error scenarios
5. **Performance Tests** - Large dataset scenarios
6. **Security Tests** - Unauthorized access attempts

## 1. Permission Combination Tests

### Test Case TC-AUTH-001: Default User (No Role Group)
**Setup:**
- User: TST00001 (no role group assignment)
- Department: Information Technology, Station: Head Office
- Employee: PayrollNo='TST00001', Department='Information Technology', Station='Head Office'

**Test Scenarios:**
```csharp
[Test]
public async Task DefaultUser_CanOnlyAccessOwnData()
{
    // Arrange
    var userPayrollNo = "TST00001";
    
    // Act
    var visibleRequisitions = await _visibilityService
        .ApplyVisibilityScopeAsync(_context.Requisitions, userPayrollNo);
    var results = await visibleRequisitions.ToListAsync();
    
    // Assert
    Assert.All(results, r => Assert.Equal(userPayrollNo, r.PayrollNo));
    Assert.DoesNotContain(results, r => r.PayrollNo != userPayrollNo);
}
```

**Expected Results:**
- ✅ Can see own requisitions/approvals only
- ❌ Cannot see other users' data in same department
- ❌ Cannot see other departments' data
- ❌ Cannot see other stations' data

### Test Case TC-AUTH-002: Department Manager
**Setup:**
- User: TST00002 (Department Manager role group)
- Employee: PayrollNo='TST00002', Department='Information Technology', Station='Head Office'
- Role Group: 'Test_Department_Manager'
- Permissions: CanAccessAcrossStations=false, CanAccessAcrossDepartments=false

**Test Scenarios:**
```csharp
[Test]
public async Task DepartmentManager_CanAccessDepartmentAtStation()
{
    // Arrange
    var userPayrollNo = "TST00002";
    
    // Act
    var visibleRequisitions = await _visibilityService
        .ApplyVisibilityScopeAsync(_context.Requisitions, userPayrollNo);
    var results = await visibleRequisitions.ToListAsync();
    
    // Assert
    Assert.All(results, r => r.DepartmentId == 1); // Information Technology Department only
    Assert.All(results, r => r.IssueStationId == 1 || r.DeliveryStationId == 1); // Head Office only
}
```

**Expected Results:**
- ✅ Can see all Information Technology department data at Head Office
- ❌ Cannot see Human Resources department data at Head Office
- ❌ Cannot see Information Technology department data at other stations

### Test Case TC-AUTH-003: Station Support
**Setup:**
- User: TST00003 (Station Support role group)
- Employee: PayrollNo='TST00003', Department='Information Technology', Station='Head Office'
- Role Group: 'Test_Station_Support'
- Permissions: CanAccessAcrossStations=false, CanAccessAcrossDepartments=true

**Test Scenarios:**
```csharp
[Test]
public async Task StationSupport_CanAccessAllDepartmentsAtStation()
{
    // Arrange
    var userPayrollNo = "TST00003";
    
    // Act
    var visibleDepartments = await _visibilityService
        .GetVisibleDepartmentsAsync(userPayrollNo);
    var visibleRequisitions = await _visibilityService
        .ApplyVisibilityScopeAsync(_context.Requisitions, userPayrollNo);
    
    // Assert
    Assert.Contains(visibleDepartments, d => d.DepartmentId == 1); // Information Technology
    Assert.Contains(visibleDepartments, d => d.DepartmentId == 2); // Human Resources
    
    var results = await visibleRequisitions.ToListAsync();
    Assert.All(results, r => r.IssueStationId == 1 || r.DeliveryStationId == 1); // Head Office only
}
```

**Expected Results:**
- ✅ Can see all departments at Head Office
- ❌ Cannot see any departments at other stations

### Test Case TC-AUTH-004: Group Manager
**Setup:**
- User: TST00004 (Group Manager role group)
- Employee: PayrollNo='TST00004', Department='Information Technology', Station='Head Office'
- Role Group: 'Test_Group_Manager'
- Permissions: CanAccessAcrossStations=true, CanAccessAcrossDepartments=false

**Test Scenarios:**
```csharp
[Test]
public async Task GroupManager_CanAccessDepartmentAtAllStations()
{
    // Arrange
    var userPayrollNo = "TST00004";
    
    // Act
    var visibleStations = await _visibilityService
        .GetVisibleStationsAsync(userPayrollNo);
    var visibleRequisitions = await _visibilityService
        .ApplyVisibilityScopeAsync(_context.Requisitions, userPayrollNo);
    
    // Assert
    Assert.True(visibleStations.Count > 1); // Multiple stations
    
    var results = await visibleRequisitions.ToListAsync();
    Assert.All(results, r => r.DepartmentId == 1); // Information Technology Department only
}
```

**Expected Results:**
- ✅ Can see Information Technology department data at all stations
- ❌ Cannot see other departments at any station

### Test Case TC-AUTH-005: Administrator
**Setup:**
- User: TST00005 (Administrator role group)
- Employee: PayrollNo='TST00005', Department='Information Technology', Station='Head Office'
- Role Group: 'Test_Administrator'
- Permissions: CanAccessAcrossStations=true, CanAccessAcrossDepartments=true

**Test Scenarios:**
```csharp
[Test]
public async Task Administrator_CanAccessAllData()
{
    // Arrange
    var userPayrollNo = "TST00005";
    
    // Act
    var visibleDepartments = await _visibilityService
        .GetVisibleDepartmentsAsync(userPayrollNo);
    var visibleStations = await _visibilityService
        .GetVisibleStationsAsync(userPayrollNo);
    var visibleRequisitions = await _visibilityService
        .ApplyVisibilityScopeAsync(_context.Requisitions, userPayrollNo);
    
    // Assert
    Assert.True(visibleDepartments.Count >= 5); // All test departments
    Assert.True(visibleStations.Count >= 4);    // All test stations
    
    var totalRequisitions = await _context.Requisitions.CountAsync();
    var visibleCount = await visibleRequisitions.CountAsync();
    Assert.Equal(totalRequisitions, visibleCount);
}
```

**Expected Results:**
- ✅ Can see all data from all departments and stations

## 2. Cross-Department Access Tests

### Test Case TC-AUTH-006: Cross-Department Visibility
**Objective:** Verify department boundary enforcement

**Test Data:**
- User A: IT Department, Factory A
- User B: HR Department, Factory A
- Data: Requisitions from both departments

**Test Scenarios:**
```csharp
[Test]
public async Task CrossDepartment_DefaultUser_CannotAccessOtherDepartments()
{
    // Test that IT user cannot see HR requisitions
    var itUser = "TST001";
    var hrRequisitions = await _context.Requisitions
        .Where(r => r.DepartmentId == 2) // HR Department
        .ToListAsync();
    
    foreach (var requisition in hrRequisitions)
    {
        var canAccess = await _visibilityService
            .CanUserAccessEntityAsync(requisition, itUser);
        Assert.False(canAccess);
    }
}

[Test]
public async Task CrossDepartment_StationSupport_CanAccessAllDepartments()
{
    // Test that station support can see all departments at their station
    var stationSupportUser = "TST003";
    
    var itRequisitions = await _context.Requisitions
        .Where(r => r.DepartmentId == 1 && r.IssueStationId == 1)
        .ToListAsync();
    var hrRequisitions = await _context.Requisitions
        .Where(r => r.DepartmentId == 2 && r.IssueStationId == 1)
        .ToListAsync();
    
    // Should be able to access both IT and HR requisitions at same station
    foreach (var requisition in itRequisitions.Concat(hrRequisitions))
    {
        var canAccess = await _visibilityService
            .CanUserAccessEntityAsync(requisition, stationSupportUser);
        Assert.True(canAccess);
    }
}
```

## 3. Cross-Station Access Tests

### Test Case TC-AUTH-007: Cross-Station Visibility
**Objective:** Verify station boundary enforcement

**Test Scenarios:**
```csharp
[Test]
public async Task CrossStation_DefaultUser_CannotAccessOtherStations()
{
    // Test that Factory A user cannot see Factory B data
    var factoryAUser = "TST001";
    var factoryBRequisitions = await _context.Requisitions
        .Where(r => r.IssueStationId == 2) // Factory B
        .ToListAsync();
    
    foreach (var requisition in factoryBRequisitions)
    {
        var canAccess = await _visibilityService
            .CanUserAccessEntityAsync(requisition, factoryAUser);
        Assert.False(canAccess);
    }
}

[Test]
public async Task CrossStation_GroupManager_CanAccessAllStations()
{
    // Test that group manager can see their department at all stations
    var groupManagerUser = "TST004";
    
    var factoryARequisitions = await _context.Requisitions
        .Where(r => r.DepartmentId == 1 && r.IssueStationId == 1)
        .ToListAsync();
    var factoryBRequisitions = await _context.Requisitions
        .Where(r => r.DepartmentId == 1 && r.IssueStationId == 2)
        .ToListAsync();
    
    // Should be able to access IT department at both stations
    foreach (var requisition in factoryARequisitions.Concat(factoryBRequisitions))
    {
        var canAccess = await _visibilityService
            .CanUserAccessEntityAsync(requisition, groupManagerUser);
        Assert.True(canAccess);
    }
}
```

## 4. Edge Cases and Boundary Conditions

### Test Case TC-AUTH-008: Multiple Role Group Memberships
**Objective:** Test OR logic for multiple role groups

**Setup:**
```sql
-- User with multiple role group memberships
INSERT INTO RoleGroupMembers (RoleGroupId, PayrollNo, IsActive) VALUES
(1, 'TST008', 1), -- Department Manager (false, false)
(2, 'TST008', 1); -- Station Support (false, true)
```

**Test:**
```csharp
[Test]
public async Task MultipleRoleGroups_TakesMaximumPermissions()
{
    // User should get station-wide access (OR of permissions)
    var userPayrollNo = "TST008";
    
    var visibleDepartments = await _visibilityService
        .GetVisibleDepartmentsAsync(userPayrollNo);
    
    // Should see all departments due to Station Support role
    Assert.True(visibleDepartments.Count > 1);
}
```

### Test Case TC-AUTH-009: Inactive Role Groups
**Objective:** Test inactive role group handling

**Test:**
```csharp
[Test]
public async Task InactiveRoleGroup_FallsBackToDefaultUser()
{
    // Deactivate role group
    var roleGroup = await _context.RoleGroups
        .FirstAsync(rg => rg.Name == "Test_Department_Manager");
    roleGroup.IsActive = false;
    await _context.SaveChangesAsync();
    
    // User should behave as default user
    var userPayrollNo = "TST002";
    var visibleRequisitions = await _visibilityService
        .ApplyVisibilityScopeAsync(_context.Requisitions, userPayrollNo);
    var results = await visibleRequisitions.ToListAsync();
    
    // Should only see own data
    Assert.All(results, r => Assert.Equal(userPayrollNo, r.PayrollNo));
}
```

### Test Case TC-AUTH-010: Missing Employee Data
**Objective:** Test error handling for invalid users

**Test:**
```csharp
[Test]
public async Task MissingEmployee_ReturnsEmptyResults()
{
    var nonExistentUser = "INVALID";
    
    var visibleRequisitions = await _visibilityService
        .ApplyVisibilityScopeAsync(_context.Requisitions, nonExistentUser);
    var results = await visibleRequisitions.ToListAsync();
    
    Assert.Empty(results);
}
```

## 5. Performance Test Scenarios

### Test Case TC-AUTH-011: Large Dataset Performance
**Objective:** Validate performance with large datasets

**Setup:**
```csharp
// Create large test dataset
private async Task SeedLargeDataset()
{
    var requisitions = new List<Requisition>();
    for (int i = 0; i < 10000; i++)
    {
        requisitions.Add(new Requisition
        {
            PayrollNo = $"TST{i % 100:000}",
            DepartmentId = (i % 5) + 1,
            IssueStationId = (i % 4) + 1,
            DeliveryStationId = (i % 4) + 1,
            Description = $"Test requisition {i}"
        });
    }
    await _context.Requisitions.AddRangeAsync(requisitions);
    await _context.SaveChangesAsync();
}
```

**Test:**
```csharp
[Test]
public async Task LargeDataset_PerformanceTest()
{
    await SeedLargeDataset();
    
    var stopwatch = Stopwatch.StartNew();
    var visibleRequisitions = await _visibilityService
        .ApplyVisibilityScopeAsync(_context.Requisitions, "TST003");
    var results = await visibleRequisitions.ToListAsync();
    stopwatch.Stop();
    
    // Should complete within reasonable time
    Assert.True(stopwatch.ElapsedMilliseconds < 5000); // 5 seconds max
    Assert.True(results.Count > 0);
}
```

### Test Case TC-AUTH-012: Concurrent Access Test
**Objective:** Test concurrent authorization requests

**Test:**
```csharp
[Test]
public async Task ConcurrentAccess_MaintainsConsistency()
{
    var tasks = new List<Task<List<Requisition>>>();
    var userPayrollNos = new[] { "TST001", "TST002", "TST003", "TST004", "TST005" };
    
    // Create concurrent authorization requests
    foreach (var payrollNo in userPayrollNos)
    {
        tasks.Add(Task.Run(async () =>
        {
            var visibleRequisitions = await _visibilityService
                .ApplyVisibilityScopeAsync(_context.Requisitions, payrollNo);
            return await visibleRequisitions.ToListAsync();
        }));
    }
    
    var results = await Task.WhenAll(tasks);
    
    // All tasks should complete successfully
    Assert.All(results, result => Assert.NotNull(result));
}
```

## 6. Security Test Scenarios

### Test Case TC-AUTH-013: SQL Injection Prevention
**Objective:** Ensure authorization service prevents SQL injection

**Test:**
```csharp
[Test]
public async Task SQLInjection_PreventionTest()
{
    var maliciousPayrollNo = "'; DROP TABLE Requisitions; --";
    
    // Should not cause SQL injection
    var visibleRequisitions = await _visibilityService
        .ApplyVisibilityScopeAsync(_context.Requisitions, maliciousPayrollNo);
    var results = await visibleRequisitions.ToListAsync();
    
    // Should return empty results, not cause error
    Assert.Empty(results);
    
    // Verify table still exists
    var tableExists = await _context.Requisitions.AnyAsync();
    Assert.True(await _context.Database.CanConnectAsync());
}
```

### Test Case TC-AUTH-014: Admin Role Bypass Test
**Objective:** Verify admin role bypasses all restrictions

**Test:**
```csharp
[Test]
public async Task AdminRole_BypassesAllRestrictions()
{
    // Create user with Admin role but no role groups
    var adminUser = new EmployeeBkp
    {
        PayrollNo = "ADMIN001",
        Department = "1",
        Station = "001",
        Role = "Admin"
    };
    await _context.EmployeeBkps.AddAsync(adminUser);
    await _context.SaveChangesAsync();
    
    // Should see all data despite no role group membership
    var visibleRequisitions = await _visibilityService
        .ApplyVisibilityScopeAsync(_context.Requisitions, "ADMIN001");
    var totalRequisitions = await _context.Requisitions.CountAsync();
    var visibleCount = await visibleRequisitions.CountAsync();
    
    Assert.Equal(totalRequisitions, visibleCount);
}
```

## Test Automation Framework

### Base Test Class
```csharp
public abstract class AuthorizationTestBase : IDisposable
{
    protected readonly TestDbContext _context;
    protected readonly IVisibilityAuthorizeService _visibilityService;
    
    protected AuthorizationTestBase()
    {
        _context = CreateTestContext();
        _visibilityService = CreateVisibilityService();
        SeedTestData().Wait();
    }
    
    protected abstract Task SeedTestData();
    protected abstract TestDbContext CreateTestContext();
    protected abstract IVisibilityAuthorizeService CreateVisibilityService();
    
    public void Dispose()
    {
        _context?.Dispose();
    }
}
```

### Test Data Cleanup
```csharp
[TearDown]
public async Task Cleanup()
{
    // Clean up test data after each test
    await _context.Database.ExecuteSqlRawAsync("DELETE FROM RoleGroupMembers WHERE PayrollNo LIKE 'TST%'");
    await _context.Database.ExecuteSqlRawAsync("DELETE FROM Requisitions WHERE PayrollNo LIKE 'TST%'");
    await _context.Database.ExecuteSqlRawAsync("DELETE FROM EmployeeBkp WHERE PayrollNo LIKE 'TST%'");
    await _context.Database.ExecuteSqlRawAsync("DELETE FROM RoleGroups WHERE Name LIKE 'Test_%'");
}
```

## Test Coverage Checklist

### ✅ Permission Combinations
- [ ] Default User (no role group)
- [ ] Department Manager (false, false)
- [ ] Station Support (false, true)
- [ ] Group Manager (true, false)
- [ ] Administrator (true, true)

### ✅ Entity Types
- [ ] Requisition filtering
- [ ] Approval filtering
- [ ] Department visibility
- [ ] Station visibility

### ✅ Edge Cases
- [ ] Multiple role group memberships
- [ ] Inactive role groups
- [ ] Missing employee data
- [ ] Null/empty parameters

### ✅ Performance
- [ ] Large dataset handling
- [ ] Concurrent access
- [ ] Query optimization
- [ ] Memory usage

### ✅ Security
- [ ] SQL injection prevention
- [ ] Admin role bypass
- [ ] Unauthorized access attempts
- [ ] Data leak prevention

## Test Execution Guidelines

1. **Run tests in isolation** - Each test should be independent
2. **Use fresh test data** - Reset database state between tests
3. **Test all permission combinations** - Don't skip edge cases
4. **Validate performance** - Monitor execution times
5. **Check security boundaries** - Attempt unauthorized access

## Related Documentation

- [Role Group Authorization System](../Authorization/RoleGroupAuthorizationSystem.md) - System overview
- [VisibilityAuthorizeService Reference](../Services/VisibilityAuthorizeService_Reference.md) - Implementation details
- [Role Group Test Data](RoleGroupTestData.md) - Standard test data setup

---

*This test matrix should be updated whenever new authorization features are added or permission logic changes.*