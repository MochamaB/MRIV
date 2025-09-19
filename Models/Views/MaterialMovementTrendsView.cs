using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MRIV.Models.Views
{
    /// <summary>
    /// Entity model for vw_MaterialMovementTrends database view
    /// Track material movements over time for trend analysis
    /// </summary>
    [Table("vw_MaterialMovementTrends")]
    public class MaterialMovementTrendsView
    {
        // Time dimensions
        public int MovementYear { get; set; }
        public int MovementMonth { get; set; }
        public int MovementWeek { get; set; }
        public DateTime MovementDate { get; set; }

        // Material category
        [StringLength(100)]
        public string? CategoryName { get; set; }

        // Location information (with fallbacks applied)
        [StringLength(50)]
        public string? StationName { get; set; }

        [StringLength(100)]
        public string? DepartmentName { get; set; }

        [StringLength(50)]
        public string? StationCategoryCode { get; set; }

        [StringLength(50)]
        public string? StationCategoryName { get; set; }

        // Movement type
        [StringLength(50)]
        public string? MovementType { get; set; }  // Assignment, Return

        // Aggregated metrics
        public int MovementCount { get; set; }
        public int UniqueMaterials { get; set; }
        public int UniqueEmployees { get; set; }

        // Rolling averages (calculated by database)
        [Column(TypeName = "decimal(10, 2)")]
        public decimal? MovementTrend3Month { get; set; }

        // Composite key properties for Entity Framework
        // Since this view doesn't have a single primary key, we'll use a composite key
        [Key, Column(Order = 0)]
        public string CompositeKey => $"{MovementYear}-{MovementMonth}-{MovementWeek}-{MovementDate:yyyyMMdd}-{CategoryName}-{StationName}-{DepartmentName}-{MovementType}";
    }
}