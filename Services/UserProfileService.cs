using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MRIV.Extensions;
using MRIV.Models;

namespace MRIV.Services
{
    public class UserProfileService : IUserProfileService
    {
        private readonly KtdaleaveContext _ktdaContext;
        private readonly RequisitionContext _requisitionContext;
        private readonly IUserProfileCacheService _cacheService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<UserProfileService> _logger;

        public UserProfileService(
            KtdaleaveContext ktdaContext,
            RequisitionContext requisitionContext,
            IUserProfileCacheService cacheService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<UserProfileService> logger)
        {
            _ktdaContext = ktdaContext;
            _requisitionContext = requisitionContext;
            _cacheService = cacheService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<UserProfile> BuildUserProfileAsync(string payrollNo)
        {
            try
            {
                _logger.LogInformation("Building user profile for PayrollNo: {PayrollNo}", payrollNo);

                // Get employee from KtdaleaveContext
                var employee = await _ktdaContext.EmployeeBkps
                    .FirstOrDefaultAsync(e => e.PayrollNo == payrollNo);

                if (employee == null)
                {
                    _logger.LogWarning("Employee not found for PayrollNo: {PayrollNo}", payrollNo);
                    return null;
                }

                var profile = new UserProfile
                {
                    BasicInfo = await BuildBasicInfoAsync(employee),
                    RoleInformation = await BuildRoleInformationAsync(payrollNo),
                    LocationAccess = await BuildLocationAccessAsync(employee),
                    CacheInfo = new CacheInfo
                    {
                        CreatedAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddMinutes(30), // 30-minute cache
                        LastRefresh = DateTime.UtcNow,
                        Version = 1
                    }
                };

                // Calculate visibility scope based on role groups
                profile.VisibilityScope = BuildVisibilityScope(profile.RoleInformation);

                // Calculate accessible locations based on permissions
                await CalculateAccessibleLocationsAsync(profile);

                _logger.LogInformation("Successfully built user profile for PayrollNo: {PayrollNo}", payrollNo);
                return profile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building user profile for PayrollNo: {PayrollNo}", payrollNo);
                throw;
            }
        }

        public async Task<UserProfile> GetCachedProfileAsync(string payrollNo)
        {
            var profile = await _cacheService.GetProfileAsync(payrollNo);
            
            if (profile != null && profile.CacheInfo.IsExpired)
            {
                _logger.LogInformation("User profile expired for PayrollNo: {PayrollNo}, refreshing", payrollNo);
                return await RefreshUserProfileAsync(payrollNo);
            }

            return profile;
        }

        public async Task<UserProfile> RefreshUserProfileAsync(string payrollNo)
        {
            var profile = await BuildUserProfileAsync(payrollNo);
            if (profile != null)
            {
                await _cacheService.SetProfileAsync(profile);
            }
            return profile;
        }

        public async Task InvalidateProfileAsync(string payrollNo)
        {
            await _cacheService.InvalidateAsync(payrollNo);
            _logger.LogInformation("Invalidated user profile cache for PayrollNo: {PayrollNo}", payrollNo);
        }

        public async Task<UserProfile> GetCurrentUserProfileAsync()
        {
            var payrollNo = _httpContextAccessor.HttpContext?.Session.GetString("EmployeePayrollNo");
            if (string.IsNullOrEmpty(payrollNo))
            {
                return null;
            }

            return await GetCachedProfileAsync(payrollNo);
        }

        private async Task<BasicInfo> BuildBasicInfoAsync(EmployeeBkp employee)
        {
            return new BasicInfo
            {
                PayrollNo = employee.PayrollNo,
                Name = employee.Fullname ?? string.Empty,
                Email = employee.EmailAddress ?? string.Empty,
                Designation = employee.Designation ?? string.Empty,
                Department = employee.Department ?? string.Empty,
                Station = employee.Station ?? string.Empty,
                Role = employee.Role ?? string.Empty
            };
        }

        private async Task<RoleInformation> BuildRoleInformationAsync(string payrollNo)
        {
            var roleInfo = new RoleInformation
            {
                IsAdmin = false,
                RoleGroups = new List<RoleGroupInfo>()
            };

            // Get user's role groups from RequisitionContext
            var roleGroupMembers = await _requisitionContext.RoleGroupMembers
                .Where(rgm => rgm.PayrollNo == payrollNo && rgm.IsActive)
                .Include(rgm => rgm.RoleGroup)
                .ToListAsync();

            foreach (var member in roleGroupMembers)
            {
                if (member.RoleGroup != null && member.RoleGroup.IsActive)
                {
                    roleInfo.RoleGroups.Add(new RoleGroupInfo
                    {
                        Id = member.RoleGroup.Id,
                        Name = member.RoleGroup.Name,
                        Description = member.RoleGroup.Description ?? string.Empty,
                        CanAccessAcrossStations = member.RoleGroup.CanAccessAcrossStations,
                        CanAccessAcrossDepartments = member.RoleGroup.CanAccessAcrossDepartments,
                        IsActive = member.RoleGroup.IsActive
                    });
                }
            }

            // Check if user has admin role
            var employee = await _ktdaContext.EmployeeBkps
                .FirstOrDefaultAsync(e => e.PayrollNo == payrollNo);
            
            roleInfo.SystemRole = employee?.Role ?? string.Empty;
            roleInfo.IsAdmin = employee?.Role?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true;

            return roleInfo;
        }

        private async Task<LocationAccess> BuildLocationAccessAsync(EmployeeBkp employee)
        {
            var locationAccess = new LocationAccess();

            // Map employee's home department (handle legacy inconsistencies)
            if (!string.IsNullOrEmpty(employee.Department))
            {
                Department department = null;
                
                // Try multiple lookup strategies
                // 1. Exact DepartmentId match
                department = await _ktdaContext.Departments
                    .FirstOrDefaultAsync(d => d.DepartmentId == employee.Department);
                
                // 2. If not found, try departmentCode match (if employee.Department is numeric)
                if (department == null && int.TryParse(employee.Department, out int deptCode))
                {
                    department = await _ktdaContext.Departments
                        .FirstOrDefaultAsync(d => d.DepartmentCode == deptCode);
                }
                
                // 3. If still not found, load all departments and try name matching in memory
                if (department == null)
                {
                    var allDepartments = await _ktdaContext.Departments.ToListAsync();
                    
                    department = allDepartments.FirstOrDefault(d => 
                        d.DepartmentName.Contains(employee.Department, StringComparison.OrdinalIgnoreCase) ||
                        d.DepartmentCode.ToString() == employee.Department
                    );
                }
                
                if (department != null)
                {
                    locationAccess.HomeDepartment = new DepartmentInfo
                    {
                        Id = department.DepartmentCode,
                        Code = department.DepartmentId,
                        Name = department.DepartmentName ?? string.Empty
                    };
                }
                else
                {
                    // Fallback: Create a placeholder for unmatched departments
                    locationAccess.HomeDepartment = new DepartmentInfo
                    {
                        Id = 0, // Use 0 for unknown departments
                        Code = employee.Department,
                        Name = $"Unknown Department ({employee.Department})"
                    };
                    
                    _logger.LogWarning("Could not find department mapping for employee {PayrollNo} with department '{Department}'", 
                        employee.PayrollNo, employee.Department);
                }
            }

            // Map employee's home station (handle legacy inconsistencies)
            if (!string.IsNullOrEmpty(employee.Station))
            {
                Station station = null;
                
                // Try multiple lookup strategies
                // 1. Exact name match
                station = await _ktdaContext.Stations
                    .FirstOrDefaultAsync(s => s.StationName == employee.Station);
                
                // 2. If not found, try case-insensitive name match  
                if (station == null)
                {
                    station = await _ktdaContext.Stations
                        .FirstOrDefaultAsync(s => s.StationName.ToLower() == employee.Station.ToLower());
                }
                
                // 3. If not found and employee.Station is numeric, try ID match
                if (station == null && int.TryParse(employee.Station, out int stationId))
                {
                    station = await _ktdaContext.Stations
                        .FirstOrDefaultAsync(s => s.StationId == stationId);
                }
                
                // 4. If still not found, try to find by padded station code (load all and match in memory)
                if (station == null)
                {
                    var normalizedEmployeeStation = NormalizeStationCode(employee.Station);
                    var allStations = await _ktdaContext.Stations.ToListAsync();
                    
                    station = allStations.FirstOrDefault(s => 
                        s.StationId.ToString().PadLeft(3, '0') == normalizedEmployeeStation ||
                        s.StationName.Equals(employee.Station, StringComparison.OrdinalIgnoreCase)
                    );
                }
                
                if (station != null)
                {
                    locationAccess.HomeStation = new StationInfo
                    {
                        Id = station.StationId,
                        Name = station.StationName ?? string.Empty,
                        Code = station.StationId.ToString()
                    };
                }
                else
                {
                    // Fallback: Create a placeholder for unmatched stations
                    locationAccess.HomeStation = new StationInfo
                    {
                        Id = 0, // Use 0 for unknown stations
                        Name = $"Unknown Station ({employee.Station})",
                        Code = employee.Station
                    };
                    
                    _logger.LogWarning("Could not find station mapping for employee {PayrollNo} with station '{Station}'", 
                        employee.PayrollNo, employee.Station);
                }
            }

            return locationAccess;
        }

        /// <summary>
        /// Normalize station codes to handle legacy data inconsistencies
        /// </summary>
        private string NormalizeStationCode(string stationCode)
        {
            if (string.IsNullOrEmpty(stationCode)) return string.Empty;
            
            // Handle special cases
            if (stationCode.Equals("HQ", StringComparison.OrdinalIgnoreCase))
                return "0";
                
            // Convert numeric codes to 3-digit format
            if (int.TryParse(stationCode, out int numericCode))
            {
                return numericCode.ToString("D3"); // Pad to 3 digits: "5" -> "005"
            }
            
            return stationCode;
        }

        private VisibilityScope BuildVisibilityScope(RoleInformation roleInfo)
        {
            var scope = new VisibilityScope();

            if (roleInfo.IsAdmin)
            {
                scope.CanAccessAcrossStations = true;
                scope.CanAccessAcrossDepartments = true;
                scope.PermissionLevel = PermissionLevel.Administrator;
                return scope;
            }

            // Check role group permissions (OR logic - any role group with permission grants it)
            scope.CanAccessAcrossStations = roleInfo.RoleGroups.Any(rg => rg.CanAccessAcrossStations);
            scope.CanAccessAcrossDepartments = roleInfo.RoleGroups.Any(rg => rg.CanAccessAcrossDepartments);

            // Set permission level based on capabilities
            if (scope.CanAccessAcrossStations && scope.CanAccessAcrossDepartments)
            {
                scope.PermissionLevel = PermissionLevel.Administrator;
            }
            else if (scope.CanAccessAcrossDepartments || scope.CanAccessAcrossStations)
            {
                scope.PermissionLevel = PermissionLevel.Manager;
            }
            else
            {
                scope.PermissionLevel = PermissionLevel.Default;
            }

            return scope;
        }

        private async Task CalculateAccessibleLocationsAsync(UserProfile profile)
        {
            if (profile.RoleInformation.IsAdmin || 
                (profile.VisibilityScope.CanAccessAcrossStations && profile.VisibilityScope.CanAccessAcrossDepartments))
            {
                // Admin access - can see all departments and stations
                var allDepartments = await _ktdaContext.Departments.ToListAsync();
                var allStations = await _ktdaContext.Stations.ToListAsync();

                profile.LocationAccess.AccessibleDepartments = allDepartments.Select(d => new DepartmentInfo
                {
                    Id = d.DepartmentCode,
                    Code = d.DepartmentId,
                    Name = d.DepartmentName ?? string.Empty
                }).ToList();

                profile.LocationAccess.AccessibleStations = allStations.Select(s => new StationInfo
                {
                    Id = s.StationId,
                    Name = s.StationName ?? string.Empty,
                    Code = s.StationId.ToString()
                }).ToList();

                profile.LocationAccess.AccessibleDepartmentIds = allDepartments.Select(d => d.DepartmentCode).ToList();
                profile.LocationAccess.AccessibleStationIds = allStations.Select(s => s.StationId).ToList();
            }
            else if (profile.VisibilityScope.CanAccessAcrossDepartments)
            {
                // Can access all departments at home station
                var allDepartments = await _ktdaContext.Departments.ToListAsync();
                profile.LocationAccess.AccessibleDepartments = allDepartments.Select(d => new DepartmentInfo
                {
                    Id = d.DepartmentCode,
                    Code = d.DepartmentId,
                    Name = d.DepartmentName ?? string.Empty
                }).ToList();
                profile.LocationAccess.AccessibleDepartmentIds = allDepartments.Select(d => d.DepartmentCode).ToList();

                // Only home station
                profile.LocationAccess.AccessibleStations = new List<StationInfo> { profile.LocationAccess.HomeStation };
                profile.LocationAccess.AccessibleStationIds = new List<int> { profile.LocationAccess.HomeStation.Id };
            }
            else if (profile.VisibilityScope.CanAccessAcrossStations)
            {
                // Can access home department at all stations
                var allStations = await _ktdaContext.Stations.ToListAsync();
                profile.LocationAccess.AccessibleStations = allStations.Select(s => new StationInfo
                {
                    Id = s.StationId,
                    Name = s.StationName ?? string.Empty,
                    Code = s.StationId.ToString()
                }).ToList();
                profile.LocationAccess.AccessibleStationIds = allStations.Select(s => s.StationId).ToList();

                // Only home department
                profile.LocationAccess.AccessibleDepartments = new List<DepartmentInfo> { profile.LocationAccess.HomeDepartment };
                profile.LocationAccess.AccessibleDepartmentIds = new List<int> { profile.LocationAccess.HomeDepartment.Id };
            }
            else
            {
                // Default user - only home department and station
                profile.LocationAccess.AccessibleDepartments = new List<DepartmentInfo> { profile.LocationAccess.HomeDepartment };
                profile.LocationAccess.AccessibleStations = new List<StationInfo> { profile.LocationAccess.HomeStation };
                profile.LocationAccess.AccessibleDepartmentIds = new List<int> { profile.LocationAccess.HomeDepartment.Id };
                profile.LocationAccess.AccessibleStationIds = new List<int> { profile.LocationAccess.HomeStation.Id };
            }
        }
    }
}