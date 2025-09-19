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
                    RoleInformation = await BuildRoleInformationAsync(payrollNo, employeeDetails), // Pass employeeDetails to avoid duplicate query
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

                // Calculate accessible locations based on permissions using optimized view-based approach
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
            _logger.LogInformation("Attempting to get cached profile for PayrollNo: {PayrollNo}", payrollNo);
            Console.WriteLine($"Attempting to get cached profile for PayrollNo: {payrollNo}");

            var profile = await _cacheService.GetProfileAsync(payrollNo);

            if (profile == null)
            {
                _logger.LogInformation("No cached profile found for PayrollNo: {PayrollNo}, building new profile", payrollNo);
                Console.WriteLine($"No cached profile found for PayrollNo: {payrollNo}, building new profile");
                return await RefreshUserProfileAsync(payrollNo);
            }

            if (profile.CacheInfo.IsExpired)
            {
                _logger.LogInformation("User profile expired for PayrollNo: {PayrollNo}, refreshing", payrollNo);
                Console.WriteLine($"User profile expired for PayrollNo: {payrollNo}, refreshing");
                return await RefreshUserProfileAsync(payrollNo);
            }

            _logger.LogInformation("Using cached profile for PayrollNo: {PayrollNo}, IsAdmin: {IsAdmin}",
                payrollNo, profile.RoleInformation.IsAdmin);
            Console.WriteLine($"Using cached profile for PayrollNo: {payrollNo}, IsAdmin: {profile.RoleInformation.IsAdmin}");

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
                _logger.LogWarning("No PayrollNo found in session for GetCurrentUserProfileAsync");
                Console.WriteLine("No PayrollNo found in session for GetCurrentUserProfileAsync");
                return null;
            }

            _logger.LogInformation("Getting current user profile for PayrollNo: {PayrollNo}", payrollNo);
            Console.WriteLine($"Getting current user profile for PayrollNo: {payrollNo}");

            return await GetCachedProfileAsync(payrollNo);
        }

        private BasicInfo BuildBasicInfoFromView(EmployeeDetailsView employeeDetails)
        {
            _logger.LogInformation("Building BasicInfo from EmployeeDetailsView - PayrollNo: {PayrollNo}, Fullname: '{Fullname}', Role: '{Role}'",
                employeeDetails.PayrollNo, employeeDetails.Fullname, employeeDetails.Role);

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

        private async Task<RoleInformation> BuildRoleInformationAsync(string payrollNo, EmployeeDetailsView employeeDetails)
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

            // Use passed employeeDetails instead of querying again
            roleInfo.SystemRole = employeeDetails?.Role ?? string.Empty;
            roleInfo.IsAdmin = employeeDetails?.Role?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true;

            _logger.LogInformation("Role check for PayrollNo: {PayrollNo} - Role: '{Role}', IsAdmin: {IsAdmin}",
                payrollNo, roleInfo.SystemRole, roleInfo.IsAdmin);

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

            // Admin users bypass all scope restrictions
            if (roleInfo.IsAdmin)
            {
                scope.CanAccessAcrossStations = true;
                scope.CanAccessAcrossDepartments = true;
                scope.IsDefaultUser = false;
                scope.PermissionLevel = PermissionLevel.Administrator;
                return scope;
            }

            // Determine if user is a default user (not in any role group)
            scope.IsDefaultUser = !roleInfo.RoleGroups.Any();

            if (scope.IsDefaultUser)
            {
                // Default users: NULL, NULL - only personal data access
                scope.CanAccessAcrossStations = false;
                scope.CanAccessAcrossDepartments = false;
                scope.PermissionLevel = PermissionLevel.Default;
            }
            else
            {
                // Users in role groups: check permissions (OR logic)
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
            }

            return scope;
        }

        private async Task CalculateAccessibleLocationsAsync(UserProfile profile)
        {
            // Admin users see everything - use separate optimized queries
            if (profile.RoleInformation.IsAdmin)
            {
                // Get all departments from ktdaleave context
                var allDepartments = await _ktdaContext.Departments.ToListAsync();

                // Get all stations with categories from vw_StationDetails
                var allStations = await _requisitionContext.StationDetailsViews
                    .OrderBy(s => s.StationId == 0 ? 1 : s.StationCategoryCode == "region" ? 2 : s.StationCategoryCode == "factory" ? 3 : 4)
                    .ThenBy(s => s.StationName)
                    .ToListAsync();

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
                        Code = s.StationCategoryCode ?? string.Empty,
                        Name = s.StationCategoryName ?? string.Empty
                    }
                }).ToList();

                profile.LocationAccess.AccessibleDepartmentIds = allDepartments.Select(d => d.DepartmentCode).ToList();
                profile.LocationAccess.AccessibleStationIds = allStations.Select(s => s.StationId).ToList();
                return;
            }

            // Default users (not in any role group) have no accessible locations
            // They access data only through PayrollNo filtering, not location filtering
            if (profile.VisibilityScope.IsDefaultUser)
            {
                profile.LocationAccess.AccessibleDepartments = new List<EnhancedDepartmentInfo>();
                profile.LocationAccess.AccessibleStations = new List<EnhancedStationInfo>();
                profile.LocationAccess.AccessibleDepartmentIds = new List<int>();
                profile.LocationAccess.AccessibleStationIds = new List<int>();
                return;
            }

            // Users in role groups: calculate based on their permissions
            if (profile.VisibilityScope.CanAccessAcrossStations && profile.VisibilityScope.CanAccessAcrossDepartments)
            {
                // Full organizational access - same as Admin but logged differently
                var allDepartments = await _ktdaContext.Departments.ToListAsync();
                var allStations = await _requisitionContext.StationDetailsViews
                    .OrderBy(s => s.StationId == 0 ? 1 : s.StationCategoryCode == "region" ? 2 : s.StationCategoryCode == "factory" ? 3 : 4)
                    .ThenBy(s => s.StationName)
                    .ToListAsync();

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
                        Code = s.StationCategoryCode ?? string.Empty,
                        Name = s.StationCategoryName ?? string.Empty
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
                var allStations = await _requisitionContext.StationDetailsViews
                    .OrderBy(s => s.StationId == 0 ? 1 : s.StationCategoryCode == "region" ? 2 : s.StationCategoryCode == "factory" ? 3 : 4)
                    .ThenBy(s => s.StationName)
                    .ToListAsync();

                profile.LocationAccess.AccessibleStations = allStations.Select(s => new EnhancedStationInfo
                {
                    Id = s.StationId,
                    Name = s.StationName ?? string.Empty,
                    OriginalName = s.StationName ?? string.Empty,
                    Category = new StationCategoryInfo
                    {
                        Code = s.StationCategoryCode ?? string.Empty,
                        Name = s.StationCategoryName ?? string.Empty
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

    }
}