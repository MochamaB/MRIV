# KTDALeave Database Structure Analysis

## Overview
Analysis of the KTDALeave database structure, relationships, and data patterns to understand how they impact the MRIV system integration and services. This document serves as a quick reference for understanding the legacy database design.

---

## Core Tables Analysis

### 1. Employee_bkp Table

**Purpose**: Main employee master data table (backup version, now primary)

**Structure**:
```sql
CREATE TABLE [dbo].[Employee_bkp](
    [PayrollNo] [varchar](8) NOT NULL,           -- Primary Key
    [RollNo] [char](5) NOT NULL,                 -- Secondary Key
    [SurName] [varchar](50) NULL,                -- Last name
    [OtherNames] [varchar](50) NULL,             -- First/middle names
    [fullname] AS (isnull([Surname],'')+isnull([othernames],'')), -- Computed column
    [Designation] [varchar](50) NULL,            -- Job title
    [Email_address] [varchar](100) NULL,         -- Email
    [Station] [varchar](50) NULL,                -- Station NAME (not ID)
    [Department] [varchar](50) NULL,             -- Department CODE/NUMBER (as string)
    [Hod] [varchar](50) NULL,                    -- Head of Department PayrollNo
    [supervisor] [varchar](50) NULL,             -- Direct supervisor PayrollNo
    [Role] [varchar](20) NULL,                   -- System role
    [EmpisCurrActive] [int] NULL,                -- Active status (1=active)
    [hire_date] [datetime] NULL,                 -- Employment start
    [contractEnd] [datetime] NULL,               -- Contract end
    [Service_years] [int] NULL,                  -- Years of service
    [password_p] [varchar](50) NULL,             -- Legacy password
    [pass] [varbinary](50) NULL,                 -- Encrypted password
    [username] [varchar](50) NULL,               -- Username
    -- Additional fields...
    CONSTRAINT [PK_Employee_bkp] PRIMARY KEY ([PayrollNo], [RollNo])
)
```

**Key Characteristics**:
- **Composite Primary Key**: (PayrollNo, RollNo) - unusual design
- **String-based References**: Station and Department stored as strings, not foreign keys
- **Computed Column**: fullname automatically concatenates SurName + OtherNames
- **Hierarchical Fields**: Hod and supervisor contain PayrollNo references
- **Active Status**: EmpisCurrActive = 1 for active employees

**Sample Data Patterns**:
```sql
-- PayrollNo patterns: 'FAL02890', 'HGD00032', 'HAM00000'
-- Station patterns: 'HQ', '005', '011', '367', '711'
-- Department patterns: '367', '711', '101', 'HGD', 'HFC'
-- Role patterns: 'FieldUser', 'user', 'Hod', 'FieldSupervisor'
```

### 2. Department Table

**Purpose**: Department master data with hierarchy information

**Structure**:
```sql
CREATE TABLE [dbo].[Department](
    [departmentCode] [int] IDENTITY(1,1) NOT NULL, -- Auto-increment PK
    [DepartmentID] [varchar](50) NULL,              -- Department code/number
    [DepartmentName] [varchar](100) NOT NULL,       -- Full department name
    [DepartmentHD] [nvarchar](50) NULL,             -- Head of Department PayrollNo
    [Emailaddress] [nvarchar](50) NULL,             -- Department email
    [UserName] [varchar](50) NULL,                  -- Department username
    [OrgCode] [varchar](5) NULL,                    -- Organization code
    CONSTRAINT [PK_Department] PRIMARY KEY ([departmentCode])
)
```

**Sample Data**:
```sql
-- departmentCode: 395, 396, 397... (auto-increment)
-- DepartmentID: '100', '101', '102'... (business codes)
-- DepartmentName: 'HEAD OFFICE', 'CHIEF EXECUTIVE', 'CORPORATE SERVICES'
-- DepartmentHD: 'SAP01892', 'SAP02152'... (PayrollNo references)
```

### 3. Station Table

**Purpose**: Station/location master data

