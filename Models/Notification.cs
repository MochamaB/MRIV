using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;


namespace MRIV.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Message { get; set; }

        [Required]
        public string RecipientId { get; set; } // User's PayrollNo

        public string? URL { get; set; } // Optional link to relevant page

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ReadAt { get; set; }

        public bool IsRead => ReadAt.HasValue;

        public string NotificationType { get; set; } // e.g., "Approval", "Dispatch", etc.

        public int? EntityId { get; set; } // ID of related entity (requisition, approval, etc.)

        public string? EntityType { get; set; } // Type of related entity
    }
}
