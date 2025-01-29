using Microsoft.EntityFrameworkCore;
using MRIV.Models;

namespace MRIV.Services
{
    public interface IEmployeeService  // Changed from class to interface
    {
        Task<(EmployeeBkp employee, Department department, Station station)> GetEmployeeAndDepartmentAsync(string payrollNo);
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


    }
}