-- =============================================
-- Employee Details View Creation Script
-- Purpose: Create cross-database view for employee information with resolved department/station names
-- Usage: Run this script in the Requisition database (replace database name as needed)
-- Test: SELECT * FROM vw_EmployeeDetails WHERE PayrollNo = 'FAL02890'
-- =============================================

USE [MRIV]; -- Replace with your actual Requisition database name
GO

-- Drop existing view if it exists
IF OBJECT_ID('dbo.vw_EmployeeDetails', 'V') IS NOT NULL
    DROP VIEW dbo.vw_EmployeeDetails;
GO

-- =============================================
-- Employee Details View
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
    
    -- Department information (both integer ID and string code)
    d.departmentCode as DepartmentId,        -- Integer primary key from Department table
    e.Department as DepartmentCode,          -- String business code from Employee_bkp
    ISNULL(d.DepartmentName, 'Unknown Department') as DepartmentName,
    
    -- Station information with category integration
    e.Station as OriginalStationName,        -- Original station value from ktdaleave
    ISNULL(sd.StationName, e.Station) as StationName,  -- Resolved station name
    ISNULL(sd.StationId, 
        CASE 
            WHEN e.Station = 'HQ' OR e.Station = '0' THEN 0  -- Map HQ variations to 0
            WHEN ISNUMERIC(e.Station) = 1 AND e.Station <> '0' THEN TRY_CAST(e.Station AS INT)
            ELSE NULL  -- Unknown station
        END
    ) as StationId,  -- Station ID with HQ = 0 mapping
    
    -- Station category information from vw_StationDetails
    ISNULL(sd.StationCategoryCode, 
        CASE 
            WHEN e.Station = 'HQ' OR e.Station = '0' THEN 'headoffice'
            ELSE 'unknown'
        END
    ) as StationCategoryCode,
    ISNULL(sd.StationCategoryName, 
        CASE 
            WHEN e.Station = 'HQ' OR e.Station = '0' THEN 'Head Office'
            ELSE 'Unknown'
        END
    ) as StationCategoryName,
    
    -- Employee status and role information
    e.EmpisCurrActive as IsActive,
    e.Role,
    e.Designation,
    e.Email_Address,
    e.scale,
    e.RollNo,
    
    -- Hierarchy information
    e.Hod as HeadOfDepartment,
    e.Supervisor,
    ISNULL(hod.Fullname, e.Hod) as HeadOfDepartmentName,
    ISNULL(sup.Fullname, e.Supervisor) as SupervisorName

FROM [ktdaleave].[dbo].[Employee_bkp] e
-- Join with Department table to get integer department ID and name
LEFT JOIN [ktdaleave].[dbo].[Department] d
    ON e.Department = d.DepartmentID  -- String comparison for department codes
-- Join with vw_StationDetails for clean station category integration
LEFT JOIN dbo.vw_StationDetails sd ON (
    e.Station = sd.StationName OR 
    (e.Station = 'HQ' AND sd.StationId = 0) OR
    (e.Station = '0' AND sd.StationId = 0) OR
    (ISNUMERIC(e.Station) = 1 
     AND e.Station <> '0' 
     AND TRY_CAST(e.Station AS INT) = sd.StationId)
)
-- Self-join to get HOD name
LEFT JOIN [ktdaleave].[dbo].[Employee_bkp] hod
    ON e.Hod = hod.PayrollNo
-- Self-join to get Supervisor name
LEFT JOIN [ktdaleave].[dbo].[Employee_bkp] sup
    ON e.Supervisor = sup.PayrollNo
GO

-- Test the view creation
PRINT 'Testing vw_EmployeeDetails view...';

-- Basic test query with new columns
SELECT TOP 5
    PayrollNo,
    Fullname,
    DepartmentId,           -- New integer department ID
    DepartmentCode,         -- String department code
    DepartmentName,
    StationId,              -- New station ID column
    StationName,            -- Resolved station name
    StationCategoryCode,    -- New station category
    StationCategoryName,    -- New station category name
    HeadOfDepartmentName,
    SupervisorName,
    IsActive,
    Role
FROM dbo.vw_EmployeeDetails
WHERE IsActive = 0  -- Active employees (EmpisCurrActive = 0 means active in ktdaleave)
ORDER BY Fullname;

-- Test specific employee lookup
PRINT 'Testing specific employee lookup...';
DECLARE @TestPayroll VARCHAR(8) = (SELECT TOP 1 PayrollNo FROM [ktdaleave].[dbo].[Employee_bkp] WHERE EmpisCurrActive = 0);
SELECT * FROM dbo.vw_EmployeeDetails WHERE PayrollNo = @TestPayroll;

-- Test department filtering (for admin employees)
PRINT 'Testing department filtering for admin employees...';
SELECT
    COUNT(*) as AdminEmployeeCount,
    DepartmentId,
    DepartmentCode,
    DepartmentName
FROM dbo.vw_EmployeeDetails
WHERE DepartmentCode = '106' AND IsActive = 0
GROUP BY DepartmentId, DepartmentCode, DepartmentName;

-- Test station category filtering
PRINT 'Testing station category filtering...';
SELECT
    StationCategoryCode,
    StationCategoryName,
    COUNT(*) as EmployeeCount
FROM dbo.vw_EmployeeDetails
WHERE IsActive = 0
GROUP BY StationCategoryCode, StationCategoryName
ORDER BY EmployeeCount DESC;

-- Test factory employees by department
PRINT 'Testing factory employees by department...';
SELECT TOP 10
    PayrollNo,
    Fullname,
    StationName,
    DepartmentName,
    StationCategoryCode
FROM dbo.vw_EmployeeDetails
WHERE StationCategoryCode = 'factory' 
  AND IsActive = 0
ORDER BY DepartmentName, Fullname;

-- Test HQ employees (StationId = 0)
PRINT 'Testing HQ employees (StationId = 0)...';
SELECT TOP 5
    PayrollNo,
    Fullname,
    StationId,
    StationName,
    StationCategoryCode,
    DepartmentName
FROM dbo.vw_EmployeeDetails
WHERE StationId = 0 AND IsActive = 0
ORDER BY DepartmentName, Fullname;

-- Grant permissions (uncomment and modify as needed)
-- GRANT SELECT ON dbo.vw_EmployeeDetails TO [your_app_user];

PRINT 'vw_EmployeeDetails view created successfully!';
GO