using Microsoft.EntityFrameworkCore;
using MRIV.Models;
using static System.Collections.Specialized.BitVector32;

namespace MRIV.Services
{
    public interface IEmployeeService  // Changed from class to interface
    {
        Task<(EmployeeBkp loggedInUserEmployee, Department loggedInUserDepartment, Station loggedInUserStation)> GetEmployeeAndDepartmentAsync(string payrollNo);
        EmployeeBkp GetSupervisor(EmployeeBkp employee);
        Task<EmployeeBkp> GetFactoryEmployeeAsync(string stationName); // Changed to Task<EmployeeBkp>
        Task<EmployeeBkp> GetRegionEmployee(); // Changed to Task<EmployeeBkp>
        Task<EmployeeBkp> GetHoEmployeeAsync(string departmentName);

        Task<EmployeeBkp> GetEmployeeByPayrollAsync(string payrollNo);
        Task<List<EmployeeBkp>> GetEmployeesByDepartmentAsync(int departmentId);
        Task<List<EmployeeBkp>> GetFactoryEmployeesByStationAsync(string station);
        Task<List<EmployeeBkp>> GetSupervisorsByDepartmentAsync(int departmentId);
    }

    public class EmployeeService : IEmployeeService
    {
        private readonly KtdaleaveContext _context;

        public EmployeeService(KtdaleaveContext context)
        {
            _context = context;
        }

        public async Task<(EmployeeBkp loggedInUserEmployee, Department loggedInUserDepartment, Station loggedInUserStation)> GetEmployeeAndDepartmentAsync(string payrollNo)
        {
            var loggedInUserEmployee = await _context.EmployeeBkps
                .FirstOrDefaultAsync(e => e.PayrollNo == payrollNo);

            if (loggedInUserEmployee == null)
                return (null, null, null);

            var loggedInUserDepartment = await _context.Departments
                .FirstOrDefaultAsync(d => d.DepartmentId == loggedInUserEmployee.Department);  // Added Async

            Station loggedInUserStation;
            if (loggedInUserEmployee.Station.Equals("HQ", StringComparison.OrdinalIgnoreCase))
            {
                // Explicitly create an HQ station
                loggedInUserStation = new Station { StationId = 0, StationName = "HQ" };
            }
            else if (int.TryParse(loggedInUserEmployee.Station, out int stationId))
            {
                loggedInUserStation = await _context.Stations
                    .FirstOrDefaultAsync(d => d.StationId == stationId);
            }
            else
            {
                // Handle invalid station data gracefully
                loggedInUserStation = new Station { StationId = -1, StationName = "Unknown" };
            }

            return (loggedInUserEmployee, loggedInUserDepartment, loggedInUserStation);
        }

        public EmployeeBkp GetSupervisor(EmployeeBkp loggedInUserEmployee)
        {
            // Get supervisor from employee's HOD or Supervisor property
            return _context.EmployeeBkps
                .FirstOrDefault(e => e.PayrollNo == loggedInUserEmployee.Supervisor || e.PayrollNo == loggedInUserEmployee.Hod);
        }

        public async Task<EmployeeBkp> GetFactoryEmployeeAsync(string locationName)
        {
            if (string.IsNullOrEmpty(locationName)) return null;

            EmployeeBkp employee = null;
            var station = await _context.Stations
                   .FirstOrDefaultAsync(d => d.StationName == locationName);

            // 1. First attempt: Find employee by station and designation containing "ADMINISTR" (case-insensitive)
            if (station != null)
            {
                string formattedStationId = station.StationId.ToString("D3");
                employee = await _context.EmployeeBkps
                    .Where(e => e.Station == formattedStationId &&
                               e.Designation.ToLower().Contains("field systems administr") && // Partial match for "ADMINISTRATOR" or "ADMINISTRA"
                               e.EmpisCurrActive == 0)
                    .FirstOrDefaultAsync();
            }

            // 2. Fallback: If no employee found, search by designation alone (case-insensitive)
            if (employee == null)
            {
                employee = await _context.EmployeeBkps
                    .Where(e => e.Designation.ToLower().Contains("field systems administr") && 
                                e.Station !="HQ" &&
                                e.EmpisCurrActive == 0)
                    .FirstOrDefaultAsync();
            }

            return employee;
        }

        public async Task<EmployeeBkp> GetRegionEmployee()
        {
            return await _context.EmployeeBkps
                .Where(e => e.Designation.Contains("REGIONAL ICT"))
                .FirstOrDefaultAsync(e=> e.EmpisCurrActive == 0); // Add Async
        }

        public async Task<EmployeeBkp> GetHoEmployeeAsync(string locationName)
        {
            if (string.IsNullOrEmpty(locationName)) return null;
            var department = await _context.Departments
       .FirstOrDefaultAsync(d => d.DepartmentName == locationName);

            if (department != null)
            {
                return await _context.EmployeeBkps
                    .FirstOrDefaultAsync(e => e.Department == department.DepartmentId &&
                e.EmpisCurrActive == 0);
            }
            return null;
        }
        public async Task<EmployeeBkp> GetEmployeeByPayrollAsync(string payrollNo)
        {
            // Implement logic to fetch employee by payroll number
            return await _context.EmployeeBkps.FirstOrDefaultAsync(e => e.PayrollNo == payrollNo);
        }

        public async Task<List<EmployeeBkp>> GetEmployeesByDepartmentAsync(int departmentId)
        {

            return await _context.EmployeeBkps
                .Where(e => e.Department == departmentId.ToString() && e.EmpisCurrActive == 0)
                .OrderBy(e => e.Fullname)
                .ToListAsync();
        }
        public async Task<List<EmployeeBkp>> GetSupervisorsByDepartmentAsync(int departmentId)
        {
            var roles = new[] { "Admin", "Hod", "supervisor" };
            return await _context.EmployeeBkps
                .Where(e => e.Department == departmentId.ToString() && roles.Contains(e.Role)
                && e.EmpisCurrActive == 0)
                .OrderBy(e => e.Fullname)
                .ToListAsync();
        }
        public async Task<List<EmployeeBkp>> GetFactoryEmployeesByStationAsync(string station)
        {
            var deliveryStation = await _context.Stations
                  .FirstOrDefaultAsync(d => d.StationName == station);
            if (deliveryStation != null)
            {
                string formattedStationId = deliveryStation.StationId.ToString("D3");
                return await _context.EmployeeBkps
                .Where(e => e.Station == formattedStationId && e.EmpisCurrActive == 0)
                .OrderBy(e => e.Scale)
                .ToListAsync();
            }
            return null;
        }


    }
}