using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MRIV.Models
{
    public class Approval
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RequisitionId { get; set; }

        public int DepartmentId { get; set; }

        [Required]
        [StringLength(50)]
        public string ApprovalStep { get; set; }

        [Required]
        public int PayrollNo { get; set; }

        [Required]
        [StringLength(20)]
        public string ApprovalStatus { get; set; }

        [StringLength(500)]
        public string? Comments { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        // Add navigation property to Department
        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }

        // Navigation property
        // Explicitly map the foreign key for Requisition
        [ForeignKey("RequisitionId")]
        public virtual Requisition? Requisition { get; set; }
    }
}
