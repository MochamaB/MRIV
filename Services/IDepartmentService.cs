using Microsoft.EntityFrameworkCore;
using MRIV.Models;

namespace MRIV.Services
{
    public interface IDepartmentService  // Changed from class to interface
    {
        // In IDepartmentService
        // Add this new method
        Task<string> GetLocationNameAsync(string locationKey);
        Task<Department> GetDepartmentByIdAsync(int departmentId);
        Task<Department> GetDepartmentByNameAsync(string deliveryStation);
        Task<Station> GetStationByStationNameAsync(string employeeStation);
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
        public async Task<Department> GetDepartmentByIdAsync(int departmentId)
            {

                return await _context.Departments
                    .FirstOrDefaultAsync(d => d.DepartmentId == departmentId.ToString());
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

    }
    }

