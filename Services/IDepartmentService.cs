using Microsoft.EntityFrameworkCore;
using MRIV.Models;

namespace MRIV.Services
{
    public interface IDepartmentService  // Changed from class to interface
    {
        // Get Department and Station By Name
        Task<string> GetLocationNameAsync(string locationKey);
        Task<Department> GetDepartmentByNameAsync(string deliveryStation);
        Task<Station> GetStationByStationNameAsync(string employeeStation);

        // Get Department and station by ID
        Task<Department> GetDepartmentByIdAsync(int departmentId);
        Task<Station> GetStationByIdAsync(int stationId);
        Task<string> GetLocationNameFromIdsAsync(int? stationId, string departmentId);
    }
    public class DepartmentService : IDepartmentService
    {
        private readonly KtdaleaveContext _context;
            

            public DepartmentService(KtdaleaveContext context)
            {
                _context = context;
            }

        public async Task<string> GetLocationNameAsync(string locationKey)
        {
            if (string.IsNullOrEmpty(locationKey))
                return "Unknown";

            // Try to get station
            var station = await GetStationByStationNameAsync(locationKey);
            if (station != null)
                return station.StationName;

            // Try to get department
            var department = await GetDepartmentByNameAsync(locationKey);
            if (department != null)
                return department.DepartmentName;

            // Return the original key if not found
            return locationKey;
        }
        

        public async Task<Department> GetDepartmentByNameAsync(string deliveryStation)
        {

            return await _context.Departments
                .FirstOrDefaultAsync(d => d.DepartmentName == deliveryStation);
        }
        public async Task<Station> GetStationByStationNameAsync(string deliveryStation)
            {
           // var stationId = Convert.ToInt32(employeeStation);
            return await _context.Stations
                    .FirstOrDefaultAsync(s => s.StationName == deliveryStation);
            }



        // Get By Name
        public async Task<Department> GetDepartmentByIdAsync(int departmentId)
        {

            return await _context.Departments
                .FirstOrDefaultAsync(d => d.DepartmentId == departmentId.ToString());
        }
       
        public async Task<Station> GetStationByIdAsync(int stationId)
        {
            return await _context.Stations.FirstOrDefaultAsync(s => s.StationId == stationId);
        }

        public async Task<string> GetLocationNameFromIdsAsync(int? stationId, string departmentId)
        {
            // Prioritize station if available
            if (stationId.HasValue && stationId.Value > 0)
            {
                var station = await GetStationByIdAsync(stationId.Value);
                if (station != null)
                    return station.StationName;
            }

            // Fall back to department
            if (!string.IsNullOrEmpty(departmentId))
            {
                int deptId;
                if (int.TryParse(departmentId, out deptId))
                {
                    var department = await GetDepartmentByIdAsync(deptId);
                    if (department != null)
                        return department.DepartmentName;
                }
            }

            return "Unknown";
        }

    }
    }

