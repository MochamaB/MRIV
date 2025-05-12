using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MRIV.Models
{
    public class MaterialSubcategory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Category")]
        public int MaterialCategoryId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Sub Category")]
        public string? Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        // Navigation properties
        [ForeignKey("MaterialCategoryId")]
        public virtual MaterialCategory? MaterialCategory { get; set; }

        public virtual ICollection<Material>? Materials { get; set; }

        public virtual ICollection<MediaFile> Media { get; set; } = new List<MediaFile>();
    }
}
