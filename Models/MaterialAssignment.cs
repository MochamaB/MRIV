using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;
using MRIV.Enums;

namespace MRIV.Models
{
    public class MaterialAssignment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MaterialId { get; set; }

        [Required]
        [StringLength(20)]
        [DisplayName("Assigned To")]
        public string PayrollNo { get; set; }

        [DisplayName("Assignment Date")]
        public DateTime AssignmentDate { get; set; } = DateTime.UtcNow;

        [DisplayName("Return Date")]
        public DateTime? ReturnDate { get; set; }

        // Location data
        [StringLength(50)]
        [DisplayName("Station Category")]
        public string? StationCategory { get; set; }

        [StringLength(100)]
        [DisplayName("Station")]
        public string? Station { get; set; }

        [DisplayName("Department")]
        public int? DepartmentId { get; set; }

        [StringLength(100)]
        [DisplayName("Specific Location")]
        public string? SpecificLocation { get; set; }

        // Assignment context
        [Required]
        [DisplayName("Assignment Type")]
        public AssignmentType AssignmentType { get; set; } // New, Transfer, Maintenance, Return

        [DisplayName("Requisition")]
        public int? RequisitionId { get; set; }

        [StringLength(20)]
        [DisplayName("Assigned By")]
        public string? AssignedByPayrollNo { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        [ForeignKey("MaterialId")]
        public virtual Material? Material { get; set; }

        [ForeignKey("RequisitionId")]
        public virtual Requisition? Requisition { get; set; }

        public virtual ICollection<MaterialCondition>? MaterialConditions { get; set; }
    }

    public enum AssignmentType
    {
        New,
        Transfer,
        Maintenance,
        Return,
        Disposal
    }
}