using MRIV.Enums;
using MRIV.Models;

namespace MRIV.Services
{
    /// <summary>
    /// Interface for the settings service that provides access to application settings
    /// </summary>
    public interface ISettingsService
    {
        // Basic CRUD operations
        T GetSetting<T>(string key, SettingScope scope = SettingScope.Global, string scopeId = null);
        Task<T> GetSettingAsync<T>(string key, SettingScope scope = SettingScope.Global, string scopeId = null);
        bool UpdateSetting<T>(string key, T value, SettingScope scope = SettingScope.Global, string scopeId = null);
        Task<bool> UpdateSettingAsync<T>(string key, T value, SettingScope scope = SettingScope.Global, string scopeId = null);
        
        // Convenience methods for different scopes
        T GetGlobalSetting<T>(string key);
        Task<T> GetGlobalSettingAsync<T>(string key);
        T GetModuleSetting<T>(string moduleKey, string settingKey);
        Task<T> GetModuleSettingAsync<T>(string moduleKey, string settingKey);
        T GetUserSetting<T>(string userId, string settingKey);
        Task<T> GetUserSettingAsync<T>(string userId, string settingKey);
        
        // Hierarchical resolution (user -> module -> global)
        T GetEffectiveSetting<T>(string key, string userId = null, string moduleKey = null);
        Task<T> GetEffectiveSettingAsync<T>(string key, string userId = null, string moduleKey = null);
        
        // Admin operations
        IEnumerable<SettingDefinition> GetAllSettingDefinitions();
        Task<IEnumerable<SettingDefinition>> GetAllSettingDefinitionsAsync();
        IEnumerable<SettingDefinition> GetModuleSettingDefinitions(string moduleKey);
        Task<IEnumerable<SettingDefinition>> GetModuleSettingDefinitionsAsync(string moduleKey);
        IEnumerable<SettingDefinition> GetUserConfigurableSettings();
        Task<IEnumerable<SettingDefinition>> GetUserConfigurableSettingsAsync();
        
        // Cache management
        void InvalidateCache(string key = null, SettingScope scope = SettingScope.Global, string scopeId = null);
        Task InvalidateCacheAsync(string key = null, SettingScope scope = SettingScope.Global, string scopeId = null);
    }
}
