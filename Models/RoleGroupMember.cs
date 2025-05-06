using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MRIV.Models
{
    [Table("RoleGroupMembers")]
    public class RoleGroupMember
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RoleGroupId { get; set; }

        [Required]
        [StringLength(8)]
        public string PayrollNo { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Navigation property for role group
        [ForeignKey("RoleGroupId")]
        public virtual RoleGroup RoleGroup { get; set; }
    }
}
