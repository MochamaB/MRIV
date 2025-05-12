using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MRIV.Models
{
    [Table("RoleGroups")]
    public class RoleGroup
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Indicates if members can access data across all departments
        /// </summary>
        public bool CanAccessAcrossStations { get; set; } = true;

        /// <summary>
        /// Indicates if members can access data across all stations
        /// </summary>
        public bool CanAccessAcrossDepartments { get; set; } = true;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Navigation property for members
        public virtual ICollection<RoleGroupMember> Members { get; set; } = new List<RoleGroupMember>();
    }
}