**Structure**:
```sql
CREATE TABLE [dbo].[Station](
    [StationID] [int] IDENTITY(1,1) NOT NULL,    -- Auto-increment PK
    [Station_Name] [varchar](50) NULL,           -- Station name
    CONSTRAINT [PK_Station] PRIMARY KEY ([StationID])
)
```

**Sample Data**:
```sql
-- StationID: 1, 2, 3, 4...
-- Station_Name: 'KAPSARA', 'NDUTI', 'GACHEGE', 'GIANCHORE'
```

### 4. Role Table

**Purpose**: System role definitions

**Structure**:
```sql
CREATE TABLE [dbo].[Role](
    [RoleID] [int] IDENTITY(1,1) NOT NULL,       -- Auto-increment PK
    [RoleName] [varchar](20) NULL,               -- Role name
    CONSTRAINT [PK_Role] PRIMARY KEY ([RoleID])
)
```

---

## Critical Integration Challenges

### 1. **Naming Inconsistencies**

**Table Naming**:
- Main table is `Employee_bkp` (not `Employee`)
- Station table uses `Station_Name` (with underscore)
- Department table uses mixed naming conventions

**Column Naming**:
- Inconsistent capitalization: `Email_address`, `hire_date`, `contractEnd`
- Mixed naming styles: `DepartmentHD` vs `Hod`

### 2. **Relationship Patterns**

**No Foreign Key Constraints**:
```sql
-- Employee references Department by string matching
Employee_bkp.Department -> Department.DepartmentID (string comparison)
Employee_bkp.Station -> Station.Station_Name (string comparison) 
Employee_bkp.Hod -> Employee_bkp.PayrollNo (no FK constraint)
Employee_bkp.supervisor -> Employee_bkp.PayrollNo (no FK constraint)
```

**Mapping Requirements**:
```csharp
// Required mapping for integration
string stationName = employee.Station;         // "HQ", "005"
string departmentCode = employee.Department;   // "101", "HGD"

// Must resolve to:
int stationId = GetStationByName(stationName);        // 1, 2, 3...
int departmentId = GetDepartmentByCode(departmentCode); // 395, 396, 397...
```

### 3. **Data Quality Issues**

**Inconsistent Station References**:
```sql
-- Employee data shows stations as:
'HQ', '005', '011', '367', '711'  -- Mix of codes and abbreviations

-- Station master shows:
'KAPSARA', 'NDUTI', 'GACHEGE'     -- Full names
```

**Department Code Variations**:
```sql
-- Employee.Department contains:
'101', '102', 'HGD', 'HFC', '367'  -- Mix of numeric codes and abbreviations

-- Department.DepartmentID contains:
'100', '101', '102'                -- Standardized numeric codes
```

---

## Service Integration Impact

### 1. **EmployeeService Complexity**

The service must handle multiple data sources and mapping:

```csharp
public class EmployeeService
{
    // Must integrate data from multiple contexts
    private readonly KtdaleaveContext _hrContext;
    private readonly RequisitionContext _appContext;
    
    public async Task<Employee> GetEmployeeByPayrollAsync(string payrollNo)
    {
        // Get from HR system (string-based)
        var hrEmployee = await _hrContext.EmployeeBkps
            .FirstOrDefaultAsync(e => e.PayrollNo == payrollNo);
            
        // Map to application model (ID-based)
        var stationId = await ResolveStationId(hrEmployee.Station);
        var departmentId = await ResolveDepartmentId(hrEmployee.Department);
        
        return new Employee
        {
            PayrollNo = hrEmployee.PayrollNo,
            StationId = stationId,
            DepartmentId = departmentId,
            // Additional mapping...
        };
    }
}
```

### 2. **Department Service Patterns**

```csharp
public class DepartmentService
{
    public async Task<Department> GetDepartmentByEmployee(string payrollNo)
    {
        var employee = await _hrContext.EmployeeBkps
            .FirstOrDefaultAsync(e => e.PayrollNo == payrollNo);
            
        // Department lookup requires string matching
        var department = await _hrContext.Departments
            .FirstOrDefaultAsync(d => d.DepartmentID == employee.Department);
            
        return department;
    }
}
```

