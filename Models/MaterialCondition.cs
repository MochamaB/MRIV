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
        public Condition? Condition { get; set; }

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

    public enum Condition
    {
        [Description("Good Condition")]
        GoodCondition = 1,

        [Description("Minor Damage")]
        MinorDamage = 2,

        [Description("Major Damage")]
        MajorDamage = 3,

        [Description("Faulty")]
        Faulty = 4,

        [Description("Broken")]
        UnderMaintenance = 5,

        [Description("Lost or Stolen")]
        LostOrStolen = 6,

        [Description("Disposed")]
        Disposed = 7,
    }

    public enum ConditionCheckType
    {
        [Description("Initial")]
        Initial = 0,

        [Description("Requisition Transfer")]
        Transfer = 1,

        [Description("At Dispatch")]
        AtDispatch = 2,

        [Description("At Receipt")]
        AtReceipt = 3,

        [Description("Periodic")]
        Periodic = 4,

        [Description("At Disposal")]
        AtDisposal = 5
    }

    public enum FunctionalStatus
    {
        [Description("Fully Functional")]
        FullyFunctional = 0,

        [Description("Partially Functional")]
        PartiallyFunctional = 1,

        [Description("Non Functional")]
        NonFunctional = 2
    }

    public enum CosmeticStatus
    {
        [Description("Excellent")]
        Excellent = 0,

        [Description("Good")]
        Good = 1,

        [Description("Fair")]
        Fair = 2,

        [Description("Poor")]
        Poor = 3
    }
}