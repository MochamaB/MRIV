# Authorization Test Matrix - Master Test Scenarios

## Overview

This document provides comprehensive test scenarios for all authorization combinations in the MRIV system. Use this as a reference for creating test cases, validating system behavior, and ensuring complete test coverage for role group permissions.

## Test Environment Setup

### Prerequisites

1. **Test Database**: Isolated test database with clean data
2. **Test Users**: Standardized test accounts for each role type
3. **Test Data**: Sample organizational structure and role groups
4. **Test Tools**: Unit test framework and integration test setup

### Standard Test Data Structure

#### Test Stations
```sql
-- Standard test stations
INSERT INTO Station (StationId, Station_Name) VALUES
(0, 'Head Office'),
(001, 'Factory A'),
(002, 'Factory B'),
(003, 'Region Central');
```

#### Test Departments
```sql
-- Standard test departments
INSERT INTO Department (DepartmentCode, DepartmentID, DepartmentName) VALUES
(1, 'IT', 'Information Technology'),
(2, 'HR', 'Human Resources'),
(3, 'FIN', 'Finance'),
(4, 'OPS', 'Operations'),
(5, 'MNT', 'Maintenance');
```

#### Test Employees
```sql
-- Test users for each authorization scenario
INSERT INTO EmployeeBkp (PayrollNo, Surname, Othernames, Department, Station, Role) VALUES
('TST001', 'Default', 'User', '1', '001', 'User'),           -- Default User
('TST002', 'Dept', 'Manager', '1', '001', 'Manager'),        -- Department Manager
('TST003', 'Station', 'Support', '1', '001', 'Support'),     -- Station Support
('TST004', 'Group', 'Manager', '1', '001', 'Manager'),       -- Group Manager
('TST005', 'System', 'Admin', '1', '001', 'Admin'),          -- Administrator
('TST006', 'Cross', 'Dept', '2', '001', 'User'),             -- Different Department
('TST007', 'Cross', 'Station', '1', '002', 'User');          -- Different Station
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

-- Role group memberships
INSERT INTO RoleGroupMembers (RoleGroupId, PayrollNo, IsActive) VALUES
(1, 'TST002', 1), -- Department Manager
(2, 'TST003', 1), -- Station Support
(3, 'TST004', 1), -- Group Manager
(4, 'TST005', 1); -- Administrator
-- TST001 has no role group (Default User)
```

### Test Sample Data
```sql
-- Sample requisitions for testing visibility
INSERT INTO Requisitions (PayrollNo, DepartmentId, IssueStationId, DeliveryStationId, Status, Description) VALUES
('TST001', 1, 1, 1, 'NotStarted', 'Default user requisition'),
('TST006', 2, 1, 1, 'NotStarted', 'Different department requisition'),
('TST007', 1, 2, 2, 'NotStarted', 'Different station requisition'),
('TST002', 1, 1, 1, 'NotStarted', 'Department manager requisition');

-- Sample approvals for testing
INSERT INTO Approvals (PayrollNo, DepartmentId, StationId, Status, RequisitionId) VALUES
('TST001', 1, 1, 'Pending', 1),
('TST006', 2, 1, 'Pending', 2),
('TST007', 1, 2, 'Pending', 3);
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
- User: TST001 (no role group assignment)
- Department: IT (1), Station: Factory A (001)

**Test Scenarios:**
```csharp
[Test]
public async Task DefaultUser_CanOnlyAccessOwnData()
{
    // Arrange
    var userPayrollNo = "TST001";
    
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
- User: TST002 (Department Manager role group)
- Permissions: CanAccessAcrossStations=false, CanAccessAcrossDepartments=false

**Test Scenarios:**
```csharp
[Test]
public async Task DepartmentManager_CanAccessDepartmentAtStation()
{
    // Arrange
    var userPayrollNo = "TST002";
    
    // Act
    var visibleRequisitions = await _visibilityService
        .ApplyVisibilityScopeAsync(_context.Requisitions, userPayrollNo);
    var results = await visibleRequisitions.ToListAsync();
    
    // Assert
    Assert.All(results, r => r.DepartmentId == 1); // IT Department only
    Assert.All(results, r => r.IssueStationId == 1 || r.DeliveryStationId == 1); // Factory A only
}
```

**Expected Results:**
- ✅ Can see all IT department data at Factory A
- ❌ Cannot see HR department data at Factory A
- ❌ Cannot see IT department data at other stations

### Test Case TC-AUTH-003: Station Support
**Setup:**
- User: TST003 (Station Support role group)
- Permissions: CanAccessAcrossStations=false, CanAccessAcrossDepartments=true

**Test Scenarios:**
```csharp
[Test]
public async Task StationSupport_CanAccessAllDepartmentsAtStation()
{
    // Arrange
    var userPayrollNo = "TST003";
    
    // Act
    var visibleDepartments = await _visibilityService
        .GetVisibleDepartmentsAsync(userPayrollNo);
    var visibleRequisitions = await _visibilityService
        .ApplyVisibilityScopeAsync(_context.Requisitions, userPayrollNo);
    
    // Assert
    Assert.Contains(visibleDepartments, d => d.DepartmentCode == 1); // IT
    Assert.Contains(visibleDepartments, d => d.DepartmentCode == 2); // HR
    
    var results = await visibleRequisitions.ToListAsync();
    Assert.All(results, r => r.IssueStationId == 1 || r.DeliveryStationId == 1); // Factory A only
}
```

**Expected Results:**
- ✅ Can see all departments at Factory A
- ❌ Cannot see any departments at other stations

### Test Case TC-AUTH-004: Group Manager
**Setup:**
- User: TST004 (Group Manager role group)
- Permissions: CanAccessAcrossStations=true, CanAccessAcrossDepartments=false

**Test Scenarios:**
```csharp
[Test]
public async Task GroupManager_CanAccessDepartmentAtAllStations()
{
    // Arrange
    var userPayrollNo = "TST004";
    
    // Act
    var visibleStations = await _visibilityService
        .GetVisibleStationsAsync(userPayrollNo);
    var visibleRequisitions = await _visibilityService
        .ApplyVisibilityScopeAsync(_context.Requisitions, userPayrollNo);
    
    // Assert
    Assert.True(visibleStations.Count > 1); // Multiple stations
    
    var results = await visibleRequisitions.ToListAsync();
    Assert.All(results, r => r.DepartmentId == 1); // IT Department only
}
```

**Expected Results:**
- ✅ Can see IT department data at all stations
- ❌ Cannot see other departments at any station

### Test Case TC-AUTH-005: Administrator
**Setup:**
- User: TST005 (Administrator role group)
- Permissions: CanAccessAcrossStations=true, CanAccessAcrossDepartments=true

**Test Scenarios:**
```csharp
[Test]
public async Task Administrator_CanAccessAllData()
{
    // Arrange
    var userPayrollNo = "TST005";
    
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