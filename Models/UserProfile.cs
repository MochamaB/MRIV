using System.ComponentModel.DataAnnotations;

namespace MRIV.Models
{
    /// <summary>
    /// In-memory model for caching comprehensive user profile data
    /// This is NOT a database entity - it's used for session/memory caching only
    /// </summary>
    public class UserProfile
    {
        public BasicInfo BasicInfo { get; set; } = new();
        public RoleInformation RoleInformation { get; set; } = new();
        public LocationAccess LocationAccess { get; set; } = new();
        public VisibilityScope VisibilityScope { get; set; } = new();
        public CacheInfo CacheInfo { get; set; } = new();
    }

    public class BasicInfo
    {
        public string PayrollNo { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Station { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class RoleInformation
    {
        public string SystemRole { get; set; } = string.Empty;
        public List<RoleGroupInfo> RoleGroups { get; set; } = new();
        public bool IsAdmin { get; set; } = false;
    }

    public class RoleGroupInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool CanAccessAcrossStations { get; set; }
        public bool CanAccessAcrossDepartments { get; set; }
        public bool IsActive { get; set; }
    }

    public class LocationAccess
    {
        public DepartmentInfo HomeDepartment { get; set; } = new();
        public StationInfo HomeStation { get; set; } = new();
        public List<int> AccessibleDepartmentIds { get; set; } = new();
        public List<int> AccessibleStationIds { get; set; } = new();
        public List<DepartmentInfo> AccessibleDepartments { get; set; } = new();
        public List<StationInfo> AccessibleStations { get; set; } = new();
    }

    public class DepartmentInfo
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class StationInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    public class VisibilityScope
    {
        public bool CanAccessAcrossStations { get; set; }
        public bool CanAccessAcrossDepartments { get; set; }
        public PermissionLevel PermissionLevel { get; set; } = PermissionLevel.Default;
    }

    public class CacheInfo
    {
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime LastRefresh { get; set; }
        public int Version { get; set; } = 1;
        
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    }

    public enum PermissionLevel
    {
        Default = 0,
        Manager = 1,
        Administrator = 2
    }
}