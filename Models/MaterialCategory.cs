using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MRIV.Models.Interfaces;

namespace MRIV.Models
{
    public class MaterialCategory : IHasMedia
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string? Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(500)]
        [Display(Name = "Unit Of Measure")]
        public string? UnitOfMeasure { get; set; }

        // Navigation property
        public virtual ICollection<MaterialSubcategory>? Subcategories { get; set; }
        public virtual ICollection<Material>? Materials { get; set; }
        // Add this property to implement the interface
        [NotMapped] // If you don't want this in the database table
        public virtual ICollection<MediaFile> Media { get; set; } = new List<MediaFile>();
    }
}
