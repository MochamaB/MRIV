using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using MRIV.Enums;
using MRIV.Models.Interfaces;

namespace MRIV.Models
{
    public class Material : IHasMedia
    {
        // MAIN/MANDATORY DATA /////////////////////
        [Key]
        public int Id { get; set; }

        [Required]
        [DisplayName("Category")]
        public int MaterialCategoryId { get; set; }

        
        [DisplayName("Subcategory")]
        public int? MaterialSubcategoryId { get; set; }

        [StringLength(50)]
        [DisplayName("Code/SNo")]
        public string? Code { get; set; }

       
        [StringLength(100)]
        public string? Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        // Vendor/Supplier information
        [StringLength(50)]
        [DisplayName("Vendor/Supplier")]
        public string? VendorId { get; set; }

        // ADDITIONAL DATA /////////////////////

        // Acquisition data
        [DisplayName("Purchase Date")]
        public DateTime? PurchaseDate { get; set; }

        [DisplayName("Purchase Price")]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? PurchasePrice { get; set; }

        // Warranty information
        [DisplayName("Warranty Start Date")]
        public DateTime? WarrantyStartDate { get; set; }

        [DisplayName("Warranty End Date")]
        public DateTime? WarrantyEndDate { get; set; }

        [StringLength(500)]
        [DisplayName("Warranty Terms")]
        public string? WarrantyTerms { get; set; }

        // Lifecycle management
        [DisplayName("Expected Lifespan (months)")]
        public int? ExpectedLifespanMonths { get; set; }

        [DisplayName("Maintenance Schedule (months)")]
        public int? MaintenanceIntervalMonths { get; set; }

        [DisplayName("Last Maintenance Date")]
        public DateTime? LastMaintenanceDate { get; set; }

        [DisplayName("Next Maintenance Date")]
        public DateTime? NextMaintenanceDate { get; set; }

        // Metadata
        [StringLength(100)]
        public string? Manufacturer { get; set; }

        [StringLength(100)]
        [DisplayName("Model Number")]
        public string? ModelNumber { get; set; }

        [StringLength(100)]
        [DisplayName("Serial Number")]
        public string? SerialNumber { get; set; }

        [StringLength(100)]
        [DisplayName("Asset Tag")]
        public string? AssetTag { get; set; }

        [StringLength(1000)]
        public string? Specifications { get; set; }

     
        public MaterialStatus? Status { get; set; } //In Use, Broken,Dispatched,Being Repaired 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Add this property to implement the interface
        [NotMapped] // If you don't want this in the database table
        public virtual ICollection<MediaFile> Media { get; set; } = new List<MediaFile>();

        // Navigation properties
        [ForeignKey("MaterialCategoryId")]
        public virtual MaterialCategory? MaterialCategory { get; set; }

        // Navigation properties
        [ForeignKey("MaterialSubcategoryId")]
        public virtual MaterialSubcategory? MaterialSubcategory { get; set; }

        public virtual ICollection<RequisitionItem>? RequisitionItems { get; set; }

        public virtual ICollection<MaterialAssignment>? MaterialAssignments { get; set; }

        public virtual ICollection<MaterialCondition>? MaterialConditions { get; set; }
    }
}
