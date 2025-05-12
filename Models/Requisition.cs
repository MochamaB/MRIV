using MRIV.Attributes;
using MRIV.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MRIV.Models.Interfaces;

namespace MRIV.Models
{
    public class Requisition : IHasMedia
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
        [Display(Name = "Requisition Type")]
        public RequisitionType RequisitionType { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Issue Point Category")]
        public string IssueStationCategory { get; set; }

        
        public int IssueStationId  { get; set; }

        public string? IssueDepartmentId { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Delivery point category")]
        public string DeliveryStationCategory { get; set; }

        public int DeliveryStationId { get; set; }

        public string? DeliveryDepartmentId { get; set; }

        [StringLength(500)]
        public string Remarks { get; set; }

        [StringLength(20)]
        [RequiredIfSectionVisible]
        [Display(Name = "Dispatched By")]
        public string? DispatchType { get; set; }

        [RequiredIf(nameof(DispatchType), "admin")]
        [StringLength(20)]
        [Display(Name = "Dispatching Employee")]
        public string? DispatchPayrollNo { get; set; }

        [RequiredIf(nameof(DispatchType), "vendor")]
        [StringLength(50)]
        [Display(Name = "Dispatching Vendor")]
        public string? DispatchVendor { get; set; }

        [StringLength(100)]
        public string? CollectorName { get; set; }

        [StringLength(50)]
        public string? CollectorId { get; set; }

        public RequisitionStatus? Status { get; set; } // Enum Property

        public DateTime? CompleteDate { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        // Add navigation property to Department
        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }

        // Navigation properties
        public virtual ICollection<RequisitionItem>? RequisitionItems { get; set; }
        public virtual ICollection<Approval>? Approvals { get; set; }

        // Add this property to implement the interface
        [NotMapped] // If you don't want this in the database table
        public virtual ICollection<MediaFile> Media { get; set; } = new List<MediaFile>();
        public int? MaterialAssignmentId { get; set; }
        [ForeignKey("MaterialAssignmentId")]
        public virtual MaterialAssignment? MaterialAssignment { get; set; }
    }
}
