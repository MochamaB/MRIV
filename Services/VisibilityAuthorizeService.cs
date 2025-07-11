using MRIV.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MRIV.Services
{
    public interface IVisibilityAuthorizeService
    {
        Task<IQueryable<T>> ApplyVisibilityScopeAsync<T>(IQueryable<T> query, string userPayrollNo) where T : class;
        Task<bool> CanUserAccessEntityAsync<T>(T entity, string userPayrollNo) where T : class;
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

        // Helper: Normalize station (HQ as 0 or "HQ", field stations as 3-digit string)
        private string NormalizeStation(string station)
        {
            if (string.IsNullOrEmpty(station)) return "";
            if (station.Equals("HQ", System.StringComparison.OrdinalIgnoreCase) || station == "0")
                return "0";
            if (int.TryParse(station, out int stationId))
                return stationId.ToString("D3");
            return station;
        }

        // Helper: Normalize department (as string)
        private string NormalizeDepartment(string department)
        {
            if (string.IsNullOrEmpty(department)) return "";
            return department.Trim();
        }

        // Helper: Get all active role groups for a user
        private async Task<List<RoleGroup>> GetActiveRoleGroupsAsync(string payrollNo)
        {
            var groupIds = await _context.RoleGroupMembers
                .Where(m => m.PayrollNo == payrollNo && m.IsActive)
                .Select(m => m.RoleGroupId)
                .ToListAsync();
            if (!groupIds.Any()) return new List<RoleGroup>();
            return await _context.RoleGroups
                .Where(g => groupIds.Contains(g.Id) && g.IsActive)
                .ToListAsync();
        }

        // Central method for scoping queries based on user/group access
        public async Task<IQueryable<T>> ApplyVisibilityScopeAsync<T>(IQueryable<T> query, string userPayrollNo) where T : class
        {
            var employee = await _employeeService.GetEmployeeByPayrollAsync(userPayrollNo);
            if (employee == null)
                return query.Take(0); // No access if user not found

            if (employee.Role == "Admin")
                return query; // Admin sees all

            // Normalize user's department and station
            var userDept = NormalizeDepartment(employee.Department);
            var userStation = NormalizeStation(employee.Station);

            // Get all active role groups for the user
            var roleGroups = await GetActiveRoleGroupsAsync(userPayrollNo);
            bool isInAnyGroup = roleGroups.Any();
            bool canAccessAcrossStations = roleGroups.Any(g => g.CanAccessAcrossStations);
            bool canAccessAcrossDepartments = roleGroups.Any(g => g.CanAccessAcrossDepartments);

            // If not in any group, user is a default user: only their own data
            if (!isInAnyGroup)
            {
                // Approval entity
                if (typeof(T) == typeof(Approval))
                {
                    var approvals = query.Cast<Approval>();
                    return approvals.Where(a => a.PayrollNo == userPayrollNo).Cast<T>();
                }
                // Requisition entity
                if (typeof(T) == typeof(Requisition))
                {
                    var requisitions = query.Cast<Requisition>();
                    return requisitions.Where(r => r.PayrollNo == userPayrollNo).Cast<T>();
                }
                // Add similar logic for other entities as needed
                return query;
            }

            // If in a group but both flags are false: department-at-station access
            if (!canAccessAcrossStations && !canAccessAcrossDepartments)
            {
                if (typeof(T) == typeof(Approval))
                {
                    var approvals = query.Cast<Approval>();
                    return approvals.Where(a =>
                        NormalizeDepartment(a.DepartmentId.ToString()) == userDept &&
                        NormalizeStation(a.StationId.ToString()) == userStation
                    ).Cast<T>();
                }
                if (typeof(T) == typeof(Requisition))
                {
                    var requisitions = query.Cast<Requisition>();
                    return requisitions.Where(r =>
                        NormalizeDepartment(r.DepartmentId.ToString()) == userDept &&
                        (NormalizeStation(r.IssueStationId.ToString()) == userStation || NormalizeStation(r.DeliveryStationId.ToString()) == userStation)
                    ).Cast<T>();
                }
                return query;
            }

            // If can access all departments at their station
            if (!canAccessAcrossStations && canAccessAcrossDepartments)
            {
                if (typeof(T) == typeof(Approval))
                {
                    var approvals = query.Cast<Approval>();
                    return approvals.Where(a => NormalizeStation(a.StationId.ToString()) == userStation).Cast<T>();
                }
                if (typeof(T) == typeof(Requisition))
                {
                    var requisitions = query.Cast<Requisition>();
                    return requisitions.Where(r =>
                        NormalizeStation(r.IssueStationId.ToString()) == userStation || NormalizeStation(r.DeliveryStationId.ToString()) == userStation
                    ).Cast<T>();
                }
                return query;
            }

            // If can access their department at all stations
            if (canAccessAcrossStations && !canAccessAcrossDepartments)
            {
                if (typeof(T) == typeof(Approval))
                {
                    var approvals = query.Cast<Approval>();
                    return approvals.Where(a => NormalizeDepartment(a.DepartmentId.ToString()) == userDept).Cast<T>();
                }
                if (typeof(T) == typeof(Requisition))
                {
                    var requisitions = query.Cast<Requisition>();
                    return requisitions.Where(r => NormalizeDepartment(r.DepartmentId.ToString()) == userDept).Cast<T>();
                }
                return query;
            }

            // If both flags true: all data
            if (canAccessAcrossStations && canAccessAcrossDepartments)
            {
                return query;
            }

            // Fallback: no access
            return query.Take(0);
        }

        // Central method for checking access to a single entity
        public async Task<bool> CanUserAccessEntityAsync<T>(T entity, string userPayrollNo) where T : class
        {
            var employee = await _employeeService.GetEmployeeByPayrollAsync(userPayrollNo);
            if (employee == null)
                return false;
            if (employee.Role == "Admin")
                return true;
            var userDept = NormalizeDepartment(employee.Department);
            var userStation = NormalizeStation(employee.Station);
            var roleGroups = await GetActiveRoleGroupsAsync(userPayrollNo);
            bool isInAnyGroup = roleGroups.Any();
            bool canAccessAcrossStations = roleGroups.Any(g => g.CanAccessAcrossStations);
            bool canAccessAcrossDepartments = roleGroups.Any(g => g.CanAccessAcrossDepartments);

            if (!isInAnyGroup)
            {
                if (entity is Approval approval)
                    return approval.PayrollNo == userPayrollNo;
                if (entity is Requisition requisition)
                    return requisition.PayrollNo == userPayrollNo;
                return false;
            }
            if (!canAccessAcrossStations && !canAccessAcrossDepartments)
            {
                if (entity is Approval approval)
                    return NormalizeDepartment(approval.DepartmentId.ToString()) == userDept && NormalizeStation(approval.StationId.ToString()) == userStation;
                if (entity is Requisition requisition)
                    return NormalizeDepartment(requisition.DepartmentId.ToString()) == userDept && (NormalizeStation(requisition.IssueStationId.ToString()) == userStation || NormalizeStation(requisition.DeliveryStationId.ToString()) == userStation);
                return false;
            }
            if (!canAccessAcrossStations && canAccessAcrossDepartments)
            {
                if (entity is Approval approval)
                    return NormalizeStation(approval.StationId.ToString()) == userStation;
                if (entity is Requisition requisition)
                    return NormalizeStation(requisition.IssueStationId.ToString()) == userStation || NormalizeStation(requisition.DeliveryStationId.ToString()) == userStation;
                return false;
            }
            if (canAccessAcrossStations && !canAccessAcrossDepartments)
            {
                if (entity is Approval approval)
                    return NormalizeDepartment(approval.DepartmentId.ToString()) == userDept;
                if (entity is Requisition requisition)
                    return NormalizeDepartment(requisition.DepartmentId.ToString()) == userDept;
                return false;
            }
            if (canAccessAcrossStations && canAccessAcrossDepartments)
            {
                return true;
            }
            return false;
        }

        // Existing workflow compatibility methods
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
                return allowedRoles.Contains(userRole, System.StringComparer.OrdinalIgnoreCase);
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
                if (bool.TryParse(restrictValue.ToString(), out var parsed))
                    return parsed;
                return string.Equals(restrictValue.ToString(), "true", System.StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }
    }
}
