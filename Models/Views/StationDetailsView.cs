using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MRIV.Models.Views
{
    /// <summary>
    /// Entity model for vw_StationDetails database view
    /// Provides station information with category mapping
    /// </summary>
    [Table("vw_StationDetails")]
    public class StationDetailsView
    {
        [Key]
        public int StationId { get; set; }             // Generic station ID (0 for HQ)

        [StringLength(50)]
        public string StationName { get; set; }

        [StringLength(50)]
        public string StationCategoryCode { get; set; } // headoffice, region, factory, other

        [StringLength(50)]
        public string StationCategoryName { get; set; } // Head Office, Region, Factory, Other
    }
}