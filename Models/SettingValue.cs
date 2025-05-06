using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MRIV.Enums;

namespace MRIV.Models
{
    /// <summary>
    /// Represents the actual value of a setting for a specific scope
    /// </summary>
    public class SettingValue
    {
        [Key]
        public int Id { get; set; }
        
        /// <summary>
        /// Reference to the setting definition
        /// </summary>
        public int SettingDefinitionId { get; set; }
        
        [ForeignKey("SettingDefinitionId")]
        public virtual SettingDefinition SettingDefinition { get; set; }
        
        /// <summary>
        /// The value of the setting (stored as string, will be converted based on SettingDefinition.DataType)
        /// </summary>
        public string Value { get; set; }
        
        /// <summary>
        /// The scope at which this setting value applies
        /// </summary>
        public SettingScope Scope { get; set; }
        
        /// <summary>
        /// The ID of the scope entity (null for global, ModuleId for module, UserId for user)
        /// </summary>
        [StringLength(100)]
        public string ScopeId { get; set; }
        
        /// <summary>
        /// When the setting value was created
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// When the setting value was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; }
        
        /// <summary>
        /// User who created this setting value
        /// </summary>
        [StringLength(100)]
        public string CreatedBy { get; set; }
        
        public SettingValue()
        {
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