### 3. **Hierarchy Resolution Services**

```csharp
public class HierarchyService
{
    public async Task<List<EmployeeBkp>> GetReportingChain(string payrollNo)
    {
        var chain = new List<EmployeeBkp>();
        var current = await GetEmployee(payrollNo);
        
        while (current != null)
        {
            chain.Add(current);
            
            // Follow supervisor chain (no FK constraints)
            if (!string.IsNullOrEmpty(current.Supervisor))
            {
                current = await GetEmployee(current.Supervisor);
            }
            else if (!string.IsNullOrEmpty(current.Hod))
            {
                current = await GetEmployee(current.Hod);
            }
            else
            {
                break; // Top of hierarchy
            }
        }
        
        return chain;
    }
}
```

---

## Data Access Patterns

### 1. **Employee Lookups**

**Primary Key Access** (fastest):
```csharp
var employee = await _context.EmployeeBkps
    .FirstOrDefaultAsync(e => e.PayrollNo == payrollNo && e.RollNo == rollNo);
```

**PayrollNo Only** (common pattern):
```csharp
var employee = await _context.EmployeeBkps
    .FirstOrDefaultAsync(e => e.PayrollNo == payrollNo);
```

**Active Employees Only**:
```csharp
var activeEmployees = await _context.EmployeeBkps
    .Where(e => e.EmpisCurrActive == 1)
    .ToListAsync();
```

### 2. **Department/Station Resolution**

**Station Name to ID**:
```csharp
var station = await _context.Stations
    .FirstOrDefaultAsync(s => s.Station_Name.Equals(stationName, 
        StringComparison.OrdinalIgnoreCase));
return station?.StationID ?? 0;
```

**Department Code to ID**:
```csharp
var department = await _context.Departments
    .FirstOrDefaultAsync(d => d.DepartmentID == departmentCode);
return department?.departmentCode ?? 0;
```

### 3. **Hierarchy Queries**

**Department Head**:
```csharp
var department = await _context.Departments
    .FirstOrDefaultAsync(d => d.DepartmentHD == payrollNo);
```

**Direct Reports**:
```csharp
var directReports = await _context.EmployeeBkps
    .Where(e => e.supervisor == supervisorPayrollNo)
    .ToListAsync();
```

---

## Performance Considerations

### 1. **Indexing Requirements**

**Critical Indexes**:
- `Employee_bkp.PayrollNo` (primary key component)
- `Employee_bkp.EmpisCurrActive` (for active employee queries)
- `Employee_bkp.Department` (for department filtering)
- `Employee_bkp.Station` (for station filtering)

**Recommended Additional Indexes**:
```sql
CREATE INDEX IX_Employee_bkp_Active ON Employee_bkp(EmpisCurrActive) WHERE EmpisCurrActive = 1;
CREATE INDEX IX_Employee_bkp_Department ON Employee_bkp(Department);
CREATE INDEX IX_Employee_bkp_Station ON Employee_bkp(Station);
CREATE INDEX IX_Employee_bkp_Supervisor ON Employee_bkp(supervisor);
```

### 2. **Caching Strategies**

**Station/Department Mappings**:
```csharp
// Cache the mapping tables - they change infrequently
var stationCache = await _context.Stations.ToDictionaryAsync(s => s.Station_Name, s => s.StationID);
var departmentCache = await _context.Departments.ToDictionaryAsync(d => d.DepartmentID, d => d.departmentCode);
```

### 3. **Query Optimization**

**Avoid N+1 Queries**:
```csharp
// Bad: Multiple database hits
foreach (var employee in employees)
{
    var department = await GetDepartment(employee.Department); // N+1 problem
}

// Good: Single query with join/include
var employeesWithDepartments = await _context.EmployeeBkps
    .Where(e => payrollNos.Contains(e.PayrollNo))
    .Join(_context.Departments, 
          e => e.Department, 
          d => d.DepartmentID, 
          (e, d) => new { Employee = e, Department = d })
    .ToListAsync();
```

