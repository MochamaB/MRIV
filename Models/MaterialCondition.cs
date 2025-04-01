using MRIV.Enums;

namespace MRIV.Models
{
    public class MaterialCondition
    {
        public int Id { get; set; }
        public int? MaterialId { get; set; }
        public int RequisitionId { get; set; }
        public int? RequisitionItemId { get; set; }
        public int? ApprovalId { get; set; }
        public string Stage { get; set; } // "Pre-Dispatch", "Post-Receive", etc.
        public MaterialStatus? Condition { get; set; } // Description of condition
        public string? Notes { get; set; }
        public string? InspectedBy { get; set; }
        public DateTime? InspectionDate { get; set; }
        public string? PhotoUrl { get; set; } // Optional: Store URL to photo evidence

        // Navigation properties
        public Material? Material { get; set; }
        public Requisition? Requisition { get; set; }
        public RequisitionItem? RequisitionItem { get; set; }
        public Approval? Approval { get; set; }
    }
}
