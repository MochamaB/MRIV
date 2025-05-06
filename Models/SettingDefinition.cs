using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MRIV.Enums;

namespace MRIV.Models
{
    /// <summary>
    /// Defines a setting that can be configured in the system
    /// </summary>
    public class SettingDefinition
    {
        [Key]
        public int Id { get; set; }
        
        /// <summary>
        /// Unique key for the setting
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Key { get; set; }
        
        /// <summary>
        /// Display name for the setting
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        /// <summary>
        /// Description of the setting's purpose
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; }
        
        /// <summary>
        /// Data type of the setting
        /// </summary>
        public SettingDataType DataType { get; set; }
        
        /// <summary>
        /// Default value for the setting (stored as string, will be converted based on DataType)
        /// </summary>
        public string DefaultValue { get; set; }
        
        /// <summary>
        /// Whether users can configure this setting for themselves
        /// </summary>
        public bool IsUserConfigurable { get; set; }
        
        /// <summary>
        /// Module name if this setting is specific to a module, null for global settings
        /// </summary>
        [StringLength(100)]
        public string? ModuleName { get; set; }
        
        /// <summary>
        /// Group this setting belongs to
        /// </summary>
        public int GroupId { get; set; }
        
        [ForeignKey("GroupId")]
        public virtual SettingGroup Group { get; set; }
        
        /// <summary>
        /// Validation rules for the setting (JSON schema or regex)
        /// </summary>
        public string? ValidationRules { get; set; }
        
        /// <summary>
        /// Controls the order in which settings are displayed in the UI
        /// </summary>
        public int DisplayOrder { get; set; }
        
        /// <summary>
        /// When the setting was created
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// When the setting was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; }
        
        /// <summary>
        /// Values for this setting definition
        /// </summary>
        public virtual ICollection<SettingValue> SettingValues { get; set; }
        
        public SettingDefinition()
        {
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            SettingValues = new HashSet<SettingValue>();
        }
    }
}
