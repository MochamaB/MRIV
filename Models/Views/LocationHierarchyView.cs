using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MRIV.Models.Views
{
    /// <summary>
    /// Entity model for vw_LocationHierarchy database view
    /// Provides all station-department combinations with resolved names and categories
    /// </summary>
    [Table("vw_LocationHierarchy")]
    public class LocationHierarchyView
    {
        // Station information
        public int StationId { get; set; }             // Generic station ID (0 for HQ)

        [StringLength(50)]
        public string StationName { get; set; }

        // Department information
        public int DepartmentId { get; set; }

        [StringLength(50)]
        public string DepartmentCode { get; set; }     // String department code for requisition matching

        [StringLength(100)]
        public string DepartmentName { get; set; }

        // Combined location information
        [StringLength(150)]
        public string FullLocationName { get; set; }   // "StationName - DepartmentName"

        // Station category information
        [StringLength(50)]
        public string StationCategoryCode { get; set; } // headoffice, region, factory, other

        [StringLength(50)]
        public string StationCategoryName { get; set; } // Head Office, Region, Factory, Other

        // Sort order for consistent ordering
        public int SortOrder { get; set; }
    }
}