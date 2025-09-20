using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MRIV.Models.Views
{
    /// <summary>
    /// Entity model for vw_MaterialUtilizationSummary database view
    /// Aggregated metrics for dashboard KPIs and summary statistics
    /// </summary>
    [Table("vw_MaterialUtilizationSummary")]
    public class MaterialUtilizationSummaryView
    {
        // Overall Metrics
        public int TotalMaterials { get; set; }

        public double TotalValue { get; set; }

        public int AvailableMaterials { get; set; }
        public int AssignedMaterials { get; set; }

        public double UtilizationRate { get; set; }

        // Raw counts for alert calculation in application layer
        public int WarrantyExpiringSoonCount { get; set; }
        public int WarrantyExpiredCount { get; set; }
        public int MaintenanceOverdueCount { get; set; }
        public int MaintenanceDueSoonCount { get; set; }

        // Value aggregations (raw values for application layer categorization)
        public double TotalPurchaseValue { get; set; }

        public double AveragePurchaseValue { get; set; }

        public double MaxPurchaseValue { get; set; }

        public int HighValueCount { get; set; }

        // Assignment Status Distribution (from CurrentStatus calculation)
        public int AssignmentStatusAssigned { get; set; }
        public int AssignmentStatusAvailable { get; set; }
        public int AssignmentStatusRecentlyReturned { get; set; }

        // Raw MaterialStatus enum counts (let application layer interpret)
        public int MaterialStatus1Count { get; set; }  // UnderMaintenance
        public int MaterialStatus2Count { get; set; }  // LostOrStolen
        public int MaterialStatus3Count { get; set; }  // Disposed
        public int MaterialStatus4Count { get; set; }  // Available
        public int MaterialStatus5Count { get; set; }  // Assigned
        public int MaterialStatus6Count { get; set; }  // InProcess

        // Location Grouping (including station category from LocationHierarchy)
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

        public int CategoryId { get; set; }

        [StringLength(100)]
        public string? CategoryName { get; set; }
    }
}