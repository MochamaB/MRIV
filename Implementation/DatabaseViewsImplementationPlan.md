# Database Views Implementation Plan

## Overview
This document outlines the implementation plan for creating database views to reduce cross-context queries between the Requisition and ktdaleave databases. The views will significantly improve performance by eliminating N+1 query problems and simplifying the codebase.

## Problem Statement
Currently, the application makes multiple cross-context queries to resolve:
- Employee details from PayrollNo
- Station and Department names from IDs
- Location hierarchies for display
- Repeated lookups for the same data

Example problem areas:
- `DashboardService.cs:79-83` - Multiple calls to `GetLocationNameFromIdsAsync` per requisition
- `MaterialRequisitionController.cs:84-91` - Fetching admin employees from ktdaleave
- Throughout the application - Manual joins between PayrollNo and Employee details

## Critical Data Type Mapping Issues

### **Key Differences Between Databases:**

1. **Station References**:
   - **ktdaleave**: `Employee_bkp.Station` is `varchar(50)` containing station **names** like "HQ", "005", "011"
   - **ktdaleave**: `Station.Station_Name` is `varchar(50)` containing names like "KAPSARA", "NDUTI"
   - **Requisition**: Uses `int` Station IDs (1, 2, 3...)
   - **Problem**: No direct mapping table exists between station names and IDs

2. **Department References**:
   - **ktdaleave**: `Employee_bkp.Department` is `varchar(50)` containing codes like "101", "HGD", "HFC"
   - **ktdaleave**: `Department.DepartmentID` is `varchar(50)` with standardized codes like "101", "102"
   - **ktdaleave**: `Department.departmentCode` is `int` (auto-increment primary key)
   - **Requisition**: Uses `string` Department IDs matching ktdaleave patterns

3. **Station Categories**:
   - **Requisition**: Has `StationCategory` table with categories: "headoffice", "region", "factory", "vendor"
   - **ktdaleave**: No station category classification
   - **Need**: Create mapping between ktdaleave stations and requisition station categories

## Solution Architecture
Create read-only database views in the Requisition database that reference ktdaleave data, handling the data type mismatches and providing pre-joined data for common queries.

## Phase 1: Database Views Creation

### 1.1 Create SQL Script File
Create `DatabaseScripts/CreateDatabaseViews.sql`:

