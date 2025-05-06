using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MRIV.Models
{
    /// <summary>
    /// Represents a logical grouping of settings for organization purposes
    /// </summary>
    public class SettingGroup
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        [StringLength(500)]
        public string Description { get; set; }
        
        /// <summary>
        /// Controls the order in which groups are displayed in the UI
        /// </summary>
        public int DisplayOrder { get; set; }
        
        /// <summary>
        /// Optional parent group ID for hierarchical grouping
        /// </summary>
        public int? ParentGroupId { get; set; }
        
        [ForeignKey("ParentGroupId")]
        public virtual SettingGroup ParentGroup { get; set; }
        
        /// <summary>
        /// Child groups under this group
        /// </summary>
        public virtual ICollection<SettingGroup> ChildGroups { get; set; }
        
        /// <summary>
        /// Settings that belong to this group
        /// </summary>
        public virtual ICollection<SettingDefinition> SettingDefinitions { get; set; }
        
        public SettingGroup()
        {
            ChildGroups = new HashSet<SettingGroup>();
            SettingDefinitions = new HashSet<SettingDefinition>();
        }
    }
}
