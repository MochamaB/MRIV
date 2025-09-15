-- =============================================
-- Station Details View Creation Script
-- Purpose: Create view for station information with category mapping
-- Usage: Run this script in the Requisition database (replace database name as needed)
-- Test: SELECT * FROM vw_StationDetails WHERE StationCategoryCode = 'headoffice'
-- =============================================

USE [MRIV]; -- Replace with your actual Requisition database name
GO

-- Drop existing view if it exists
IF OBJECT_ID('dbo.vw_StationDetails', 'V') IS NOT NULL
    DROP VIEW dbo.vw_StationDetails;
GO

-- =============================================
-- Station Details View
-- Purpose: Provides station information with proper category mapping
-- Usage: Base view for location hierarchy and station categorization
-- Note: Uses enhanced category logic from LocationHierarchyView with proper HQ mapping
-- =============================================
CREATE VIEW dbo.vw_StationDetails AS
SELECT
    -- Station ID with HQ mapping (ID 55 -> 0 for consistency)
    CASE
        WHEN s.StationID = 55 THEN 0  -- Map HEAD OFFICE (ID 55) to generic ID 0 (HQ)
        ELSE s.StationID
    END as StationId,
    
    -- Station Name with HQ normalization
    CASE
        WHEN s.StationID = 55 THEN 'HQ'
        ELSE s.Station_Name
    END as StationName,
    
    -- Station category code using enhanced business logic
    CASE
        -- Head Office: ID 55 or name contains HQ/HEAD OFFICE
        WHEN s.StationID = 55 OR s.Station_Name LIKE '%HEAD OFFICE%' OR s.Station_Name LIKE '%HQ%' THEN 'headoffice'
        
        -- Region: Contains REGION, ZONAL, or specific regional patterns
        WHEN s.Station_Name LIKE '%REGION%'
             OR s.Station_Name LIKE '%ZONAL%'
             OR s.Station_Name LIKE 'REGION[0-9]%' THEN 'region'
        
        -- Other: External entities, KTDA subsidiaries, or other-like names
        WHEN s.Station_Name IN ('EXTERNAL', 'KETEPA', 'KOBEL', 'KTDA_HOLDINGS', 'KTDA_POWER', 'GREENLAND_FEDHA',
                               'KIGALI RWANDA OFFICE', 'Tea machinery & engineering services', 'KTDA MS',
                               'CHAI LOGISTICS CENTER')
             OR s.Station_Name LIKE '%RWANDA%'
             OR s.Station_Name LIKE '%LOGISTICS%'
             OR s.Station_Name LIKE '%ENGINEERING%' THEN 'other'
        
        -- Factory: Everything else (tea collection centers and processing facilities)
        ELSE 'factory'
    END as StationCategoryCode,
    
    -- Station category display name
    CASE
        WHEN s.StationID = 55 OR s.Station_Name LIKE '%HEAD OFFICE%' OR s.Station_Name LIKE '%HQ%' THEN 'Head Office'
        WHEN s.Station_Name LIKE '%REGION%'
             OR s.Station_Name LIKE '%ZONAL%'
             OR s.Station_Name LIKE 'REGION[0-9]%' THEN 'Region'
        WHEN s.Station_Name IN ('EXTERNAL', 'KETEPA', 'KOBEL', 'KTDA_HOLDINGS', 'KTDA_POWER', 'GREENLAND_FEDHA',
                               'KIGALI RWANDA OFFICE', 'Tea machinery & engineering services', 'KTDA MS',
                               'CHAI LOGISTICS CENTER')
             OR s.Station_Name LIKE '%RWANDA%'
             OR s.Station_Name LIKE '%LOGISTICS%'
             OR s.Station_Name LIKE '%ENGINEERING%' THEN 'Other'
        ELSE 'Factory'
    END as StationCategoryName

FROM [ktdaleave].[dbo].[Station] s
WHERE s.StationID IS NOT NULL;
GO

-- Test the view creation
PRINT 'Testing vw_StationDetails view...';

-- Test HQ mapping (should show StationId = 0)
PRINT 'Testing HQ station mapping...';
SELECT
    StationId,
    StationName,
    StationCategoryCode,
    StationCategoryName
FROM dbo.vw_StationDetails
WHERE StationCategoryCode = 'headoffice'
ORDER BY StationId;

-- Test regional stations
PRINT 'Testing regional stations...';
SELECT
    StationId,
    StationName,
    StationCategoryCode,
    StationCategoryName
FROM dbo.vw_StationDetails
WHERE StationCategoryCode = 'region'
ORDER BY StationId;

-- Test other categorization
PRINT 'Testing other categorization...';
SELECT
    StationId,
    StationName,
    StationCategoryCode,
    StationCategoryName
FROM dbo.vw_StationDetails
WHERE StationCategoryCode = 'other'
ORDER BY StationId;

-- Test factory stations (sample)
PRINT 'Testing factory stations (first 10)...';
SELECT TOP 10
    StationId,
    StationName,
    StationCategoryCode,
    StationCategoryName
FROM dbo.vw_StationDetails
WHERE StationCategoryCode = 'factory'
ORDER BY StationId;

-- Count by category
PRINT 'Station count by category...';
SELECT
    StationCategoryCode,
    StationCategoryName,
    COUNT(*) as StationCount
FROM dbo.vw_StationDetails
GROUP BY StationCategoryCode, StationCategoryName
ORDER BY StationCount DESC;

-- Test specific station lookup
PRINT 'Testing specific station lookups...';
SELECT * FROM dbo.vw_StationDetails WHERE StationId = 0;  -- Should show HQ
SELECT TOP 3 * FROM dbo.vw_StationDetails WHERE StationCategoryCode = 'factory' ORDER BY StationId;

-- Grant permissions (uncomment and modify as needed)
-- GRANT SELECT ON dbo.vw_StationDetails TO [your_app_user];

PRINT 'vw_StationDetails view created successfully!';
GO