```sql
-- Script to create cross-database views in Requisition database
-- These views reduce cross-context queries between Requisition and ktdaleave databases

USE [Requisition]; -- Replace with your actual requisition database name
GO

-- Drop existing views if they exist
IF OBJECT_ID('dbo.vw_EmployeeDetails', 'V') IS NOT NULL
    DROP VIEW dbo.vw_EmployeeDetails;
GO

IF OBJECT_ID('dbo.vw_LocationHierarchy', 'V') IS NOT NULL
    DROP VIEW dbo.vw_LocationHierarchy;
GO

IF OBJECT_ID('dbo.vw_RequisitionDetails', 'V') IS NOT NULL
    DROP VIEW dbo.vw_RequisitionDetails;
GO

-- =============================================
-- 1. Employee Details View
-- Purpose: Provides complete employee information with department and station names
-- Usage: Replace multiple queries to ktdaleave for employee lookups
-- Note: Handles data type mismatches between string-based ktdaleave and ID-based requisition
-- =============================================
CREATE VIEW dbo.vw_EmployeeDetails AS
SELECT
    e.PayrollNo,
    e.Fullname,
    e.SurName,
    e.OtherNames,
    e.Department as DepartmentCode,
    ISNULL(d.DepartmentName, 'Unknown Department') as DepartmentName,
    e.Station as StationName,  -- Keep as string since ktdaleave stores station names
    ISNULL(s.Station_Name, e.Station) as ResolvedStationName,
    e.EmpisCurrActive as IsActive,
    e.Role,
    e.Hod as HeadOfDepartment,
    e.Supervisor,
    e.Designation,
    e.EmailAddress
FROM [ktdaleave].[dbo].[Employee_bkp] e
LEFT JOIN [ktdaleave].[dbo].[Department] d
    ON e.Department = d.DepartmentID  -- String comparison for department codes
LEFT JOIN [ktdaleave].[dbo].[Station] s
    ON e.Station = s.Station_Name     -- Direct name matching for stations
    OR (ISNUMERIC(e.Station) = 1 AND CAST(e.Station AS INT) = s.StationID); -- Handle numeric station codes
GO

-- =============================================
-- 2. Station Details View
-- Purpose: Enhanced station information with category mapping
-- Usage: Bridge between requisition station categories and ktdaleave stations
-- Note: Maps ktdaleave stations to requisition station categories
-- =============================================
CREATE VIEW dbo.vw_StationDetails AS
SELECT
    s.StationID as KtdaleaveStationId,
    s.Station_Name as StationName,
    -- Map stations to categories based on business rules
    CASE
        WHEN s.Station_Name IN ('HQ', 'HEAD OFFICE') THEN 'headoffice'
        WHEN s.Station_Name LIKE '%FACTORY%' OR s.Station_Name LIKE '%PROCESSING%' THEN 'factory'
        WHEN s.Station_Name IN ('VENDOR', 'SUPPLIER') THEN 'vendor'
        ELSE 'region'  -- Default to region for tea collection centers
    END as StationCategoryCode,
    CASE
        WHEN s.Station_Name IN ('HQ', 'HEAD OFFICE') THEN 'Head Office'
        WHEN s.Station_Name LIKE '%FACTORY%' OR s.Station_Name LIKE '%PROCESSING%' THEN 'Factory'
        WHEN s.Station_Name IN ('VENDOR', 'SUPPLIER') THEN 'Vendor'
        ELSE 'Region'
    END as StationCategoryName
FROM [ktdaleave].[dbo].[Station] s;
GO

-- =============================================
-- 3. Location Hierarchy View
-- Purpose: Provides station-department combinations with resolved names
-- Usage: Quick lookup of location names handling requisition ID to ktdaleave mapping
-- Note: Handles the mismatch between requisition int IDs and ktdaleave string references
-- =============================================
CREATE VIEW dbo.vw_LocationHierarchy AS
SELECT
    sd.KtdaleaveStationId,
    sd.StationName,
    d.departmentCode as KtdaleaveDepartmentId,
    d.DepartmentID as DepartmentCode,
    d.DepartmentName,
    CONCAT(sd.StationName, ' - ', d.DepartmentName) as FullLocationName,
    sd.StationCategoryCode,
    sd.StationCategoryName
FROM dbo.vw_StationDetails sd
CROSS JOIN [ktdaleave].[dbo].[Department] d;
GO

-- =============================================
-- 4. Enhanced Requisition Details View
-- Purpose: Provides requisitions with all related data pre-joined
-- Usage: Single query for dashboard and listing pages
-- Note: Handles complex mapping between requisition int IDs and ktdaleave string references
-- =============================================
CREATE VIEW dbo.vw_RequisitionDetails AS
SELECT
    r.Id,
    r.PayrollNo,
    r.DepartmentId,
    r.TicketId,
    r.RequisitionType,
    r.IssueStationId,
    r.IssueDepartmentId,
    r.DeliveryStationId,
    r.DeliveryDepartmentId,
    r.Status,
    r.CreatedAt,
    r.UpdatedAt,
    r.Remarks as Notes,
    r.CollectorName,
    r.CollectorId,
    -- Issue location details (complex mapping required)
    CASE
        WHEN r.IssueStationId IS NOT NULL THEN
            (SELECT TOP 1 lh.StationName
             FROM vw_LocationHierarchy lh
             WHERE lh.KtdaleaveStationId = r.IssueStationId)
        ELSE 'Unknown Station'
    END as IssueStationName,
    CASE
        WHEN r.IssueDepartmentId IS NOT NULL THEN
            (SELECT TOP 1 lh.DepartmentName
             FROM vw_LocationHierarchy lh
             WHERE lh.DepartmentCode = r.IssueDepartmentId)
        ELSE 'Unknown Department'
    END as IssueDepartmentName,
    -- Delivery location details
    CASE
        WHEN r.DeliveryStationId IS NOT NULL THEN
            (SELECT TOP 1 lh.StationName
             FROM vw_LocationHierarchy lh
             WHERE lh.KtdaleaveStationId = r.DeliveryStationId)
        ELSE 'Unknown Station'
    END as DeliveryStationName,
    CASE
        WHEN r.DeliveryDepartmentId IS NOT NULL THEN
            (SELECT TOP 1 lh.DepartmentName
             FROM vw_LocationHierarchy lh
             WHERE lh.DepartmentCode = r.DeliveryDepartmentId)
        ELSE 'Unknown Department'
    END as DeliveryDepartmentName,
    -- Employee details
    ISNULL(emp.Fullname, r.PayrollNo) as EmployeeName,
    emp.DepartmentName as EmployeeDepartmentName,
    emp.ResolvedStationName as EmployeeStationName,
    emp.Role as EmployeeRole,
    -- Computed columns
    (SELECT COUNT(*) FROM RequisitionItems ri WHERE ri.RequisitionId = r.Id) as ItemCount,
    (SELECT SUM(ri.Quantity) FROM RequisitionItems ri WHERE ri.RequisitionId = r.Id) as TotalQuantity
FROM Requisitions r
LEFT JOIN vw_EmployeeDetails emp
    ON r.PayrollNo = emp.PayrollNo;
GO

-- Grant permissions (update with your actual application user)
-- GRANT SELECT ON dbo.vw_EmployeeDetails TO [your_app_user];
-- GRANT SELECT ON dbo.vw_LocationHierarchy TO [your_app_user];
-- GRANT SELECT ON dbo.vw_RequisitionDetails TO [your_app_user];
-- GO
```

