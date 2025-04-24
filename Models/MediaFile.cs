using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MRIV.Models
{
    public class MediaFile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; }

        [Required]
        [StringLength(100)]
        public string MimeType { get; set; }

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; }

        [StringLength(255)]
        public string? Collection { get; set; } // e.g., "images", "documents", "avatars"

        [Required]
        public string ModelType { get; set; } // The type of the model this belongs to

        [Required]
        public int ModelId { get; set; } // The ID of the model this belongs to

        public long FileSize { get; set; } // In bytes

        [StringLength(500)]
        public string? Alt { get; set; } // Alternative text for images

        [StringLength(500)]
        public string? CustomProperties { get; set; } // JSON string for additional properties

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // You can add methods to generate URLs or thumbnails here
        public string GetUrl()
        {
            // Implementation depends on your storage strategy
            return $"/media/{FilePath}";
        }
    }
}