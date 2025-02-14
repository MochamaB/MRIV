using Microsoft.EntityFrameworkCore;
using MRIV.Models;

namespace MRIV.Services
{
    public interface IDepartmentService  // Changed from class to interface
    {
        // In IDepartmentService
        Task<Department> GetDepartmentByIdAsync(int departmentId);
        Task<Station> GetStationByIdAsync(string employeeStation);
    }
        public class DepartmentService : IDepartmentService
        {
            private readonly KtdaleaveContext _context;

            public DepartmentService(KtdaleaveContext context)
            {
                _context = context;
            }
            public async Task<Department> GetDepartmentByIdAsync(int departmentId)
            {

                return await _context.Departments
                    .FirstOrDefaultAsync(d => d.DepartmentId == departmentId.ToString());
                }
            public async Task<Station> GetStationByIdAsync(string deliveryStation)
            {
           // var stationId = Convert.ToInt32(employeeStation);
            return await _context.Stations
                    .FirstOrDefaultAsync(s => s.StationName == deliveryStation);
            }

    }
    }

