using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MRIV.Enums;
using MRIV.Models;
using MRIV.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MRIV.Services
{
    public interface ILocationService
    {
        // Get location information
        Task<Department> GetDepartmentByIdAsync(string id);
        Task<Station> GetStationByIdAsync(int id);
   

        // Get locations for a user
        Task<(Department Department, Station Station)> GetUserLocationsAsync(string payrollNo);

        // Check if a user belongs to a location
        Task<bool> IsUserInDepartmentAsync(string payrollNo, int departmentId);
        Task<bool> IsUserInStationAsync(string payrollNo, int stationId);

        // Get locations for selection UI
        Task<SelectList> GetDepartmentsSelectListAsync(string? selectedId = null);
        Task<SelectList> GetStationsSelectListAsync(int? selectedId = null, int? filterByDepartmentId = null);
     //   Task<SelectList> GetVendorsSelectListAsync(int? selectedId = null);

        // Get location names by ID
        Task<string> GetDepartmentNameAsync(int departmentId);
        Task<string> GetStationNameAsync(int stationId);
       

        // Get location display name based on type and ID
       // Task<string> GetLocationDisplayNameAsync(LocationType locationType, int? departmentId, int? stationId, int? vendorId);
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

        public async Task<Department> GetDepartmentByIdAsync(string id)
        {
            
            return await _ktdaContext.Departments.FirstOrDefaultAsync(d => d.DepartmentId == id);
        }

        public async Task<Station> GetStationByIdAsync(int id)
        {
            if (id == 0)
            {
                return new Station
                {
                    StationId = 0,
                    StationName = "HQ"
                };
            }

            return await _ktdaContext.Stations.FirstOrDefaultAsync(s => s.StationId == id)
                   ?? new Station { StationId = id, StationName = "Unknown Station" };
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
                        .FirstOrDefaultAsync(d => d.DepartmentId == departmentId.ToString());
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
                        .FirstOrDefaultAsync(s => s.StationId == stationId);
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

                return hqStation != null && hqStation.StationId == stationId;
            }
            else if (int.TryParse(employee.Station, out int employeeStationId))
            {
                return employeeStationId == stationId;
            }

            return false;
        }

        public async Task<SelectList> GetDepartmentsSelectListAsync(string? selectedId = null)
        {
            try
            {
                var departments = await _ktdaContext.Departments
                    .OrderBy(d => d.DepartmentName)
                    .ToListAsync();

                // Use the exact property names from the Department model
                return new SelectList(departments, "DepartmentId", "DepartmentName", selectedId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting departments select list");
                // Return an empty SelectList instead of throwing
                return new SelectList(new List<Department>(), "DepartmentId", "DepartmentName");
            }
        }

        public async Task<SelectList> GetStationsSelectListAsync(int? selectedId = null, int? filterByDepartmentId = null)
        {
            try
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

                // Use the exact property names from the Station model
                return new SelectList(stations, "StationId", "StationName", selectedId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stations select list");
                // Return an empty SelectList instead of throwing
                return new SelectList(new List<Station>(), "StationId", "StationName");
            }
        }
        /*
        public async Task<SelectList> GetVendorsSelectListAsync(int? selectedId = null)
        {
            var vendors = await _requisitionContext.Vendors
                .OrderBy(v => v.Name)
                .ToListAsync();

            return new SelectList(vendors, "Id", "Name", selectedId);
        }
        */

        public async Task<string> GetDepartmentNameAsync(int departmentId)
        {
            var department = await GetDepartmentByIdAsync(departmentId.ToString());
            return department?.DepartmentName ?? "Unknown Department";
        }

        public async Task<string> GetStationNameAsync(int stationId)
        {
            var station = await GetStationByIdAsync(stationId);
            return station?.StationName ?? "Unknown Station";
        }

       

        
    }
}
