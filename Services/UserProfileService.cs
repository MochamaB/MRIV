using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MRIV.Extensions;
using MRIV.Models;
using MRIV.Models.Views;

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
                Console.WriteLine("Building user profile for PayrollNo:", payrollNo);

                // Get employee from vw_EmployeeDetails view for complete resolved information
                var employeeDetails = await _requisitionContext.EmployeeDetailsViews
                    .FirstOrDefaultAsync(e => e.PayrollNo == payrollNo && e.IsActive == 0);

                if (employeeDetails == null)
                {
                    _logger.LogWarning("Employee not found or inactive for PayrollNo: {PayrollNo}", payrollNo);
                    return null;
                }

                var profile = new UserProfile
                {
                    BasicInfo = BuildBasicInfoFromView(employeeDetails),
                    RoleInformation = await BuildRoleInformationAsync(payrollNo),
                    LocationAccess = BuildLocationAccessFromView(employeeDetails),
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
                Console.WriteLine("Successfully built user profile for PayrollNo:", payrollNo);

                return profile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building user profile for PayrollNo: {PayrollNo}", payrollNo);
                Console.WriteLine("Error building user profile for PayrollNo:", payrollNo);

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

        private BasicInfo BuildBasicInfoFromView(EmployeeDetailsView employeeDetails)
        {
            return new BasicInfo
            {
                PayrollNo = employeeDetails.PayrollNo,
                Fullname = employeeDetails.Fullname ?? string.Empty,
                SurName = employeeDetails.SurName ?? string.Empty,
                OtherNames = employeeDetails.OtherNames ?? string.Empty,
                EmailAddress = employeeDetails.EmailAddress ?? string.Empty,
                Designation = employeeDetails.Designation ?? string.Empty,
                Role = employeeDetails.Role ?? string.Empty,
                Scale = employeeDetails.Scale ?? string.Empty,
                RollNo = employeeDetails.RollNo ?? string.Empty,
                IsActive = employeeDetails.IsActive,
                Department = employeeDetails.DepartmentName ?? string.Empty,
                Station = employeeDetails.StationName ?? string.Empty
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

        private LocationAccess BuildLocationAccessFromView(EmployeeDetailsView employeeDetails)
        {
            var locationAccess = new LocationAccess();

            // Department information is already resolved in the view
            if (employeeDetails.DepartmentId.HasValue)
            {
                locationAccess.HomeDepartment = new EnhancedDepartmentInfo
                {
                    Id = employeeDetails.DepartmentId.Value,
                    Code = employeeDetails.DepartmentCode ?? string.Empty,
                    Name = employeeDetails.DepartmentName ?? string.Empty
                };
            }
            else
            {
                // Fallback for employees with unresolved departments
                locationAccess.HomeDepartment = new EnhancedDepartmentInfo
                {
                    Id = 0,
                    Code = employeeDetails.DepartmentCode ?? "Unknown",
                    Name = $"Unknown Department ({employeeDetails.DepartmentCode})"
                };

                _logger.LogWarning("Employee {PayrollNo} has unresolved department: {DepartmentCode}",
                    employeeDetails.PayrollNo, employeeDetails.DepartmentCode);
            }

            // Station information is already resolved in the view with proper HQ=0 mapping
            if (employeeDetails.StationId.HasValue)
            {
                locationAccess.HomeStation = new EnhancedStationInfo
                {
                    Id = employeeDetails.StationId.Value,
                    Name = employeeDetails.StationName ?? string.Empty,
                    OriginalName = employeeDetails.OriginalStationName ?? string.Empty,
                    Category = new StationCategoryInfo
                    {
                        Code = employeeDetails.StationCategoryCode ?? string.Empty,
                        Name = employeeDetails.StationCategoryName ?? string.Empty
                    }
                };
            }
            else
            {
                // Fallback for employees with unresolved stations
                locationAccess.HomeStation = new EnhancedStationInfo
                {
                    Id = 0,
                    Name = $"Unknown Station ({employeeDetails.OriginalStationName})",
                    OriginalName = employeeDetails.OriginalStationName ?? "Unknown",
                    Category = new StationCategoryInfo
                    {
                        Code = "unknown",
                        Name = "Unknown"
                    }
                };

                _logger.LogWarning("Employee {PayrollNo} has unresolved station: {OriginalStation}",
                    employeeDetails.PayrollNo, employeeDetails.OriginalStationName);
            }

            // Station Category (already included in HomeStation)
            locationAccess.StationCategory = locationAccess.HomeStation.Category;

            // Hierarchy information from the view
            locationAccess.Hierarchy = new HierarchyInfo
            {
                HeadOfDepartment = employeeDetails.HeadOfDepartment ?? string.Empty,
                HeadOfDepartmentName = employeeDetails.HeadOfDepartmentName ?? string.Empty,
                Supervisor = employeeDetails.Supervisor ?? string.Empty,
                SupervisorName = employeeDetails.SupervisorName ?? string.Empty
            };

            return locationAccess;
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

                profile.LocationAccess.AccessibleDepartments = allDepartments.Select(d => new EnhancedDepartmentInfo
                {
                    Id = d.DepartmentCode,
                    Code = d.DepartmentId,
                    Name = d.DepartmentName ?? string.Empty
                }).ToList();

                profile.LocationAccess.AccessibleStations = allStations.Select(s => new EnhancedStationInfo
                {
                    Id = s.StationId,
                    Name = s.StationName ?? string.Empty,
                    OriginalName = s.StationName ?? string.Empty,
                    Category = new StationCategoryInfo
                    {
                        Code = DetermineStationCategory(s.StationId, s.StationName),
                        Name = DetermineStationCategoryName(s.StationId, s.StationName)
                    }
                }).ToList();

                profile.LocationAccess.AccessibleDepartmentIds = allDepartments.Select(d => d.DepartmentCode).ToList();
                profile.LocationAccess.AccessibleStationIds = allStations.Select(s => s.StationId).ToList();
            }
            else if (profile.VisibilityScope.CanAccessAcrossDepartments)
            {
                // Can access all departments at home station
                var allDepartments = await _ktdaContext.Departments.ToListAsync();
                profile.LocationAccess.AccessibleDepartments = allDepartments.Select(d => new EnhancedDepartmentInfo
                {
                    Id = d.DepartmentCode,
                    Code = d.DepartmentId,
                    Name = d.DepartmentName ?? string.Empty
                }).ToList();
                profile.LocationAccess.AccessibleDepartmentIds = allDepartments.Select(d => d.DepartmentCode).ToList();

                // Only home station
                profile.LocationAccess.AccessibleStations = new List<EnhancedStationInfo> { profile.LocationAccess.HomeStation };
                profile.LocationAccess.AccessibleStationIds = new List<int> { profile.LocationAccess.HomeStation.Id };
            }
            else if (profile.VisibilityScope.CanAccessAcrossStations)
            {
                // Can access home department at all stations
                var allStations = await _ktdaContext.Stations.ToListAsync();
                profile.LocationAccess.AccessibleStations = allStations.Select(s => new EnhancedStationInfo
                {
                    Id = s.StationId,
                    Name = s.StationName ?? string.Empty,
                    OriginalName = s.StationName ?? string.Empty,
                    Category = new StationCategoryInfo
                    {
                        Code = DetermineStationCategory(s.StationId, s.StationName),
                        Name = DetermineStationCategoryName(s.StationId, s.StationName)
                    }
                }).ToList();
                profile.LocationAccess.AccessibleStationIds = allStations.Select(s => s.StationId).ToList();

                // Only home department
                profile.LocationAccess.AccessibleDepartments = new List<EnhancedDepartmentInfo> { profile.LocationAccess.HomeDepartment };
                profile.LocationAccess.AccessibleDepartmentIds = new List<int> { profile.LocationAccess.HomeDepartment.Id };
            }
            else
            {
                // Default user - only home department and station
                profile.LocationAccess.AccessibleDepartments = new List<EnhancedDepartmentInfo> { profile.LocationAccess.HomeDepartment };
                profile.LocationAccess.AccessibleStations = new List<EnhancedStationInfo> { profile.LocationAccess.HomeStation };
                profile.LocationAccess.AccessibleDepartmentIds = new List<int> { profile.LocationAccess.HomeDepartment.Id };
                profile.LocationAccess.AccessibleStationIds = new List<int> { profile.LocationAccess.HomeStation.Id };
            }
        }

        /// <summary>
        /// Determine station category based on station ID and name (fallback for stations not in view)
        /// </summary>
        private string DetermineStationCategory(int stationId, string stationName)
        {
            // HQ mapping
            if (stationId == 0 || stationId == 55 || stationName?.Contains("HQ", StringComparison.OrdinalIgnoreCase) == true ||
                stationName?.Contains("HEAD OFFICE", StringComparison.OrdinalIgnoreCase) == true)
                return "headoffice";

            // Region mapping
            if (stationName?.Contains("REGION", StringComparison.OrdinalIgnoreCase) == true ||
                stationName?.Contains("ZONAL", StringComparison.OrdinalIgnoreCase) == true)
                return "region";

            // Other mapping
            var otherStations = new[] { "EXTERNAL", "KETEPA", "KOBEL", "KTDA_HOLDINGS", "KTDA_POWER", "GREENLAND_FEDHA" };
            if (otherStations.Any(os => stationName?.Contains(os, StringComparison.OrdinalIgnoreCase) == true))
                return "other";

            // Default to factory
            return "factory";
        }

        /// <summary>
        /// Get display name for station category
        /// </summary>
        private string DetermineStationCategoryName(int stationId, string stationName)
        {
            var category = DetermineStationCategory(stationId, stationName);
            return category switch
            {
                "headoffice" => "Head Office",
                "region" => "Region",
                "other" => "Other",
                "factory" => "Factory",
                _ => "Unknown"
            };
        }
    }
}