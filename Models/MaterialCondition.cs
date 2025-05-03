using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;
using MRIV.Enums;
using MRIV.Models.Interfaces;

namespace MRIV.Models
{
    public class MaterialCondition : IHasMedia
    {
        [Key]
        public int Id { get; set; }

        public int? MaterialId { get; set; }

        public int? MaterialAssignmentId { get; set; }

        public int? RequisitionId { get; set; }

        public int? RequisitionItemId { get; set; }

        public int? ApprovalId { get; set; }

        [DisplayName("Condition Check Type")]
        public ConditionCheckType? ConditionCheckType { get; set; }

        [DisplayName("Stage")]
        public string? Stage { get; set; } // "Pre-Dispatch", "Post-Receive", etc.

        [DisplayName("Condition")]
        public MaterialStatus? Condition { get; set; }

        [DisplayName("Functional Status")]
        public FunctionalStatus? FunctionalStatus { get; set; }

        [DisplayName("Cosmetic Status")]
        public CosmeticStatus? CosmeticStatus { get; set; }

        [StringLength(1000)]
        [DisplayName("Component Statuses")]
        public string? ComponentStatuses { get; set; } // JSON string

        [StringLength(500)]
        public string? Notes { get; set; }

        [StringLength(20)]
        [DisplayName("Inspected By")]
        public string? InspectedBy { get; set; }

        [DisplayName("Inspection Date")]
        public DateTime? InspectionDate { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        [DisplayName("Action Required")]
        public string? ActionRequired { get; set; }

        [DisplayName("Action Due Date")]
        public DateTime? ActionDueDate { get; set; }

        // Navigation properties
        [ForeignKey("MaterialId")]
        public Material? Material { get; set; }

        [ForeignKey("MaterialAssignmentId")]
        public MaterialAssignment? MaterialAssignment { get; set; }

        [ForeignKey("RequisitionId")]
        public Requisition? Requisition { get; set; }

        [ForeignKey("RequisitionItemId")]
        public RequisitionItem? RequisitionItem { get; set; }

        [ForeignKey("ApprovalId")]
        public Approval? Approval { get; set; }
        // Add this property to implement the interface
        [NotMapped] // If you don't want this in the database table
        public virtual ICollection<MediaFile> Media { get; set; } = new List<MediaFile>();
    }

    public enum ConditionCheckType
    {
        Initial,
        Assignment,
        Return,
        Periodic,
        Damage,
        Disposal
    }

    public enum FunctionalStatus
    {
        FullyFunctional,
        PartiallyFunctional,
        NonFunctional
    }

    public enum CosmeticStatus
    {
        Excellent,
        Good,
        Fair,
        Poor
    }
}