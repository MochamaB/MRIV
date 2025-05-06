using MRIV.Enums;
using MRIV.Models;
using System.ComponentModel.DataAnnotations;

namespace MRIV.ViewModels
{
    /// <summary>
    /// View model for displaying and editing settings
    /// </summary>
    public class SettingsViewModel
    {
        /// <summary>
        /// List of setting groups with their settings
        /// </summary>
        public List<SettingGroupViewModel> Groups { get; set; } = new List<SettingGroupViewModel>();
        
        /// <summary>
        /// Current scope of the settings being viewed/edited
        /// </summary>
        public SettingScope Scope { get; set; }
        
        /// <summary>
        /// ID of the scope entity (null for global, module name for module, user ID for user)
        /// </summary>
        public string ScopeId { get; set; }
        
        /// <summary>
        /// Display name of the current scope
        /// </summary>
        public string ScopeDisplayName { get; set; }
        
        /// <summary>
        /// List of available modules for navigation
        /// </summary>
        public List<string> AvailableModules { get; set; } = new List<string>();
        
        /// <summary>
        /// List of available users for navigation
        /// </summary>
        public List<UserViewModel> AvailableUsers { get; set; } = new List<UserViewModel>();
    }
    
    /// <summary>
    /// View model for a setting group
    /// </summary>
    public class SettingGroupViewModel
    {
        /// <summary>
        /// Group ID
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// Group name
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Group description
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Settings in this group
        /// </summary>
        public List<SettingItemViewModel> Settings { get; set; } = new List<SettingItemViewModel>();
    }
    
    /// <summary>
    /// View model for a single setting
    /// </summary>
    public class SettingItemViewModel
    {
        /// <summary>
        /// Setting definition ID
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// Setting key
        /// </summary>
        public string Key { get; set; }
        
        /// <summary>
        /// Display name
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Data type
        /// </summary>
        public SettingDataType DataType { get; set; }
        
        /// <summary>
        /// Current value
        /// </summary>
        public string Value { get; set; }
        
        /// <summary>
        /// Default value
        /// </summary>
        public string DefaultValue { get; set; }
        
        /// <summary>
        /// Whether this setting can be configured by users
        /// </summary>
        public bool IsUserConfigurable { get; set; }
        
        /// <summary>
        /// Module name if this is a module setting
        /// </summary>
        public string ModuleName { get; set; }
        
        /// <summary>
        /// Whether this setting has a custom value for the current scope
        /// </summary>
        public bool HasCustomValue { get; set; }
        
        /// <summary>
        /// Validation rules for the setting
        /// </summary>
        public string ValidationRules { get; set; }
    }
    
    /// <summary>
    /// View model for updating a setting
    /// </summary>
    public class UpdateSettingViewModel
    {
        /// <summary>
        /// Setting key
        /// </summary>
        [Required]
        public string Key { get; set; }
        
        /// <summary>
        /// New value
        /// </summary>
        public string Value { get; set; }
        
        /// <summary>
        /// Scope of the setting
        /// </summary>
        public SettingScope Scope { get; set; }
        
        /// <summary>
        /// ID of the scope entity
        /// </summary>
        public string ScopeId { get; set; }
        
        /// <summary>
        /// Whether to reset to default value
        /// </summary>
        public bool ResetToDefault { get; set; }
    }
    
    /// <summary>
    /// Simple view model for user selection
    /// </summary>
    public class UserViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}
