using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using MRIV.Enums;

namespace MRIV.Models
{
    public class Material
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [DisplayName("Category")]
        public int MaterialCategoryId { get; set; }

       
        [StringLength(50)]
        [DisplayName("Code/SNo")]
        public string? Code { get; set; }

       
        [StringLength(100)]
        public string? Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [DisplayName("Current Location")]
        public string? CurrentLocationId { get; set; }

        [StringLength(50)]
        [DisplayName("Vendor/Supplier")]
        public string? VendorId { get; set; }

        public MaterialStatus? Status { get; set; } //In Use, Broken,Dispatched,Being Repaired 



        // Navigation properties
        [ForeignKey("MaterialCategoryId")]
        public virtual MaterialCategory? MaterialCategory { get; set; }

        public virtual ICollection<RequisitionItem>? RequisitionItems { get; set; }
    }
}
