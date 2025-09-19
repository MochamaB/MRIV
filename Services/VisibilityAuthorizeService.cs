using MRIV.Models;
using MRIV.Models.Views;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MRIV.Services
{
    public interface IVisibilityAuthorizeService
    {
        // Existing methods (backward compatibility)
        Task<IQueryable<T>> ApplyVisibilityScopeAsync<T>(IQueryable<T> query, string userPayrollNo) where T : class;
        Task<bool> CanUserAccessEntityAsync<T>(T entity, string userPayrollNo) where T : class;
        Task<bool> UserHasRoleForStep(int? stepConfigId, string userRole);
        Task<bool> ShouldRestrictToPayrollAsync(int? stepConfigId);
        Task<List<Department>> GetVisibleDepartmentsAsync(string userPayrollNo);
        Task<List<Station>> GetVisibleStationsAsync(string userPayrollNo);
        
        // Enhanced methods using cached UserProfile (Phase 2A)
        IQueryable<T> ApplyVisibilityScopeWithProfile<T>(IQueryable<T> query, UserProfile userProfile) where T : class;
        bool CanUserAccessEntityWithProfile<T>(T entity, UserProfile userProfile) where T : class;
    }

    public class VisibilityAuthorizeService : IVisibilityAuthorizeService
    {
        private readonly IEmployeeService _employeeService;
        private readonly IDepartmentService _departmentService;
        private readonly ILocationService _locationService;
        private readonly RequisitionContext _context;
        private readonly KtdaleaveContext _ktdaleaveContext;


        public VisibilityAuthorizeService(IEmployeeService employeeService, RequisitionContext context, 
            KtdaleaveContext ktdaleaveContext, IDepartmentService departmentService, ILocationService locationService)
        {
            _employeeService = employeeService;
            _context = context;
            _ktdaleaveContext = ktdaleaveContext;
            _departmentService = departmentService;
            _locationService = locationService;
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
        // Get visible departments for a user
        public async Task<List<Department>> GetVisibleDepartmentsAsync(string userPayrollNo)
        {
            var employee = await _employeeService.GetEmployeeByPayrollAsync(userPayrollNo);
            if (employee == null)
                return new List<Department>();

            if (employee.Role == "Admin")
                return await _ktdaleaveContext.Departments.ToListAsync();

            var userDept = NormalizeDepartment(employee.Department);
            var roleGroups = await GetActiveRoleGroupsAsync(userPayrollNo);
            bool isInAnyGroup = roleGroups.Any();
            bool canAccessAcrossDepartments = roleGroups.Any(g => g.CanAccessAcrossDepartments);

            // If not in any group or can't access across departments, only user's department
            if (!isInAnyGroup || !canAccessAcrossDepartments)
            {
                if (int.TryParse(userDept, out int deptId))
                {
                    return await _ktdaleaveContext.Departments
                        .Where(d => d.DepartmentCode == deptId)
                        .ToListAsync();
                }
                return new List<Department>();
            }

            // Can access across departments - return all departments
            return await _ktdaleaveContext.Departments.ToListAsync();
        }

        // Get visible stations for a user
        public async Task<List<Station>> GetVisibleStationsAsync(string userPayrollNo)
        {
            var employee = await _employeeService.GetEmployeeByPayrollAsync(userPayrollNo);
            if (employee == null)
                return new List<Station>();

            if (employee.Role == "Admin")
                return await _ktdaleaveContext.Stations.ToListAsync();

            var userStation = NormalizeStation(employee.Station);
            var roleGroups = await GetActiveRoleGroupsAsync(userPayrollNo);
            bool isInAnyGroup = roleGroups.Any();
            bool canAccessAcrossStations = roleGroups.Any(g => g.CanAccessAcrossStations);

            // If not in any group or can't access across stations, only user's station
            if (!isInAnyGroup || !canAccessAcrossStations)
            {
                if (int.TryParse(userStation, out int stationId))
                {
                    return await _ktdaleaveContext.Stations
                        .Where(s => s.StationId == stationId)
                        .ToListAsync();
                }
                return new List<Station>();
            }

            // Can access across stations - return all stations
            return await _locationService.GetStationsWithHQAsync();
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

        // ===== ENHANCED METHODS USING CACHED USERPROFILE (PHASE 2A) =====

        /// <summary>
        /// Enhanced visibility scope using cached UserProfile - NO database calls for user context
        /// Performance improvement: 0 DB calls vs 5-6 DB calls in legacy method
        /// </summary>
        public IQueryable<T> ApplyVisibilityScopeWithProfile<T>(IQueryable<T> query, UserProfile userProfile) where T : class
        {
            if (userProfile == null)
            {
                // Fail secure - return empty query
                return query.Take(0);
            }

            // Admin users see everything
            if (userProfile.RoleInformation.IsAdmin)
            {
                return query;
            }

            // Default users (not in any role group) see only their own data
            if (userProfile.VisibilityScope.IsDefaultUser)
            {
                return ApplyPersonalDataFilter(query, userProfile.BasicInfo.PayrollNo);
            }

            // Users in role groups: apply location-based filtering
            if (typeof(T) == typeof(Requisition))
            {
                return ApplyRequisitionVisibility(query.Cast<Requisition>(), userProfile).Cast<T>();
            }
            else if (typeof(T) == typeof(MaterialAssignment))
            {
                return ApplyMaterialAssignmentVisibility(query.Cast<MaterialAssignment>(), userProfile).Cast<T>();
            }
            else if (typeof(T) == typeof(Approval))
            {
                return ApplyApprovalVisibility(query.Cast<Approval>(), userProfile).Cast<T>();
            }
            else if (typeof(T) == typeof(MaterialDashboardView))
            {
                return ApplyMaterialDashboardVisibility(query.Cast<MaterialDashboardView>(), userProfile).Cast<T>();
            }
            else if (typeof(T) == typeof(MaterialUtilizationSummaryView))
            {
                return ApplyMaterialUtilizationVisibility(query.Cast<MaterialUtilizationSummaryView>(), userProfile).Cast<T>();
            }
            else if (typeof(T) == typeof(MaterialMovementTrendsView))
            {
                return ApplyMaterialMovementTrendsVisibility(query.Cast<MaterialMovementTrendsView>(), userProfile).Cast<T>();
            }

            // For other entity types, return as-is (can be extended)
            return query;
        }

        /// <summary>
        /// Enhanced entity access check using cached UserProfile
        /// </summary>
        public bool CanUserAccessEntityWithProfile<T>(T entity, UserProfile userProfile) where T : class
        {
            if (userProfile == null || entity == null)
                return false;

            if (userProfile.RoleInformation.IsAdmin)
                return true;

            // Default users can only access their own data
            if (userProfile.VisibilityScope.IsDefaultUser)
            {
                if (entity is Requisition requisition)
                    return requisition.PayrollNo == userProfile.BasicInfo.PayrollNo;
                if (entity is Approval approval)
                    return approval.PayrollNo == userProfile.BasicInfo.PayrollNo;
                if (entity is MaterialAssignment assignment)
                    return assignment.PayrollNo == userProfile.BasicInfo.PayrollNo;
                return false;
            }

            // Users in role groups: check based on entity type using cached accessible locations
            if (entity is Requisition req)
            {
                return userProfile.LocationAccess.AccessibleDepartmentIds.Contains(req.DepartmentId) &&
                       (userProfile.LocationAccess.AccessibleStationIds.Contains(req.IssueStationId) ||
                        userProfile.LocationAccess.AccessibleStationIds.Contains(req.DeliveryStationId));
            }
            else if (entity is MaterialAssignment assign)
            {
                return assign.DepartmentId.HasValue &&
                       userProfile.LocationAccess.AccessibleDepartmentIds.Contains(assign.DepartmentId.Value) &&
                       assign.StationId.HasValue &&
                       userProfile.LocationAccess.AccessibleStationIds.Contains(assign.StationId.Value);
            }
            else if (entity is Approval app)
            {
                return userProfile.LocationAccess.AccessibleDepartmentIds.Contains(app.DepartmentId);
            }

            return false;
        }

        // ===== PRIVATE HELPER METHODS FOR ENHANCED VISIBILITY =====

        /// <summary>
        /// Apply personal data filter for default users (PayrollNo-based filtering)
        /// </summary>
        private IQueryable<T> ApplyPersonalDataFilter<T>(IQueryable<T> query, string payrollNo) where T : class
        {
            if (typeof(T) == typeof(Requisition))
            {
                var requisitions = query.Cast<Requisition>();
                return requisitions.Where(r => r.PayrollNo == payrollNo).Cast<T>();
            }
            else if (typeof(T) == typeof(Approval))
            {
                var approvals = query.Cast<Approval>();
                return approvals.Where(a => a.PayrollNo == payrollNo).Cast<T>();
            }
            else if (typeof(T) == typeof(MaterialAssignment))
            {
                var assignments = query.Cast<MaterialAssignment>();
                return assignments.Where(ma => ma.PayrollNo == payrollNo).Cast<T>();
            }

            // For other entity types, return empty query (default users should not see them)
            return query.Take(0);
        }

        private IQueryable<Requisition> ApplyRequisitionVisibility(IQueryable<Requisition> query, UserProfile userProfile)
        {
            var accessibleDeptIds = userProfile.LocationAccess.AccessibleDepartmentIds;
            var accessibleStationIds = userProfile.LocationAccess.AccessibleStationIds;

            return query.Where(r =>
                accessibleDeptIds.Contains(r.DepartmentId) &&
                (accessibleStationIds.Contains(r.IssueStationId) ||
                 accessibleStationIds.Contains(r.DeliveryStationId))
            );
        }

        private IQueryable<MaterialAssignment> ApplyMaterialAssignmentVisibility(IQueryable<MaterialAssignment> query, UserProfile userProfile)
        {
            var accessibleDeptIds = userProfile.LocationAccess.AccessibleDepartmentIds;
            var accessibleStationIds = userProfile.LocationAccess.AccessibleStationIds;

            return query.Where(ma =>
                ma.DepartmentId.HasValue && accessibleDeptIds.Contains(ma.DepartmentId.Value) &&
                ma.StationId.HasValue && accessibleStationIds.Contains(ma.StationId.Value)
            );
        }

        private IQueryable<Approval> ApplyApprovalVisibility(IQueryable<Approval> query, UserProfile userProfile)
        {
            var accessibleDeptIds = userProfile.LocationAccess.AccessibleDepartmentIds;

            return query.Where(a => accessibleDeptIds.Contains(a.DepartmentId));
        }

        // ===================================================================
        // MATERIAL DASHBOARD VISIBILITY METHODS
        // ===================================================================

        /// <summary>
        /// Apply access control to MaterialDashboardView based on user's accessible locations
        /// Follows same pattern as Requisition and Approval visibility
        /// </summary>
        private IQueryable<MaterialDashboardView> ApplyMaterialDashboardVisibility(IQueryable<MaterialDashboardView> query, UserProfile userProfile)
        {
            var accessibleStationIds = userProfile.LocationAccess.AccessibleStationIds;
            var accessibleDeptIds = userProfile.LocationAccess.AccessibleDepartmentIds;

            return query.Where(m =>
                accessibleStationIds.Contains(m.CurrentStationId ?? 0) ||
                accessibleDeptIds.Contains(m.CurrentDepartmentId ?? 0));
        }

        /// <summary>
        /// Apply access control to MaterialUtilizationSummaryView for KPI calculations
        /// </summary>
        private IQueryable<MaterialUtilizationSummaryView> ApplyMaterialUtilizationVisibility(IQueryable<MaterialUtilizationSummaryView> query, UserProfile userProfile)
        {
            var accessibleStationIds = userProfile.LocationAccess.AccessibleStationIds;
            var accessibleDeptIds = userProfile.LocationAccess.AccessibleDepartmentIds;

            return query.Where(s =>
                accessibleStationIds.Contains(s.CurrentStationId ?? 0) ||
                accessibleDeptIds.Contains(s.CurrentDepartmentId ?? 0));
        }

        /// <summary>
        /// Apply access control to MaterialMovementTrendsView for trend analysis
        /// Uses station names since this view is station-centric
        /// </summary>
        private IQueryable<MaterialMovementTrendsView> ApplyMaterialMovementTrendsVisibility(IQueryable<MaterialMovementTrendsView> query, UserProfile userProfile)
        {
            // Use station names from UserProfile (already cached)
            var accessibleStationNames = userProfile.LocationAccess.AccessibleStations
                .Select(s => s.Name)
                .ToList();

            return query.Where(mt => accessibleStationNames.Contains(mt.StationName));
        }
    }
}