### 1.2 Performance Optimization Indexes
Create `DatabaseScripts/CreateViewIndexes.sql`:

```sql
-- Add indexes on frequently joined columns if not already present
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Requisitions_PayrollNo')
    CREATE INDEX IX_Requisitions_PayrollNo ON Requisitions(PayrollNo);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Requisitions_IssueLocation')
    CREATE INDEX IX_Requisitions_IssueLocation ON Requisitions(IssueStationId, IssueDepartmentId);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Requisitions_DeliveryLocation')
    CREATE INDEX IX_Requisitions_DeliveryLocation ON Requisitions(DeliveryStationId, DeliveryDepartmentId);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Requisitions_Status_CreatedAt')
    CREATE INDEX IX_Requisitions_Status_CreatedAt ON Requisitions(Status, CreatedAt DESC);
```

## Phase 2: Create Entity Models

### 2.1 Create View Models
Create the following files in `Models/Views/` folder:

#### EmployeeDetailsView.cs
```csharp
using System.ComponentModel.DataAnnotations;

namespace MRIV.Models.Views
{
    public class EmployeeDetailsView
    {
        [Key]
        public string PayrollNo { get; set; }
        public string Fullname { get; set; }
        public string SurName { get; set; }
        public string OtherNames { get; set; }
        public string DepartmentCode { get; set; }  // String from ktdaleave
        public string DepartmentName { get; set; }
        public string StationName { get; set; }     // Original station name/code from ktdaleave
        public string ResolvedStationName { get; set; }  // Resolved station name
        public int IsActive { get; set; }
        public string Role { get; set; }
        public string HeadOfDepartment { get; set; }
        public string Supervisor { get; set; }
        public string Designation { get; set; }
        public string EmailAddress { get; set; }
    }
}
```

