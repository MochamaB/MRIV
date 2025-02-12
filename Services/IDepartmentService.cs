using Microsoft.EntityFrameworkCore;
using MRIV.Models;

namespace MRIV.Services
{
    public interface IDepartmentService  // Changed from class to interface
    {
        // In IDepartmentService
        Task<Department> GetDepartmentByIdAsync(int departmentId);
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
                    .FirstOrDefaultAsync(d => d.DepartmentCode == departmentId);
            }

        }
    }

