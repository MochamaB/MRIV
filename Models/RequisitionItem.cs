using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using MRIV.Enums;

namespace MRIV.Models
{
    public class RequisitionItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RequisitionId { get; set; }


        public int? MaterialId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [StringLength(20)]
        public string? Name { get; set; }

        [StringLength(70)]
        public string? Description { get; set; }


        public RequisitionItemCondition Condition { get; set; } // OK, Broken, etc.

        
        public RequisitionItemStatus Status { get; set; } //Pending Approval,Dispatched, Received, Returned


        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("RequisitionId")]
        public virtual Requisition? Requisition { get; set; }

        
    }
}