#### StationDetailsView.cs
```csharp
namespace MRIV.Models.Views
{
    public class StationDetailsView
    {
        public int KtdaleaveStationId { get; set; }
        public string StationName { get; set; }
        public string StationCategoryCode { get; set; }
        public string StationCategoryName { get; set; }
    }
}
```

#### LocationHierarchyView.cs
```csharp
namespace MRIV.Models.Views
{
    public class LocationHierarchyView
    {
        public int KtdaleaveStationId { get; set; }
        public string StationName { get; set; }
        public int KtdaleaveDepartmentId { get; set; }
        public string DepartmentCode { get; set; }  // String ID used in requisition
        public string DepartmentName { get; set; }
        public string FullLocationName { get; set; }
        public string StationCategoryCode { get; set; }
        public string StationCategoryName { get; set; }
    }
}
```

#### RequisitionDetailsView.cs
```csharp
using System;
using MRIV.Enums;

namespace MRIV.Models.Views
{
    public class RequisitionDetailsView
    {
        public int Id { get; set; }
        public string PayrollNo { get; set; }
        public int DepartmentId { get; set; }
        public int TicketId { get; set; }
        public RequisitionType RequisitionType { get; set; }
        public int IssueStationId { get; set; }
        public string IssueDepartmentId { get; set; }  // String from requisition
        public int DeliveryStationId { get; set; }
        public string DeliveryDepartmentId { get; set; }  // String from requisition
        public RequisitionStatus? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string Notes { get; set; }
        public string CollectorName { get; set; }
        public string CollectorId { get; set; }

        // View-specific resolved columns
        public string IssueStationName { get; set; }
        public string IssueDepartmentName { get; set; }
        public string DeliveryStationName { get; set; }
        public string DeliveryDepartmentName { get; set; }
        public string EmployeeName { get; set; }
        public string EmployeeDepartmentName { get; set; }
        public string EmployeeStationName { get; set; }
        public string EmployeeRole { get; set; }
        public int ItemCount { get; set; }
        public int? TotalQuantity { get; set; }
    }
}
```

## Phase 3: Configure DbContext

### 3.1 Update RequisitionContext.cs

Add the following to your `RequisitionContext.cs`:

```csharp
using MRIV.Models.Views;

public partial class RequisitionContext : DbContext
{
    // ... existing DbSets ...

    // Add view DbSets
    public DbSet<EmployeeDetailsView> EmployeeDetailsViews { get; set; }
    public DbSet<StationDetailsView> StationDetailsViews { get; set; }
    public DbSet<LocationHierarchyView> LocationHierarchyViews { get; set; }
    public DbSet<RequisitionDetailsView> RequisitionDetailsViews { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ... existing configuration ...

        // Configure views as read-only entities
        modelBuilder.Entity<EmployeeDetailsView>(entity =>
        {
            entity.HasKey(e => e.PayrollNo);
            entity.ToView("vw_EmployeeDetails");
        });

        modelBuilder.Entity<StationDetailsView>(entity =>
        {
            entity.HasKey(e => e.KtdaleaveStationId);
            entity.ToView("vw_StationDetails");
        });

        modelBuilder.Entity<LocationHierarchyView>(entity =>
        {
            entity.HasNoKey(); // Composite key, no single unique identifier
            entity.ToView("vw_LocationHierarchy");
        });

        modelBuilder.Entity<RequisitionDetailsView>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToView("vw_RequisitionDetails");
        });
    }
}
```

## Phase 4: Update Services

### 4.1 Update DashboardService.cs

#### Current Implementation (Lines 76-99)
```csharp
// Multiple queries for each requisition
foreach (var r in recentRequisitions)
{
    string issueLocationName = await _departmentService.GetLocationNameFromIdsAsync(
        r.IssueStationId, r.IssueDepartmentId);

    string deliveryLocationName = await _departmentService.GetLocationNameFromIdsAsync(
        r.DeliveryStationId, r.DeliveryDepartmentId);

    viewModel.RecentRequisitions.Add(new RequisitionSummary
    {
        Id = r.Id,
        IssueStation = issueLocationName,
        DeliveryStation = deliveryLocationName,
        // ... rest of mapping
    });
}
```

