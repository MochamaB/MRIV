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


        public int MaterialId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        [StringLength(20)]
        public string Condition { get; set; } // OK, Broken, etc.

        [Required]
        [StringLength(20)]
        public RequisitionItemStatus Status { get; set; } //Pending Approval,Dispatched, Received, Returned

        [Required]
        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("RequisitionId")]
        public virtual Requisition Requisition { get; set; }

        
    }
}
