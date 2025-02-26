using System.ComponentModel.DataAnnotations;

namespace MRIV.Models
{
    public class StationCategory
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string Code { get; set; }  // Unique identifier (e.g., "headoffice")

        [Required, StringLength(50)]
        public string StationName { get; set; } // Display name (e.g., "Head Office")

        [Required, StringLength(10)]
        public string StationPoint { get; set; } // "issue", "delivery", or "both"

        public string? DataSource { get; set; }  // The data source type: "Department", "Station", "Vendor"
        public string? FilterCriteria { get; set; }  // Optional filter criteria in JSON format
    }
}