#### New Implementation Using Views
```csharp
// Single query using the view
var recentRequisitions = await _context.RequisitionDetailsViews
    .Where(r => r.PayrollNo == payrollNo)
    .OrderByDescending(r => r.CreatedAt)
    .Take(5)
    .Select(r => new RequisitionSummary
    {
        Id = r.Id,
        IssueStation = r.IssueLocationName,
        DeliveryStation = r.DeliveryLocationName,
        IssueStationId = r.IssueStationId,
        IssueDepartmentId = r.IssueDepartmentId,
        DeliveryStationId = r.DeliveryStationId,
        DeliveryDepartmentId = r.DeliveryDepartmentId,
        Status = r.Status ?? RequisitionStatus.NotStarted,
        StatusDescription = r.Status?.GetDescription() ?? "Not Started",
        CreatedAt = r.CreatedAt,
        ItemCount = r.ItemCount,
        EmployeeName = r.EmployeeName
    })
    .ToListAsync();

viewModel.RecentRequisitions = recentRequisitions;
```

### 4.2 Update MaterialRequisitionController.cs

#### Current Implementation (Lines 84-91)
```csharp
using var ktdaContext = new KtdaleaveContext();
model.EmployeeBkps = await ktdaContext.EmployeeBkps
    .Where(e => e.Department == "106" && e.EmpisCurrActive == 0)
    .OrderBy(e => e.Fullname)
    .ToListAsync();
```

#### New Implementation Using Views
```csharp
// Use the view instead - no need for separate context
var adminEmployees = await _context.EmployeeDetailsViews
    .Where(e => e.DepartmentCode == "106" && e.IsActive == 0)
    .OrderBy(e => e.Fullname)
    .Select(e => new EmployeeBkp
    {
        PayrollNo = e.PayrollNo,
        Fullname = e.Fullname,
        SurName = e.SurName,
        OtherNames = e.OtherNames,
        Department = e.DepartmentCode,
        Station = e.StationName,  // Use station name from ktdaleave
        EmailAddress = e.EmailAddress,
        Role = e.Role,
        Designation = e.Designation
    })
    .ToListAsync();

model.EmployeeBkps = adminEmployees;
```

## Phase 5: Migration Strategy

### 5.1 Phased Rollout Plan

#### Phase A: Database Setup (No Code Changes)
**Timeline: Day 1**
- Run `CreateDatabaseViews.sql` script
- Run `CreateViewIndexes.sql` script
- Test views directly in SQL Management Studio
- Verify data accuracy
- **Risk: None** - No application changes

#### Phase B: Add View Infrastructure
**Timeline: Day 2-3**
- Add view entity models to project
- Update RequisitionContext
- Deploy application
- Verify existing functionality still works
- **Risk: Low** - Views not yet in use

#### Phase C: Gradual Service Migration
**Timeline: Week 1-2**
- Update DashboardService first (read-only, low risk)
- Monitor performance metrics
- Update MaterialRequisitionController methods one at a time
- Test each change thoroughly
- **Risk: Medium** - Can rollback individual methods

#### Phase D: Complete Migration
**Timeline: Week 3**
- Update remaining services
- Remove obsolete cross-context methods
- Update documentation
- **Risk: Low** - Most issues already identified and fixed

### 5.2 Rollback Plan

Each phase can be rolled back independently:
- **Phase A**: Drop views (no application impact)
- **Phase B**: Deploy previous version without view models
- **Phase C**: Revert individual service methods
- **Phase D**: Keep old methods as deprecated fallbacks

## Phase 6: Testing Strategy