---

## Legacy Database Characteristics

### 1. **Design Patterns**

- **Denormalized Structure**: Employee table contains direct string references
- **No Referential Integrity**: Missing foreign key constraints
- **Computed Columns**: Fullname calculated in database
- **Mixed Data Types**: String IDs mixed with integer IDs
- **Backup Table as Primary**: `Employee_bkp` is the active table

### 2. **Data Entry Patterns**

- **Manual Data Entry**: Explains inconsistent station/department codes
- **Legacy System Integration**: Multiple source systems merged
- **Incremental Changes**: Structure evolved over time without redesign

### 3. **Business Logic in Database**

```sql
-- Computed columns handle business logic
fullname AS (isnull([Surname],'')+isnull([othernames],''))

-- Financial year calculations
Finyear AS ([dbo].[fun_derivefinyear]([caldate]))
```

---

## Integration Mapping Tables

### **Station Mapping** (based on sample data)
| Employee.Station | Station.Station_Name | Station.StationID |
|------------------|---------------------|-------------------|
| 'HQ'            | ? (Missing mapping) | ? |
| '005'           | ? (Code-based)      | 5 |
| '011'           | ? (Code-based)      | 11 |
| '367'           | 'KAPSARA'           | 1 |

### **Department Mapping** (based on sample data)
| Employee.Department | Department.DepartmentID | Department.DepartmentName |
|-------------------|-------------------------|---------------------------|
| '101'             | '101'                   | 'CHIEF EXECUTIVE' |
| '102'             | '102'                   | 'CORPORATE SERVICES' |
| 'HGD'             | ? (No direct match)     | ? |
| 'HFC'             | ? (No direct match)     | ? |

---

## Service Implementation Notes

### 1. **Error Handling Requirements**

```csharp
// Handle missing mappings gracefully
public async Task<int> ResolveStationId(string stationName)
{
    if (string.IsNullOrEmpty(stationName)) 
        return 0; // Default/unknown station
        
    var station = await _context.Stations
        .FirstOrDefaultAsync(s => s.Station_Name.Contains(stationName));
        
    if (station == null)
    {
        _logger.LogWarning("Station mapping not found: {StationName}", stationName);
        return 0;
    }
    
    return station.StationID;
}
```

### 2. **Data Validation Patterns**

```csharp
// Validate employee data integrity
public async Task<List<string>> ValidateEmployeeData(EmployeeBkp employee)
{
    var issues = new List<string>();
    
    // Check station mapping
    var station = await ResolveStationId(employee.Station);
    if (station == 0)
        issues.Add($"Invalid station: {employee.Station}");
    
    // Check department mapping
    var department = await ResolveDepartmentId(employee.Department);
    if (department == 0)
        issues.Add($"Invalid department: {employee.Department}");
    
    // Check supervisor exists
    if (!string.IsNullOrEmpty(employee.Supervisor))
    {
        var supervisor = await _context.EmployeeBkps
            .AnyAsync(e => e.PayrollNo == employee.Supervisor);
        if (!supervisor)
            issues.Add($"Invalid supervisor: {employee.Supervisor}");
    }
    
    return issues;
}
```

### 3. **Synchronization Strategies**

```csharp
// Keep application cache synchronized with HR data
public class EmployeeSyncService
{
    public async Task SyncEmployeeHierarchy()
    {
        // Sync employees with role group assignments
        var activeEmployees = await _hrContext.EmployeeBkps
            .Where(e => e.EmpisCurrActive == 1)
            .ToListAsync();
            
        foreach (var employee in activeEmployees)
        {
            // Ensure employee exists in application context
            await EnsureEmployeeInAppContext(employee);
        }
    }
}
```

---

*This analysis reflects the current state of the KTDALeave database as of the database export. The structure represents a legacy system with typical enterprise database evolution patterns.*