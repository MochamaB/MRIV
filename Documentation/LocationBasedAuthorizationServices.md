# MRIV System Service Layer Changes
# Location-Based Authorization and Service Improvements

## Table of Contents
1. [New Location Service](#new-location-service)
2. [Enhanced VisibilityAuthorizeService](#enhanced-visibilityauthorizeservice)
3. [RoleGroupAccessService](#rolegroupaccessservice)
4. [Updated StationCategoryService](#updated-stationcategoryservice)

## New Location Service

Create a new service to provide a consistent way to work with locations:

```csharp
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MRIV.Enums;
using MRIV.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MRIV.Services
{
    public interface ILocationService
    {
        // Get location information
        Task<Department> GetDepartmentByIdAsync(int id);
        Task<Station> GetStationByIdAsync(int id);
        Task<Vendor> GetVendorByIdAsync(int id);
        
        // Get locations for a user
        Task<(Department Department, Station Station)> GetUserLocationsAsync(string payrollNo);
        
        // Check if a user belongs to a location
        Task<bool> IsUserInDepartmentAsync(string payrollNo, int departmentId);
        Task<bool> IsUserInStationAsync(string payrollNo, int stationId);
        
        // Get locations for selection UI
        Task<SelectList> GetDepartmentsSelectListAsync(int? selectedId = null);
        Task<SelectList> GetStationsSelectListAsync(int? selectedId = null, int? filterByDepartmentId = null);
        Task<SelectList> GetVendorsSelectListAsync(int? selectedId = null);
        
        // Get location names by ID
        Task<string> GetDepartmentNameAsync(int departmentId);
        Task<string> GetStationNameAsync(int stationId);
        Task<string> GetVendorNameAsync(int vendorId);
        
        // Get location display name based on type and ID
        Task<string> GetLocationDisplayNameAsync(LocationType locationType, int? departmentId, int? stationId, int? vendorId);
    }

    public class LocationService : ILocationService
    {
        private readonly KtdaleaveContext _ktdaContext;
        private readonly RequisitionContext _requisitionContext;
        private readonly ILogger<LocationService> _logger;

        public LocationService(
            KtdaleaveContext ktdaContext,
            RequisitionContext requisitionContext,
            ILogger<LocationService> logger)
        {
            _ktdaContext = ktdaContext;
            _requisitionContext = requisitionContext;
            _logger = logger;
        }

        public async Task<Department> GetDepartmentByIdAsync(int id)
        {
            return await _ktdaContext.Departments.FindAsync(id);
        }

        public async Task<Station> GetStationByIdAsync(int id)
        {
            return await _ktdaContext.Stations.FindAsync(id);
        }

        public async Task<Vendor> GetVendorByIdAsync(int id)
        {
            return await _requisitionContext.Vendors.FindAsync(id);
        }

        public async Task<(Department Department, Station Station)> GetUserLocationsAsync(string payrollNo)
        {
            if (string.IsNullOrEmpty(payrollNo))
                return (null, null);

            var employee = await _ktdaContext.EmployeeBkps
                .FirstOrDefaultAsync(e => e.PayrollNo == payrollNo && e.EmpisCurrActive == 0);

            if (employee == null)
                return (null, null);

            Department department = null;
            Station station = null;

            // Get department
            if (!string.IsNullOrEmpty(employee.Department))
            {
                if (int.TryParse(employee.Department, out int departmentId))
                {
                    department = await _ktdaContext.Departments
                        .FirstOrDefaultAsync(d => d.Id == departmentId);
                }
            }

            // Get station
            if (!string.IsNullOrEmpty(employee.Station))
            {
                if (employee.Station.Equals("HQ", StringComparison.OrdinalIgnoreCase))
                {
                    station = await _ktdaContext.Stations
                        .FirstOrDefaultAsync(s => s.StationName.Equals("HQ", StringComparison.OrdinalIgnoreCase));
                }
                else if (int.TryParse(employee.Station, out int stationId))
                {
                    station = await _ktdaContext.Stations
                        .FirstOrDefaultAsync(s => s.Id == stationId);
                }
            }

            return (department, station);
        }

        public async Task<bool> IsUserInDepartmentAsync(string payrollNo, int departmentId)
        {
            if (string.IsNullOrEmpty(payrollNo))
                return false;

            var employee = await _ktdaContext.EmployeeBkps
                .FirstOrDefaultAsync(e => e.PayrollNo == payrollNo && e.EmpisCurrActive == 0);

            if (employee == null || string.IsNullOrEmpty(employee.Department))
                return false;

            if (int.TryParse(employee.Department, out int employeeDeptId))
            {
                return employeeDeptId == departmentId;
            }

            return false;
        }

        public async Task<bool> IsUserInStationAsync(string payrollNo, int stationId)
        {
            if (string.IsNullOrEmpty(payrollNo))
                return false;

            var employee = await _ktdaContext.EmployeeBkps
                .FirstOrDefaultAsync(e => e.PayrollNo == payrollNo && e.EmpisCurrActive == 0);

            if (employee == null || string.IsNullOrEmpty(employee.Station))
                return false;

            if (employee.Station.Equals("HQ", StringComparison.OrdinalIgnoreCase))
            {
                var hqStation = await _ktdaContext.Stations
                    .FirstOrDefaultAsync(s => s.StationName.Equals("HQ", StringComparison.OrdinalIgnoreCase));
                
                return hqStation != null && hqStation.Id == stationId;
            }
            else if (int.TryParse(employee.Station, out int employeeStationId))
            {
                return employeeStationId == stationId;
            }

            return false;
        }

        public async Task<SelectList> GetDepartmentsSelectListAsync(int? selectedId = null)
        {
            var departments = await _ktdaContext.Departments
                .OrderBy(d => d.DepartmentName)
                .ToListAsync();
                
            return new SelectList(departments, "Id", "DepartmentName", selectedId);
        }

        public async Task<SelectList> GetStationsSelectListAsync(int? selectedId = null, int? filterByDepartmentId = null)
        {
            var query = _ktdaContext.Stations.AsQueryable();
            
            if (filterByDepartmentId.HasValue)
            {
                // If we have station-department mappings, we could filter here
                // For now, we'll just return all stations
            }
            
            var stations = await query
                .OrderBy(s => s.StationName)
                .ToListAsync();
                
            return new SelectList(stations, "Id", "StationName", selectedId);
        }

        public async Task<SelectList> GetVendorsSelectListAsync(int? selectedId = null)
        {
            var vendors = await _requisitionContext.Vendors
                .OrderBy(v => v.Name)
                .ToListAsync();
                
            return new SelectList(vendors, "Id", "Name", selectedId);
        }

        public async Task<string> GetDepartmentNameAsync(int departmentId)
        {
            var department = await GetDepartmentByIdAsync(departmentId);
            return department?.DepartmentName ?? "Unknown Department";
        }

        public async Task<string> GetStationNameAsync(int stationId)
        {
            var station = await GetStationByIdAsync(stationId);
            return station?.StationName ?? "Unknown Station";
        }

        public async Task<string> GetVendorNameAsync(int vendorId)
        {
            var vendor = await GetVendorByIdAsync(vendorId);
            return vendor?.Name ?? "Unknown Vendor";
        }

        public async Task<string> GetLocationDisplayNameAsync(LocationType locationType, int? departmentId, int? stationId, int? vendorId)
        {
            switch (locationType)
            {
                case LocationType.Department:
                    if (departmentId.HasValue)
                        return await GetDepartmentNameAsync(departmentId.Value);
                    break;
                    
                case LocationType.Station:
                    if (stationId.HasValue)
                        return await GetStationNameAsync(stationId.Value);
                    break;
                    
                case LocationType.Vendor:
                    if (vendorId.HasValue)
                        return await GetVendorNameAsync(vendorId.Value);
                    break;
            }
            
            return "Unknown Location";
        }
    }
}
```

## Enhanced VisibilityAuthorizeService

Update the VisibilityAuthorizeService to support both department and station-based filtering:

```csharp
using Microsoft.EntityFrameworkCore;
using MRIV.Enums;
using MRIV.Models;
using System.Linq;
using System.Threading.Tasks;

namespace MRIV.Services
{
    public interface IVisibilityAuthorizeService
    {
        // Existing methods
        Task<IQueryable<T>> ApplyDepartmentScopeAsync<T>(IQueryable<T> query, string userPayrollNo) where T : class;
        Task<bool> UserHasRoleAsync(string userPayrollNo, string roleParameters);
        Task<bool> ShouldRestrictToPayrollAsync(WorkflowStepConfig step, string userPayrollNo);
        
        // New methods for location-based filtering
        Task<IQueryable<T>> ApplyLocationScopeAsync<T>(
            IQueryable<T> query, 
            string userPayrollNo,
            bool checkDepartment = true,
            bool checkStation = true) where T : class;
        
        // Check if user can access a specific entity
        Task<bool> CanUserAccessEntityAsync<T>(T entity, string userPayrollNo) where T : class;
    }

    public class VisibilityAuthorizeService : IVisibilityAuthorizeService
    {
        private readonly IEmployeeService _employeeService;
        private readonly ILocationService _locationService;
        private readonly IRoleGroupAccessService _roleGroupService;
        private readonly ILogger<VisibilityAuthorizeService> _logger;

        public VisibilityAuthorizeService(
            IEmployeeService employeeService,
            ILocationService locationService,
            IRoleGroupAccessService roleGroupService,
            ILogger<VisibilityAuthorizeService> logger)
        {
            _employeeService = employeeService;
            _locationService = locationService;
            _roleGroupService = roleGroupService;
            _logger = logger;
        }

        // Keep existing methods for backward compatibility
        public async Task<IQueryable<T>> ApplyDepartmentScopeAsync<T>(IQueryable<T> query, string userPayrollNo) where T : class
        {
            // Call the new method with default parameters
            return await ApplyLocationScopeAsync(query, userPayrollNo, true, false);
        }

        public async Task<IQueryable<T>> ApplyLocationScopeAsync<T>(
            IQueryable<T> query, 
            string userPayrollNo,
            bool checkDepartment = true,
            bool checkStation = true) where T : class
        {
            var employee = await _employeeService.GetEmployeeByPayrollAsync(userPayrollNo);
            if (employee == null)
                return query.Take(0); // Return empty if employee not found

            // Admin can see all
            if (employee.Role == "Admin")
                return query;

            // Check if user has special access through role groups
            bool hasFullDepartmentAccess = await _roleGroupService.HasFullDepartmentAccessAsync(userPayrollNo);
            bool hasFullStationAccess = await _roleGroupService.HasFullStationAccessAsync(userPayrollNo);

            // If user has full access, no need to filter
            if (hasFullDepartmentAccess && hasFullStationAccess)
                return query;

            // For Approvals
            if (typeof(T) == typeof(Approval))
            {
                var approvalQuery = query.Cast<Approval>();
                
                // Start with all approvals
                IQueryable<Approval> filteredQuery = approvalQuery;
                
                // Get user's department and station
                var (userDepartment, userStation) = await _locationService.GetUserLocationsAsync(userPayrollNo);
                int? userDepartmentId = userDepartment?.Id;
                int? userStationId = userStation?.Id;
                
                // Apply department filter if needed
                if (checkDepartment && !hasFullDepartmentAccess && userDepartmentId.HasValue)
                {
                    filteredQuery = filteredQuery.Where(a => 
                        a.DepartmentId == userDepartmentId.Value || 
                        a.VisibilityScope == VisibilityScope.Global);
                }
                
                // Apply station filter if needed
                if (checkStation && !hasFullStationAccess && userStationId.HasValue)
                {
                    filteredQuery = filteredQuery.Where(a => 
                        a.StationId == userStationId.Value || 
                        a.VisibilityScope == VisibilityScope.Global ||
                        (a.VisibilityScope == VisibilityScope.Station && a.StationId == userStationId.Value));
                }
                
                return filteredQuery.Cast<T>();
            }
            
            // For Requisitions
            if (typeof(T) == typeof(Requisition))
            {
                var requisitionQuery = query.Cast<Requisition>();
                
                // Start with all requisitions
                IQueryable<Requisition> filteredQuery = requisitionQuery;
                
                // Get user's department and station
                var (userDepartment, userStation) = await _locationService.GetUserLocationsAsync(userPayrollNo);
                int? userDepartmentId = userDepartment?.Id;
                int? userStationId = userStation?.Id;
                
                // Apply department filter if needed
                if (checkDepartment && !hasFullDepartmentAccess && userDepartmentId.HasValue)
                {
                    filteredQuery = filteredQuery.Where(r => 
                        r.DepartmentId == userDepartmentId.Value || 
                        r.VisibilityScope == VisibilityScope.Global);
                }
                
                // Apply station filter if needed
                if (checkStation && !hasFullStationAccess && userStationId.HasValue)
                {
                    filteredQuery = filteredQuery.Where(r => 
                        (r.IssueLocationType == LocationType.Station && r.IssueStationId == userStationId.Value) ||
                        (r.DeliveryLocationType == LocationType.Station && r.DeliveryStationId == userStationId.Value) ||
                        r.VisibilityScope == VisibilityScope.Global ||
                        (r.VisibilityScope == VisibilityScope.Station && 
                         ((r.IssueLocationType == LocationType.Station && r.IssueStationId == userStationId.Value) ||
                          (r.DeliveryLocationType == LocationType.Station && r.DeliveryStationId == userStationId.Value))));
                }
                
                return filteredQuery.Cast<T>();
            }

            // For other entity types, no filtering
            return query;
        }

        public async Task<bool> CanUserAccessEntityAsync<T>(T entity, string userPayrollNo) where T : class
        {
            var employee = await _employeeService.GetEmployeeByPayrollAsync(userPayrollNo);
            if (employee == null)
                return false;

            // Admin can see all
            if (employee.Role == "Admin")
                return true;

            // Check if user has special access through role groups
            bool hasFullDepartmentAccess = await _roleGroupService.HasFullDepartmentAccessAsync(userPayrollNo);
            bool hasFullStationAccess = await _roleGroupService.HasFullStationAccessAsync(userPayrollNo);

            // If user has full access, they can access everything
            if (hasFullDepartmentAccess && hasFullStationAccess)
                return true;

            // Get user's department and station
            var (userDepartment, userStation) = await _locationService.GetUserLocationsAsync(userPayrollNo);
            int? userDepartmentId = userDepartment?.Id;
            int? userStationId = userStation?.Id;

            // For Approval entities
            if (entity is Approval approval)
            {
                // Check department access
                if (!hasFullDepartmentAccess && userDepartmentId.HasValue)
                {
                    if (approval.DepartmentId != userDepartmentId.Value && 
                        approval.VisibilityScope != VisibilityScope.Global)
                    {
                        // If not in same department and not global visibility, check station
                        if (!hasFullStationAccess && userStationId.HasValue)
                        {
                            if (approval.StationId != userStationId.Value || 
                                approval.VisibilityScope != VisibilityScope.Station)
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                
                // Additional checks for role parameters and payroll restrictions
                if (approval.StepConfig != null)
                {
                    // Check if user has the required role
                    bool hasRole = await UserHasRoleAsync(userPayrollNo, approval.StepConfig.RoleParameters);
                    if (!hasRole)
                        return false;
                        
                    // Check if step is restricted to specific payroll
                    bool restrictToPayroll = await ShouldRestrictToPayrollAsync(approval.StepConfig, userPayrollNo);
                    if (restrictToPayroll && approval.Requisition?.PayrollNo != userPayrollNo)
                        return false;
                }
                
                return true;
            }
            
            // For Requisition entities
            if (entity is Requisition requisition)
            {
                // Check department access
                if (!hasFullDepartmentAccess && userDepartmentId.HasValue)
                {
                    if (requisition.DepartmentId != userDepartmentId.Value && 
                        requisition.VisibilityScope != VisibilityScope.Global)
                    {
                        // If not in same department and not global visibility, check station
                        if (!hasFullStationAccess && userStationId.HasValue)
                        {
                            bool isUserStation = 
                                (requisition.IssueLocationType == LocationType.Station && 
                                 requisition.IssueStationId == userStationId.Value) ||
                                (requisition.DeliveryLocationType == LocationType.Station && 
                                 requisition.DeliveryStationId == userStationId.Value);
                                
                            if (!isUserStation || requisition.VisibilityScope != VisibilityScope.Station)
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                
                return true;
            }
            
            // For other entity types, default to true
            return true;
        }

        // Keep existing methods
        public async Task<bool> UserHasRoleAsync(string userPayrollNo, string roleParameters)
        {
            // Existing implementation
            if (string.IsNullOrEmpty(roleParameters))
                return true;

            var employee = await _employeeService.GetEmployeeByPayrollAsync(userPayrollNo);
            if (employee == null)
                return false;

            var roles = roleParameters.Split(',').Select(r => r.Trim()).ToList();
            return roles.Contains(employee.Role);
        }

        public async Task<bool> ShouldRestrictToPayrollAsync(WorkflowStepConfig step, string userPayrollNo)
        {
            // Existing implementation
            if (step == null)
                return false;

            // Parse the parameter conditions
            try
            {
                if (!string.IsNullOrEmpty(step.ParameterConditions))
                {
                    var conditions = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, bool>>(step.ParameterConditions);
                    if (conditions != null && conditions.TryGetValue("restrictToPayroll", out bool restrictToPayroll))
                    {
                        return restrictToPayroll;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing parameter conditions for step {StepId}", step.Id);
            }

            return false;
        }
    }
}
```