### 6.1 Unit Tests
```csharp
[TestClass]
public class ViewTests
{
    [TestMethod]
    public async Task EmployeeDetailsView_ReturnsCorrectData()
    {
        // Test that view returns expected employee data
    }

    [TestMethod]
    public async Task RequisitionDetailsView_JoinsCorrectly()
    {
        // Test that all joins work correctly
    }
}
```

### 6.2 Performance Tests
- Measure query execution time before and after
- Expected improvement: 10+ queries to 1 query
- Target: < 100ms for dashboard load

### 6.3 Integration Tests
- Test all affected endpoints
- Verify data consistency
- Check for null reference exceptions

## Expected Benefits

### Performance Improvements
- **Current**: 10-15 queries per dashboard load
- **After**: 1-2 queries per dashboard load
- **Expected Speed Increase**: 5-10x faster page loads

### Code Quality Improvements
- **Lines of Code Reduced**: ~30% in affected services
- **Complexity Reduction**: Eliminated nested loops and multiple async calls
- **Maintainability**: Centralized location resolution logic

### Resource Usage
- **Database Connections**: Reduced by 80%
- **Memory Usage**: Lower due to fewer object allocations
- **Network Traffic**: Significantly reduced cross-database traffic

## Monitoring and Success Metrics

### Key Performance Indicators
1. **Query Count Reduction**: Target 80% reduction
2. **Page Load Time**: Target < 500ms for dashboard
3. **Database CPU Usage**: Target 50% reduction
4. **Error Rate**: Must remain at 0%

### Monitoring Tools
- Application Insights for performance metrics
- SQL Server Profiler for query analysis
- Custom logging for view usage statistics

## Potential Issues and Mitigations

### Issue 1: Cross-Database Permissions
**Risk**: Application user may not have permissions on ktdaleave database
**Mitigation**: Work with DBA to grant SELECT permissions on required tables

### Issue 2: View Performance
**Risk**: Views might be slow with large datasets
**Mitigation**: Create indexed views if needed, add appropriate indexes

### Issue 3: Data Inconsistency
**Risk**: Views might show outdated data if caching is involved
**Mitigation**: Views are real-time by default, no caching unless explicitly added

### Issue 4: Deployment Complexity
**Risk**: Need to coordinate database and application deployments
**Mitigation**: Use phased approach, database changes first

## Conclusion

This implementation plan provides a systematic approach to reducing cross-context queries through database views. The phased approach minimizes risk while delivering immediate performance benefits. The solution is maintainable, scalable, and has zero impact on the existing ktdaleave database and its consumers.

## Appendix: Quick Reference

### Files to Create
1. `DatabaseScripts/CreateDatabaseViews.sql`
2. `DatabaseScripts/CreateViewIndexes.sql`
3. `Models/Views/EmployeeDetailsView.cs`
4. `Models/Views/StationDetailsView.cs`
5. `Models/Views/LocationHierarchyView.cs`
6. `Models/Views/RequisitionDetailsView.cs`

### Files to Modify
1. `Models/RequisitionContext.cs` - Add view DbSets and configuration
2. `Services/DashboardService.cs` - Update to use views
3. `Controllers/MaterialRequisitionController.cs` - Update to use views

### SQL Objects Created
1. `vw_EmployeeDetails` - Employee information with resolved department/station names
2. `vw_StationDetails` - Station information with category mapping
3. `vw_LocationHierarchy` - All station-department combinations with proper ID mapping
4. `vw_RequisitionDetails` - Complete requisition information with resolved location names

### Data Type Mapping Handled
1. **Station References**: ktdaleave string names → requisition int IDs
2. **Department References**: ktdaleave string codes → requisition string IDs
3. **Station Categories**: ktdaleave stations → requisition categories (headoffice, region, factory, vendor)
4. **Employee Hierarchy**: PayrollNo references to supervisor/HOD chains

### Performance Targets
- Query reduction: 10+ queries → 1 query
- Page load time: < 500ms
- Database CPU: 50% reduction
- Zero increase in error rate