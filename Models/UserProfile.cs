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
        // Core Identity
        public string PayrollNo { get; set; } = string.Empty;
        public string Fullname { get; set; } = string.Empty;
        public string SurName { get; set; } = string.Empty;
        public string OtherNames { get; set; } = string.Empty;
        
        // Contact & Role
        public string EmailAddress { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Scale { get; set; } = string.Empty;
        public string RollNo { get; set; } = string.Empty;
        
        // Status
        public int IsActive { get; set; }  // Match EmployeeDetailsView (0 = Active)
        
        // Legacy compatibility properties
        public string Name => Fullname;
        public string Email => EmailAddress;
        
        // Department and Station info (populated from LocationAccess for backward compatibility)
        public string Department { get; set; } = string.Empty;
        public string Station { get; set; } = string.Empty;
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
        // Home Department (from EmployeeDetailsView)
        public EnhancedDepartmentInfo HomeDepartment { get; set; } = new();
        
        // Home Station (from EmployeeDetailsView)  
        public EnhancedStationInfo HomeStation { get; set; } = new();
        
        // Station Category (NEW - critical for workflows)
        public StationCategoryInfo StationCategory { get; set; } = new();
        
        // Hierarchy (NEW - from EmployeeDetailsView)
        public HierarchyInfo Hierarchy { get; set; } = new();
        
        // Accessible locations (existing)
        public List<EnhancedDepartmentInfo> AccessibleDepartments { get; set; } = new();
        public List<EnhancedStationInfo> AccessibleStations { get; set; } = new();
        public List<int> AccessibleDepartmentIds { get; set; } = new();
        public List<int> AccessibleStationIds { get; set; } = new();
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

    // New enhanced classes for robust UserProfile structure
    public class StationCategoryInfo
    {
        public string Code { get; set; } = string.Empty;        // headoffice, factory, region, other
        public string Name { get; set; } = string.Empty;        // Head Office, Factory, Region, Other
    }

    public class HierarchyInfo  
    {
        public string HeadOfDepartment { get; set; } = string.Empty;     // PayrollNo
        public string HeadOfDepartmentName { get; set; } = string.Empty;
        public string Supervisor { get; set; } = string.Empty;           // PayrollNo  
        public string SupervisorName { get; set; } = string.Empty;
    }

    public class EnhancedDepartmentInfo
    {
        public int Id { get; set; }           // DepartmentId (int primary key)
        public string Code { get; set; } = string.Empty;      // DepartmentCode (string business code)
        public string Name { get; set; } = string.Empty;      // DepartmentName
    }

    public class EnhancedStationInfo
    {
        public int Id { get; set; }           // StationId (with HQ=0 mapping)
        public string Name { get; set; } = string.Empty;      // StationName (resolved)
        public string OriginalName { get; set; } = string.Empty; // OriginalStationName (raw)
        public StationCategoryInfo Category { get; set; } = new();
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