using Microsoft.EntityFrameworkCore;
using MRIV.Models;
using static System.Collections.Specialized.BitVector32;

namespace MRIV.Services
{
    public interface IEmployeeService  // Changed from class to interface
    {
        Task<(EmployeeBkp employee, Department department, Station station)> GetEmployeeAndDepartmentAsync(string payrollNo);
        EmployeeBkp GetSupervisor(EmployeeBkp employee);
        Task<EmployeeBkp> GetFactoryEmployeeAsync(string stationName); // Changed to Task<EmployeeBkp>
        Task<EmployeeBkp> GetRegionEmployee(); // Changed to Task<EmployeeBkp>
        Task<EmployeeBkp> GetHoEmployeeAsync(string departmentName);
    }

    public class EmployeeService : IEmployeeService
    {
        private readonly KtdaleaveContext _context;

        public EmployeeService(KtdaleaveContext context)
        {
            _context = context;
        }

        public async Task<(EmployeeBkp employee, Department department, Station station)> GetEmployeeAndDepartmentAsync(string payrollNo)
        {
            var employee = await _context.EmployeeBkps
                .FirstOrDefaultAsync(e => e.PayrollNo == payrollNo);

            if (employee == null)
                return (null, null, null);

            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.DepartmentId == employee.Department);  // Added Async

            Station station;
            if (employee.Station.Equals("HQ", StringComparison.OrdinalIgnoreCase))
            {
                // Explicitly create an HQ station
                station = new Station { StationId = 0, StationName = "HQ" };
            }
            else if (int.TryParse(employee.Station, out int stationId))
            {
                station = await _context.Stations
                    .FirstOrDefaultAsync(d => d.StationId == stationId);
            }
            else
            {
                // Handle invalid station data gracefully
                station = new Station { StationId = -1, StationName = "Unknown" };
            }

            return (employee, department, station);
        }

        public EmployeeBkp GetSupervisor(EmployeeBkp employee)
        {
            // Get supervisor from employee's HOD or Supervisor property
            return _context.EmployeeBkps
                .FirstOrDefault(e => e.PayrollNo == employee.Supervisor || e.PayrollNo == employee.Hod);
        }

        public async Task<EmployeeBkp> GetFactoryEmployeeAsync(string locationName)
        {
            if (string.IsNullOrEmpty(locationName)) return null;
            var station = await _context.Stations
                   .FirstOrDefaultAsync(d => d.StationName == locationName);
            if (station != null)
            {
                return _context.EmployeeBkps
                    .Where(e => e.Station == station.StationId.ToString() &&
                               e.Designation == "FIELD SYSTEMS ADMINISTRA")
                    .FirstOrDefault();
            }
            return null;
            
        }

        public async Task<EmployeeBkp> GetRegionEmployee()
        {
            return await _context.EmployeeBkps
                .Where(e => e.Designation.Contains("REGIONAL ICT"))
                .FirstOrDefaultAsync(); // Add Async
        }

        public async Task<EmployeeBkp> GetHoEmployeeAsync(string locationName)
        {
            if (string.IsNullOrEmpty(locationName)) return null;
            var department = await _context.Departments
       .FirstOrDefaultAsync(d => d.DepartmentName == locationName);

            if (department != null)
            {
                return await _context.EmployeeBkps
                    .FirstOrDefaultAsync(e => e.Department == department.DepartmentId);
            }
            return null;
        }


    }
}