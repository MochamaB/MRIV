-- =============================================
-- Location Hierarchy View Creation Script
-- Purpose: Create unified view for station/department combinations with category mapping
-- Usage: Run this script in the Requisition database
-- Test: SELECT * FROM vw_LocationHierarchy WHERE StationCategoryCode = 'headoffice'
-- =============================================

USE [MRIV]; -- Replace with your actual Requisition database name
GO

-- Drop existing view if it exists
IF OBJECT_ID('dbo.vw_LocationHierarchy', 'V') IS NOT NULL
    DROP VIEW dbo.vw_LocationHierarchy;
GO

-- =============================================
-- Location Hierarchy View
-- Purpose: Provides all station-department combinations with resolved names and categories
-- Usage: Universal location lookup view usable by any query or context
-- Note: Uses vw_StationDetails for clean category mapping and joins with StationCategories table
-- =============================================
CREATE VIEW dbo.vw_LocationHierarchy AS
SELECT
    -- Station information from vw_StationDetails
    sd.StationId,
    sd.StationName,

    -- Department information
    d.departmentCode as DepartmentId,
    d.DepartmentID as DepartmentCode,
    d.DepartmentName,

    -- Combined location name
    CONCAT(sd.StationName, ' - ', d.DepartmentName) as FullLocationName,

    -- Station category information from vw_StationDetails
    sd.StationCategoryCode,
    sd.StationCategoryName,

    -- Additional StationCategory table fields for workflow decisions
    sc.Id as StationCategoryId,
    sc.StationPoint,
    sc.DataSource,
    sc.FilterCriteria,

    -- Sort order for consistent ordering
    CASE
        WHEN sd.StationId = 0 THEN 1  -- HQ gets priority
        WHEN sd.StationCategoryCode = 'region' THEN 2  -- Regions next
        WHEN sd.StationCategoryCode = 'factory' THEN 3  -- Factories
        ELSE 4  -- Others last
    END as SortOrder

FROM dbo.vw_StationDetails sd
CROSS JOIN [ktdaleave].[dbo].[Department] d
LEFT JOIN StationCategories sc ON sd.StationCategoryCode = sc.Code
WHERE d.departmentCode IS NOT NULL;
GO

-- Test the view creation
PRINT 'Testing vw_LocationHierarchy view...';

-- Test HQ mapping (should show StationId = 0)
PRINT 'Testing HQ station mapping...';
SELECT
    StationId,
    StationName,
    StationCategoryCode,
    StationCategoryName
FROM dbo.vw_LocationHierarchy
WHERE StationCategoryCode = 'headoffice'
GROUP BY StationId, StationName, StationCategoryCode, StationCategoryName
ORDER BY StationId;

-- Test regional stations
PRINT 'Testing regional stations...';
SELECT
    StationId,
    StationName,
    StationCategoryCode
FROM dbo.vw_LocationHierarchy
WHERE StationCategoryCode = 'region'
GROUP BY StationId, StationName, StationCategoryCode
ORDER BY StationId;

-- Test other categorization
PRINT 'Testing other categorization...';
SELECT
    StationId,
    StationName,
    StationCategoryCode
FROM dbo.vw_LocationHierarchy
WHERE StationCategoryCode = 'other'
GROUP BY StationId, StationName, StationCategoryCode
ORDER BY StationId;

-- Test factory stations (sample)
PRINT 'Testing factory stations (first 10)...';
SELECT TOP 10
    StationId,
    StationName,
    StationCategoryCode
FROM dbo.vw_LocationHierarchy
WHERE StationCategoryCode = 'factory'
GROUP BY StationId, StationName, StationCategoryCode
ORDER BY StationId;

-- Test full location lookup (sample with HQ department 114)
PRINT 'Testing full location lookup for HQ with ICT department...';
SELECT
    StationId,
    DepartmentCode,
    FullLocationName,
    StationCategoryCode
FROM dbo.vw_LocationHierarchy
WHERE StationId = 0
  AND DepartmentCode = '114'  -- ICT department as used in wizard
ORDER BY FullLocationName;

-- Test StationId = 0 handling specifically
PRINT 'Testing StationId = 0 handling (should show multiple HQ departments)...';
SELECT TOP 5
    StationId,
    StationName,
    DepartmentId,
    DepartmentCode,
    DepartmentName,
    FullLocationName
FROM dbo.vw_LocationHierarchy
WHERE StationId = 0
ORDER BY DepartmentCode;

-- Count by category
PRINT 'Station count by category...';
SELECT
    StationCategoryCode,
    StationCategoryName,
    COUNT(DISTINCT StationId) as StationCount
FROM dbo.vw_LocationHierarchy
GROUP BY StationCategoryCode, StationCategoryName
ORDER BY StationCount DESC;

PRINT 'vw_LocationHierarchy view created successfully!';
GO