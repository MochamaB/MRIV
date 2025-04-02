using System.ComponentModel.DataAnnotations;

namespace MRIV.Models
{
    public class MaterialCategory
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
        public virtual ICollection<Material>? Materials { get; set; }
    }
}
