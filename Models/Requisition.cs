using MRIV.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MRIV.Models
{
    public class Requisition
    {
        [Key]
        public int Id { get; set; }

        public int TicketId { get; set; }

        [Required]
        public int DepartmentId { get; set; }

        [Required]
        [StringLength(20)]
        public string PayrollNo { get; set; }

        [Required]
        [StringLength(50)]
        public string IssueStationCategory { get; set; }

        [Required]
        public string IssueStation { get; set; }

        [Required]
        [StringLength(50)]
        public string DeliveryStationCategory { get; set; }

        [Required]
        public string DeliveryStation { get; set; }

        [StringLength(500)]
        public string Remarks { get; set; }

        [StringLength(20)]
        public string DispatchType { get; set; }

        [StringLength(20)]
        public string DispatchPayrollNo { get; set; }

        [StringLength(50)]
        public string DispatchVendor { get; set; }

        [StringLength(100)]
        public string CollectorName { get; set; }

        [StringLength(50)]
        public string CollectorId { get; set; }

        [Required]
        public RequisitionStatus Status { get; set; } // Enum Property

        public DateTime? CompleteDate { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool IsExternal { get; set; }

        public bool ForwardToAdmin { get; set; }

        // Add navigation property to Department
        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }

        // Navigation properties
        public virtual ICollection<RequisitionItem> RequisitionItems { get; set; }
        public virtual ICollection<Approval>? Approvals { get; set; }
    }
}
