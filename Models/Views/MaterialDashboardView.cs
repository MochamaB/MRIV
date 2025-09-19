using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MRIV.Enums;

namespace MRIV.Models.Views
{
    /// <summary>
    /// Entity model for vw_MaterialDashboard database view
    /// Comprehensive view combining material data with assignments, movements, and analytics
    /// for the Material-Focused Dashboard
    /// </summary>
    [Table("vw_MaterialDashboard")]
    public class MaterialDashboardView
    {
        [Key]
        public int MaterialId { get; set; }

        // Material Basic Info
        [StringLength(50)]
        public string? MaterialCode { get; set; }

        [StringLength(100)]
        public string? MaterialName { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        // Category Information
        public int CategoryId { get; set; }

        [StringLength(100)]
        public string? CategoryName { get; set; }

        public int? SubcategoryId { get; set; }

        [StringLength(100)]
        public string? SubcategoryName { get; set; }

        // Financial Data
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? PurchasePrice { get; set; }

        public DateTime? PurchaseDate { get; set; }

        // Status (using MaterialStatus enum)
        public MaterialStatus? MaterialStatus { get; set; }

        // Condition Information (from latest condition check)
        public Condition? LatestCondition { get; set; }
        public FunctionalStatus? LatestFunctionalStatus { get; set; }
        public CosmeticStatus? LatestCosmeticStatus { get; set; }
        public DateTime? LastInspectionDate { get; set; }

        [StringLength(20)]
        public string? LastInspectedBy { get; set; }

        // Location Data
        public int? CurrentStationId { get; set; }

        [StringLength(50)]
        public string? CurrentStationName { get; set; }

        public int? CurrentDepartmentId { get; set; }

        [StringLength(100)]
        public string? CurrentDepartmentName { get; set; }

        [StringLength(50)]
        public string? StationCategoryCode { get; set; }

        [StringLength(50)]
        public string? StationCategoryName { get; set; }

        [StringLength(150)]
        public string? FullLocationName { get; set; }

        // Assignment Information
        [StringLength(20)]
        public string? AssignedToPayrollNo { get; set; }

        [StringLength(100)]
        public string? AssignedToEmployeeName { get; set; }

        public DateTime? AssignmentDate { get; set; }

        public bool? IsCurrentlyAssigned { get; set; }

        public RequisitionType? AssignmentType { get; set; }

        // Utilization Metrics
        [StringLength(50)]
        public string? CurrentStatus { get; set; }

        public int? DaysInCurrentStatus { get; set; }

        public int RecentMovements { get; set; }

        // Warranty Information
        public DateTime? WarrantyStartDate { get; set; }
        public DateTime? WarrantyEndDate { get; set; }

        // Maintenance Information
        public DateTime? LastMaintenanceDate { get; set; }
        public DateTime? NextMaintenanceDate { get; set; }

        // Age calculation
        public int? AgeInDays { get; set; }

        // Timestamps
        public DateTime MaterialCreatedAt { get; set; }
        public DateTime? MaterialUpdatedAt { get; set; }
        public DateTime ViewGeneratedAt { get; set; }
    }
}