using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
namespace MRIV.Models
{
    public class NotificationTemplate
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } // e.g., "RequisitionCreated"

        [Required]
        public string TitleTemplate { get; set; } // e.g., "New Requisition #{RequisitionId}"

        [Required]
        public string MessageTemplate { get; set; } // e.g., "A new requisition has been created by {Creator}"

        public string? NotificationType { get; set; }
    }
}
