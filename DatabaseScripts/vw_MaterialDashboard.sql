-- =============================================
-- Material Dashboard View
-- Comprehensive view combining material data with assignments, movements, and analytics
-- for the Material-Focused Dashboard
-- =============================================

USE [requisition]; -- Replace with your actual Requisition database name
GO

-- Drop existing view if it exists
IF OBJECT_ID('dbo.vw_MaterialDashboard', 'V') IS NOT NULL
    DROP VIEW dbo.vw_MaterialDashboard;
GO

CREATE VIEW dbo.vw_MaterialDashboard AS
WITH MaterialAssignmentStatus AS (
    -- Get current assignment status for each material
    SELECT
        ma.MaterialId,
        ma.PayrollNo,
        ma.AssignmentDate,
        ma.IsActive,
        ma.StationId,
        ma.DepartmentId,
        ma.AssignmentType,
        ROW_NUMBER() OVER (PARTITION BY ma.MaterialId ORDER BY ma.AssignmentDate DESC) as rn
    FROM MaterialAssignments ma
),
CurrentAssignments AS (
    -- Get only the most recent assignment for each material
    SELECT
        MaterialId,
        PayrollNo,
        AssignmentDate,
        IsActive,
        StationId,
        DepartmentId,
        AssignmentType
    FROM MaterialAssignmentStatus
    WHERE rn = 1
),
MaterialMovements AS (
    -- Count material movements in last 90 days
    SELECT
        ma.MaterialId,
        COUNT(*) as MovementCount,
        MAX(ma.AssignmentDate) as LastMovementDate
    FROM MaterialAssignments ma
    WHERE ma.AssignmentDate >= DATEADD(DAY, -90, GETDATE())
    GROUP BY ma.MaterialId
),
LatestMaterialCondition AS (
    -- Get latest condition check for each material
    SELECT
        mc.MaterialId,
        mc.Condition,
        mc.FunctionalStatus,
        mc.CosmeticStatus,
        mc.InspectionDate,
        mc.InspectedBy,
        ROW_NUMBER() OVER (PARTITION BY mc.MaterialId ORDER BY mc.InspectionDate DESC) as rn
    FROM MaterialConditions mc
    WHERE mc.MaterialId IS NOT NULL
),
MaterialUtilization AS (
    -- Calculate utilization metrics
    SELECT
        ca.MaterialId,
        CASE
            WHEN ca.IsActive = 1 THEN 'Assigned'
            WHEN ca.PayrollNo IS NOT NULL AND ca.IsActive = 0 THEN 'Recently_Returned'
            ELSE 'Available'
        END as CurrentStatus,
        CASE
            WHEN ca.AssignmentDate IS NOT NULL THEN DATEDIFF(DAY, ca.AssignmentDate, GETDATE())
            ELSE NULL
        END as DaysInCurrentStatus,
        COALESCE(mm.MovementCount, 0) as RecentMovements
    FROM CurrentAssignments ca
    LEFT JOIN MaterialMovements mm ON ca.MaterialId = mm.MaterialId
)
SELECT
    -- Material Basic Info
    m.Id as MaterialId,
    m.Code as MaterialCode,
    m.Name as MaterialName,
    m.Description,

    -- Category Information
    mc.Id as CategoryId,
    mc.Name as CategoryName,
    msc.Id as SubcategoryId,
    msc.Name as SubcategoryName,

    -- Financial Data
    COALESCE(m.PurchasePrice, 0) as PurchasePrice,
    m.PurchaseDate,

    -- Status (using actual MaterialStatus enum)
    m.Status as MaterialStatus,

    -- Condition Information (from latest condition check)
    lmc.Condition as LatestCondition,
    lmc.FunctionalStatus as LatestFunctionalStatus,
    lmc.CosmeticStatus as LatestCosmeticStatus,
    lmc.InspectionDate as LastInspectionDate,
    lmc.InspectedBy as LastInspectedBy,

    -- Location Data (improved logic for better data coverage)
    COALESCE(ca.StationId, ed.StationId) as CurrentStationId,
    COALESCE(lh.StationName, sd.StationName, ed.StationName) as CurrentStationName,
    COALESCE(ca.DepartmentId, ed.DepartmentId) as CurrentDepartmentId,
    COALESCE(lh.DepartmentName, dept.DepartmentName, ed.DepartmentName) as CurrentDepartmentName,

    -- Station Category Data (fallback to StationDetails if LocationHierarchy doesn't match)
    COALESCE(lh.StationCategoryCode, sd.StationCategoryCode) as StationCategoryCode,
    COALESCE(lh.StationCategoryName, sd.StationCategoryName) as StationCategoryName,

    -- Full Location Name (construct if LocationHierarchy doesn't provide it)
    COALESCE(
        lh.FullLocationName,
        CASE
            WHEN COALESCE(lh.StationName, sd.StationName, ed.StationName) IS NOT NULL
                 AND COALESCE(lh.DepartmentName, dept.DepartmentName, ed.DepartmentName) IS NOT NULL
            THEN CONCAT(
                COALESCE(lh.StationName, sd.StationName, ed.StationName),
                ' - ',
                COALESCE(lh.DepartmentName, dept.DepartmentName, ed.DepartmentName)
            )
            ELSE NULL
        END
    ) as FullLocationName,

    -- Assignment Information
    ca.PayrollNo as AssignedToPayrollNo,
    ed.Fullname as AssignedToEmployeeName,
    ca.AssignmentDate,
    ca.IsActive as IsCurrentlyAssigned,
    ca.AssignmentType,

    -- Utilization Metrics
    mu.CurrentStatus,
    mu.DaysInCurrentStatus,
    mu.RecentMovements,

    -- Raw data for business logic calculation in application layer
    -- No hardcoded business rules - let C# handle the logic

    -- Warranty Information (raw dates only)
    m.WarrantyStartDate,
    m.WarrantyEndDate,

    -- Maintenance Information (raw dates only)
    m.LastMaintenanceDate,
    m.NextMaintenanceDate,

    -- Raw data only - business logic moved to application layer
    -- Age calculation (raw days only)
    CASE
        WHEN m.PurchaseDate IS NOT NULL THEN DATEDIFF(DAY, m.PurchaseDate, GETDATE())
        ELSE NULL
    END as AgeInDays,

    -- Timestamps
    m.CreatedAt as MaterialCreatedAt,
    m.UpdatedAt as MaterialUpdatedAt,
    GETDATE() as ViewGeneratedAt

FROM Materials m
    LEFT JOIN MaterialCategories mc ON m.MaterialCategoryId = mc.Id
    LEFT JOIN MaterialSubcategories msc ON m.MaterialSubcategoryId = msc.Id
    LEFT JOIN CurrentAssignments ca ON m.Id = ca.MaterialId
    LEFT JOIN dbo.vw_EmployeeDetails ed ON ca.PayrollNo = ed.PayrollNo AND ed.IsActive = 0
    LEFT JOIN MaterialUtilization mu ON m.Id = mu.MaterialId
    LEFT JOIN LatestMaterialCondition lmc ON m.Id = lmc.MaterialId AND lmc.rn = 1
    -- Use LocationHierarchyView for full location data when both station and department match
    LEFT JOIN dbo.vw_LocationHierarchy lh ON ca.StationId = lh.StationId AND ca.DepartmentId = lh.DepartmentId
    -- Use StationDetails as fallback for station category info when LocationHierarchy doesn't match
    LEFT JOIN dbo.vw_StationDetails sd ON COALESCE(ca.StationId, ed.StationId) = sd.StationId
    -- Use Department table as fallback for department names when LocationHierarchy doesn't match
    LEFT JOIN [ktdaleave].[dbo].[Department] dept ON COALESCE(ca.DepartmentId, ed.DepartmentId) = dept.departmentCode;

GO

-- =============================================
-- Material Utilization Summary View
-- Aggregated metrics for dashboard KPIs
-- =============================================
USE [requisition]; -- Replace with your actual Requisition database name
GO
-- Drop existing view if it exists
IF OBJECT_ID('dbo.vw_MaterialUtilizationSummary', 'V') IS NOT NULL
    DROP VIEW dbo.vw_MaterialUtilizationSummary;
GO

CREATE VIEW dbo.vw_MaterialUtilizationSummary AS
SELECT
    -- Overall Metrics
    COUNT(*) as TotalMaterials,
    SUM(COALESCE(PurchasePrice, 0)) as TotalValue,
    COUNT(CASE WHEN CurrentStatus = 'Available' THEN 1 END) as AvailableMaterials,
    COUNT(CASE WHEN CurrentStatus = 'Assigned' THEN 1 END) as AssignedMaterials,

    -- Utilization Rate (based on assignment status)
    CASE
        WHEN COUNT(*) > 0 THEN
            ROUND(CAST(COUNT(CASE WHEN CurrentStatus = 'Assigned' THEN 1 END) AS FLOAT) / COUNT(*) * 100, 2)
        ELSE 0
    END as UtilizationRate,

    -- Raw counts for alert calculation in application layer
    COUNT(CASE WHEN WarrantyEndDate IS NOT NULL AND WarrantyEndDate <= DATEADD(DAY, 30, GETDATE()) AND WarrantyEndDate > GETDATE() THEN 1 END) as WarrantyExpiringSoonCount,
    COUNT(CASE WHEN WarrantyEndDate IS NOT NULL AND WarrantyEndDate < GETDATE() THEN 1 END) as WarrantyExpiredCount,
    COUNT(CASE WHEN NextMaintenanceDate IS NOT NULL AND NextMaintenanceDate < GETDATE() THEN 1 END) as MaintenanceOverdueCount,
    COUNT(CASE WHEN NextMaintenanceDate IS NOT NULL AND NextMaintenanceDate <= DATEADD(DAY, 7, GETDATE()) AND NextMaintenanceDate >= GETDATE() THEN 1 END) as MaintenanceDueSoonCount,

    -- Value aggregations (raw values for application layer categorization)
    SUM(COALESCE(PurchasePrice, 0)) as TotalPurchaseValue,
    AVG(COALESCE(PurchasePrice, 0)) as AveragePurchaseValue,
    MAX(COALESCE(PurchasePrice, 0)) as MaxPurchaseValue,
    COUNT(CASE WHEN COALESCE(PurchasePrice, 0) >= 100000 THEN 1 END) as HighValueCount,

    -- Assignment Status Distribution (from CurrentStatus calculation)
    COUNT(CASE WHEN CurrentStatus = 'Assigned' THEN 1 END) as AssignmentStatusAssigned,
    COUNT(CASE WHEN CurrentStatus = 'Available' THEN 1 END) as AssignmentStatusAvailable,
    COUNT(CASE WHEN CurrentStatus = 'Recently_Returned' THEN 1 END) as AssignmentStatusRecentlyReturned,

    -- Raw MaterialStatus enum counts (let application layer interpret)
    COUNT(CASE WHEN MaterialStatus = 1 THEN 1 END) as MaterialStatus1Count,
    COUNT(CASE WHEN MaterialStatus = 2 THEN 1 END) as MaterialStatus2Count,
    COUNT(CASE WHEN MaterialStatus = 3 THEN 1 END) as MaterialStatus3Count,
    COUNT(CASE WHEN MaterialStatus = 4 THEN 1 END) as MaterialStatus4Count,
    COUNT(CASE WHEN MaterialStatus = 5 THEN 1 END) as MaterialStatus5Count,
    COUNT(CASE WHEN MaterialStatus = 6 THEN 1 END) as MaterialStatus6Count,

    -- Location Grouping (including station category from LocationHierarchy)
    CurrentStationId,
    CurrentStationName,
    CurrentDepartmentId,
    CurrentDepartmentName,
    StationCategoryCode,
    StationCategoryName,
    CategoryId,
    CategoryName

FROM dbo.vw_MaterialDashboard
GROUP BY
    CurrentStationId,
    CurrentStationName,
    CurrentDepartmentId,
    CurrentDepartmentName,
    StationCategoryCode,
    StationCategoryName,
    CategoryId,
    CategoryName

WITH ROLLUP;

GO

-- =============================================
-- Material Movement Trends View
-- Track material movements over time for trend analysis
-- =============================================
USE [requisition]; -- Replace with your actual Requisition database name
GO
-- Drop existing view if it exists
IF OBJECT_ID('dbo.vw_MaterialMovementTrends', 'V') IS NOT NULL
    DROP VIEW dbo.vw_MaterialMovementTrends;
GO

CREATE VIEW dbo.vw_MaterialMovementTrends AS
WITH MovementData AS (
    SELECT
        ma.MaterialId,
        m.MaterialCategoryId,
        mc.Name as CategoryName,
        ma.AssignmentDate,
        ma.PayrollNo,
        COALESCE(ma.StationId, ed.StationId) as StationId,
        COALESCE(lh.StationName, sd.StationName, ed.StationName) as StationName,
        COALESCE(ma.DepartmentId, ed.DepartmentId) as DepartmentId,
        COALESCE(lh.DepartmentName, dept.DepartmentName, ed.DepartmentName) as DepartmentName,

        -- Station category data with fallbacks
        COALESCE(lh.StationCategoryCode, sd.StationCategoryCode) as StationCategoryCode,
        COALESCE(lh.StationCategoryName, sd.StationCategoryName) as StationCategoryName,
        CASE
            WHEN ma.IsActive = 1 THEN 'Assignment'
            ELSE 'Return'
        END as MovementType,
        YEAR(ma.AssignmentDate) as MovementYear,
        MONTH(ma.AssignmentDate) as MovementMonth,
        DATEPART(WEEK, ma.AssignmentDate) as MovementWeek,
        CONVERT(DATE, ma.AssignmentDate) as MovementDate
    FROM MaterialAssignments ma
        INNER JOIN Materials m ON ma.MaterialId = m.Id
        LEFT JOIN MaterialCategories mc ON m.MaterialCategoryId = mc.Id
        LEFT JOIN dbo.vw_EmployeeDetails ed ON ma.PayrollNo = ed.PayrollNo AND ed.IsActive = 0
        LEFT JOIN dbo.vw_LocationHierarchy lh ON ma.StationId = lh.StationId AND ma.DepartmentId = lh.DepartmentId
        -- Use StationDetails as fallback for station category info when LocationHierarchy doesn't match
        LEFT JOIN dbo.vw_StationDetails sd ON COALESCE(ma.StationId, ed.StationId) = sd.StationId
        -- Use Department table as fallback for department names when LocationHierarchy doesn't match
        LEFT JOIN [ktdaleave].[dbo].[Department] dept ON COALESCE(ma.DepartmentId, ed.DepartmentId) = dept.departmentCode
    WHERE ma.AssignmentDate >= DATEADD(MONTH, -12, GETDATE()) -- Last 12 months
)
SELECT
    MovementYear,
    MovementMonth,
    MovementWeek,
    MovementDate,
    CategoryName,
    StationName,
    DepartmentName,
    StationCategoryCode,
    StationCategoryName,
    MovementType,
    COUNT(*) as MovementCount,
    COUNT(DISTINCT MaterialId) as UniqueMaterials,
    COUNT(DISTINCT PayrollNo) as UniqueEmployees,

    -- Rolling averages
    AVG(COUNT(*)) OVER (
        PARTITION BY CategoryName, MovementType
        ORDER BY MovementYear, MovementMonth
        ROWS BETWEEN 2 PRECEDING AND CURRENT ROW
    ) as MovementTrend3Month

FROM MovementData
GROUP BY
    MovementYear,
    MovementMonth,
    MovementWeek,
    MovementDate,
    CategoryName,
    StationName,
    DepartmentName,
    StationCategoryCode,
    StationCategoryName,
    MovementType;

GO

-- =============================================
-- Indexes for Performance Optimization
-- =============================================

-- Index on MaterialAssignments for dashboard queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MaterialAssignments_Dashboard')
CREATE NONCLUSTERED INDEX IX_MaterialAssignments_Dashboard
ON MaterialAssignments (MaterialId, AssignmentDate DESC, IsActive)
INCLUDE (PayrollNo, StationId, DepartmentId);

-- Index on Materials for category and status filtering
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Materials_Dashboard')
CREATE NONCLUSTERED INDEX IX_Materials_Dashboard
ON Materials (MaterialCategoryId, Status)
INCLUDE (PurchasePrice, PurchaseDate, WarrantyEndDate, NextMaintenanceDate);

GO

-- =============================================
-- Test Queries (Comment out in production)
-- =============================================

-- Test the main dashboard view
-- SELECT TOP 10 * FROM dbo.vw_MaterialDashboard ORDER BY PurchasePrice DESC;

-- Test utilization summary
-- SELECT * FROM vw_MaterialUtilizationSummary WHERE CurrentStationId IS NOT NULL;

-- Test movement trends
-- SELECT TOP 20 * FROM vw_MaterialMovementTrends ORDER BY MovementDate DESC;