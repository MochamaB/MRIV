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

            var stationId = int.Parse(employee.Station);
            var station = await _context.Stations
               .FirstOrDefaultAsync(d => d.StationId == stationId);  // Added Async

            return (employee, department, station);
        }
    }
}