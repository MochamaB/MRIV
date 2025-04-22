using MRIV.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MRIV.Services
{
    public interface IVisibilityAuthorizeService
    {
        Task<IQueryable<T>> ApplyDepartmentScopeAsync<T>(IQueryable<T> query, string userPayrollNo) where T : class;
        Task<bool> UserHasRoleForStep(int? stepConfigId, string userRole);
        Task<bool> ShouldRestrictToPayrollAsync(int? stepConfigId);
    }

    public class VisibilityAuthorizeService : IVisibilityAuthorizeService
    {
        private readonly IEmployeeService _employeeService;
        private readonly RequisitionContext _context;

        public VisibilityAuthorizeService(IEmployeeService employeeService, RequisitionContext context)
        {
            _employeeService = employeeService;
            _context = context;
        }

        public async Task<IQueryable<T>> ApplyDepartmentScopeAsync<T>(IQueryable<T> query, string userPayrollNo) where T : class
        {
            var employee = await _employeeService.GetEmployeeByPayrollAsync(userPayrollNo);
            if (employee == null)
                return query.Take(0); // Return empty if employee not found

            // Admin can see all
            if (employee.Role == "Admin")
                return query;

            // For Approvals
            if (typeof(T) == typeof(Approval))
            {
                var departmentId = int.Parse(employee.Department);
                return query.Cast<Approval>()
                           .Where(a => a.DepartmentId == departmentId)
                           .Cast<T>();
            }

            return query;
        }

        public async Task<bool> UserHasRoleForStep(int? stepConfigId, string userRole)
        {
            if (!stepConfigId.HasValue)
                return false;

            var stepConfig = await _context.WorkflowStepConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == stepConfigId.Value);

            if (stepConfig != null &&
                stepConfig.RoleParameters != null &&
                stepConfig.RoleParameters.TryGetValue("roles", out var rolesString))
            {
                List<string> allowedRoles;
                try
                {
                    allowedRoles = JsonSerializer.Deserialize<List<string>>(rolesString);
                }
                catch
                {
                    allowedRoles = rolesString.Split(',').Select(r => r.Trim()).ToList();
                }

                return allowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase);
            }
            return false;
        }

        public async Task<bool> ShouldRestrictToPayrollAsync(int? stepConfigId)
        {
            if (!stepConfigId.HasValue)
                return false;

            var stepConfig = await _context.WorkflowStepConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == stepConfigId.Value);

            if (stepConfig != null &&
                stepConfig.Conditions != null &&
                stepConfig.Conditions.TryGetValue("restrictToPayroll", out var restrictValue))
            {
                // Since TryGetValue returns a string, we need to parse it
                if (bool.TryParse(restrictValue.ToString(), out var parsed))
                    return parsed;

                // Alternative checks if the string is "true" or "false" (case-insensitive)
                return string.Equals(restrictValue.ToString(), "true", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}
