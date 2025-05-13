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

        public int MaterialId { get; set; }

      
        [StringLength(20)]
        [DisplayName("Assigned To")]
        public string? PayrollNo { get; set; }

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
        public int? StationId { get; set; }

        [DisplayName("Department")]
        public int? DepartmentId { get; set; }

        [StringLength(100)]
        [DisplayName("Specific Location")]
        public string? SpecificLocation { get; set; }

        // Assignment context
        [Required]
        [DisplayName("Assignment Type")]
        public RequisitionType AssignmentType { get; set; } // New, Transfer, Maintenance, Return

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
        [Description("New Purchase")]
        NewPurchase = 1,

        [Description("Transfer")]
        Transfer = 2,

        [Description("InterFactory")]
        InterFactory = 3,

        [Description("Maintenance")]
        Maintenance = 4,

        [Description("Return")]
        Return = 5,

        [Description("Disposal")]
        Disposal = 6,

        [Description("Loan/Borrow")]
        Loan = 7,

        [Description("Temporary Allocation")]
        TemporaryAllocation = 8,

        [Description("Other")]
        Other = 9
    }
}