using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MRIV.Enums;
using MRIV.Models;

namespace MRIV.Services
{
    /// <summary>
    /// Implementation of the settings service that provides access to application settings
    /// with caching support
    /// </summary>
    public class SettingsService : ISettingsService
    {
        private readonly RequisitionContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SettingsService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Cache keys
        private const string CACHE_KEY_ALL_DEFINITIONS = "Settings_AllDefinitions";
        private const string CACHE_KEY_SETTING_PREFIX = "Setting_";
        private const string CACHE_KEY_MODULE_SETTINGS_PREFIX = "ModuleSettings_";
        private const string CACHE_KEY_USER_SETTINGS_PREFIX = "UserSettings_";
        
        // Cache duration
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(30);

        public SettingsService(
            RequisitionContext context,
            IMemoryCache cache,
            ILogger<SettingsService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        #region Basic CRUD Operations

        /// <summary>
        /// Gets a setting value with the specified key and scope
        /// </summary>
        public T GetSetting<T>(string key, SettingScope scope = SettingScope.Global, string scopeId = null)
        {
            // Build cache key
            string cacheKey = BuildCacheKey(key, scope, scopeId);

            // Try to get from cache first
            if (_cache.TryGetValue(cacheKey, out T cachedValue))
            {
                return cachedValue;
            }

            // Get from database
            var setting = GetSettingFromDatabase(key, scope, scopeId);
            if (setting == null)
            {
                _logger.LogWarning("Setting not found: {Key} for scope {Scope} and scopeId {ScopeId}", key, scope, scopeId);
                return default;
            }

            // Convert value based on data type
            T value = SettingTypeConverter.ConvertFromString<T>(
                setting.Value ?? setting.SettingDefinition.DefaultValue,
                setting.SettingDefinition.DataType);

            // Cache the result
            _cache.Set(cacheKey, value, _cacheDuration);

            return value;
        }

        /// <summary>
        /// Gets a setting value asynchronously with the specified key and scope
        /// </summary>
        public async Task<T> GetSettingAsync<T>(string key, SettingScope scope = SettingScope.Global, string scopeId = null)
        {
            // Build cache key
            string cacheKey = BuildCacheKey(key, scope, scopeId);

            // Try to get from cache first
            if (_cache.TryGetValue(cacheKey, out T cachedValue))
            {
                return cachedValue;
            }

            // Get from database
            var setting = await GetSettingFromDatabaseAsync(key, scope, scopeId);
            if (setting == null)
            {
                _logger.LogWarning("Setting not found: {Key} for scope {Scope} and scopeId {ScopeId}", key, scope, scopeId);
                return default;
            }

            // Convert value based on data type
            T value = SettingTypeConverter.ConvertFromString<T>(
                setting.Value ?? setting.SettingDefinition.DefaultValue,
                setting.SettingDefinition.DataType);

            // Cache the result
            _cache.Set(cacheKey, value, _cacheDuration);

            return value;
        }

        /// <summary>
        /// Updates a setting value with the specified key and scope
        /// </summary>
        public bool UpdateSetting<T>(string key, T value, SettingScope scope = SettingScope.Global, string scopeId = null)
        {
            try
            {
                // Get setting definition
                var definition = _context.SettingDefinitions
                    .FirstOrDefault(sd => sd.Key == key);

                if (definition == null)
                {
                    _logger.LogError("Setting definition not found: {Key}", key);
                    return false;
                }

                // Validate value against data type
                if (!SettingTypeConverter.IsValidForDataType(value, definition.DataType))
                {
                    _logger.LogError("Invalid value type for setting {Key}: {Value} is not compatible with {DataType}",
                        key, value, definition.DataType);
                    return false;
                }

                // Convert value to string
                string stringValue = SettingTypeConverter.ConvertToString(value, definition.DataType);

                // Get existing setting value or create new one
                var settingValue = _context.SettingValues
                    .FirstOrDefault(sv => sv.SettingDefinition.Key == key && sv.Scope == scope && sv.ScopeId == scopeId);

                if (settingValue == null)
                {
                    // Create new setting value
                    settingValue = new SettingValue
                    {
                        SettingDefinitionId = definition.Id,
                        Value = stringValue,
                        Scope = scope,
                        ScopeId = scopeId,
                        CreatedBy = GetCurrentUser()
                    };
                    _context.SettingValues.Add(settingValue);
                }
                else
                {
                    // Update existing setting value
                    settingValue.Value = stringValue;
                    settingValue.UpdatedAt = DateTime.UtcNow;
                }

                // Save changes
                _context.SaveChanges();

                // Invalidate cache
                InvalidateCache(key, scope, scopeId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating setting {Key} for scope {Scope} and scopeId {ScopeId}",
                    key, scope, scopeId);
                return false;
            }
        }

        /// <summary>
        /// Updates a setting value asynchronously with the specified key and scope
        /// </summary>
        public async Task<bool> UpdateSettingAsync<T>(string key, T value, SettingScope scope = SettingScope.Global, string scopeId = null)
        {
            try
            {
                // Get setting definition
                var definition = await _context.SettingDefinitions
                    .FirstOrDefaultAsync(sd => sd.Key == key);

                if (definition == null)
                {
                    _logger.LogError("Setting definition not found: {Key}", key);
                    return false;
                }

                // Validate value against data type
                if (!SettingTypeConverter.IsValidForDataType(value, definition.DataType))
                {
                    _logger.LogError("Invalid value type for setting {Key}: {Value} is not compatible with {DataType}",
                        key, value, definition.DataType);
                    return false;
                }

                // Convert value to string
                string stringValue = SettingTypeConverter.ConvertToString(value, definition.DataType);

                // Get existing setting value or create new one
                var settingValue = await _context.SettingValues
                    .FirstOrDefaultAsync(sv => sv.SettingDefinition.Key == key && sv.Scope == scope && sv.ScopeId == scopeId);

                if (settingValue == null)
                {
                    // Create new setting value
                    settingValue = new SettingValue
                    {
                        SettingDefinitionId = definition.Id,
                        Value = stringValue,
                        Scope = scope,
                        ScopeId = scopeId,
                        CreatedBy = GetCurrentUser()
                    };
                    _context.SettingValues.Add(settingValue);
                }
                else
                {
                    // Update existing setting value
                    settingValue.Value = stringValue;
                    settingValue.UpdatedAt = DateTime.UtcNow;
                }

                // Save changes
                await _context.SaveChangesAsync();

                // Invalidate cache
                await InvalidateCacheAsync(key, scope, scopeId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating setting {Key} for scope {Scope} and scopeId {ScopeId}",
                    key, scope, scopeId);
                return false;
            }
        }

        #endregion

        #region Convenience Methods

        /// <summary>
        /// Gets a global setting value
        /// </summary>
        public T GetGlobalSetting<T>(string key)
        {
            return GetSetting<T>(key, SettingScope.Global, null);
        }

        /// <summary>
        /// Gets a global setting value asynchronously
        /// </summary>
        public Task<T> GetGlobalSettingAsync<T>(string key)
        {
            return GetSettingAsync<T>(key, SettingScope.Global, null);
        }

        /// <summary>
        /// Gets a module setting value
        /// </summary>
        public T GetModuleSetting<T>(string moduleKey, string settingKey)
        {
            return GetSetting<T>(settingKey, SettingScope.Module, moduleKey);
        }

        /// <summary>
        /// Gets a module setting value asynchronously
        /// </summary>
        public Task<T> GetModuleSettingAsync<T>(string moduleKey, string settingKey)
        {
            return GetSettingAsync<T>(settingKey, SettingScope.Module, moduleKey);
        }

        /// <summary>
        /// Gets a user setting value
        /// </summary>
        public T GetUserSetting<T>(string userId, string settingKey)
        {
            return GetSetting<T>(settingKey, SettingScope.User, userId);
        }

        /// <summary>
        /// Gets a user setting value asynchronously
        /// </summary>
        public Task<T> GetUserSettingAsync<T>(string userId, string settingKey)
        {
            return GetSettingAsync<T>(settingKey, SettingScope.User, userId);
        }

        #endregion

        #region Hierarchical Resolution

        /// <summary>
        /// Gets the effective setting value by checking user, module, and global scopes in order
        /// </summary>
        public T GetEffectiveSetting<T>(string key, string userId = null, string moduleKey = null)
        {
            // Try user setting first
            if (!string.IsNullOrEmpty(userId))
            {
                var userValue = GetSetting<T>(key, SettingScope.User, userId);
                if (!EqualityComparer<T>.Default.Equals(userValue, default))
                {
                    return userValue;
                }
            }

            // Try module setting next
            if (!string.IsNullOrEmpty(moduleKey))
            {
                var moduleValue = GetSetting<T>(key, SettingScope.Module, moduleKey);
                if (!EqualityComparer<T>.Default.Equals(moduleValue, default))
                {
                    return moduleValue;
                }
            }

            // Fall back to global setting
            return GetSetting<T>(key, SettingScope.Global, null);
        }

        /// <summary>
        /// Gets the effective setting value asynchronously by checking user, module, and global scopes in order
        /// </summary>
        public async Task<T> GetEffectiveSettingAsync<T>(string key, string userId = null, string moduleKey = null)
        {
            // Try user setting first
            if (!string.IsNullOrEmpty(userId))
            {
                var userValue = await GetSettingAsync<T>(key, SettingScope.User, userId);
                if (!EqualityComparer<T>.Default.Equals(userValue, default))
                {
                    return userValue;
                }
            }

            // Try module setting next
            if (!string.IsNullOrEmpty(moduleKey))
            {
                var moduleValue = await GetSettingAsync<T>(key, SettingScope.Module, moduleKey);
                if (!EqualityComparer<T>.Default.Equals(moduleValue, default))
                {
                    return moduleValue;
                }
            }

            // Fall back to global setting
            return await GetSettingAsync<T>(key, SettingScope.Global, null);
        }

        #endregion

        #region Admin Operations

        /// <summary>
        /// Gets all setting definitions
        /// </summary>
        public IEnumerable<SettingDefinition> GetAllSettingDefinitions()
        {
            // Try to get from cache first
            if (!_cache.TryGetValue(CACHE_KEY_ALL_DEFINITIONS, out IEnumerable<SettingDefinition> definitions))
            {
                // Get from database
                definitions = _context.SettingDefinitions
                    .Include(sd => sd.Group)
                    .OrderBy(sd => sd.Group.DisplayOrder)
                    .ThenBy(sd => sd.DisplayOrder)
                    .ToList();

                // Cache the result
                _cache.Set(CACHE_KEY_ALL_DEFINITIONS, definitions, _cacheDuration);
            }

            return definitions;
        }

        /// <summary>
        /// Gets all setting definitions asynchronously
        /// </summary>
        public async Task<IEnumerable<SettingDefinition>> GetAllSettingDefinitionsAsync()
        {
            // Try to get from cache first
            if (!_cache.TryGetValue(CACHE_KEY_ALL_DEFINITIONS, out IEnumerable<SettingDefinition> definitions))
            {
                // Get from database
                definitions = await _context.SettingDefinitions
                    .Include(sd => sd.Group)
                    .OrderBy(sd => sd.Group.DisplayOrder)
                    .ThenBy(sd => sd.DisplayOrder)
                    .ToListAsync();

                // Cache the result
                _cache.Set(CACHE_KEY_ALL_DEFINITIONS, definitions, _cacheDuration);
            }

            return definitions;
        }

        /// <summary>
        /// Gets setting definitions for a specific module
        /// </summary>
        public IEnumerable<SettingDefinition> GetModuleSettingDefinitions(string moduleKey)
        {
            string cacheKey = $"{CACHE_KEY_MODULE_SETTINGS_PREFIX}{moduleKey}";

            // Try to get from cache first
            if (!_cache.TryGetValue(cacheKey, out IEnumerable<SettingDefinition> definitions))
            {
                // Get from database
                definitions = _context.SettingDefinitions
                    .Include(sd => sd.Group)
                    .Where(sd => sd.ModuleName == moduleKey)
                    .OrderBy(sd => sd.Group.DisplayOrder)
                    .ThenBy(sd => sd.DisplayOrder)
                    .ToList();

                // Cache the result
                _cache.Set(cacheKey, definitions, _cacheDuration);
            }

            return definitions;
        }

        /// <summary>
        /// Gets setting definitions for a specific module asynchronously
        /// </summary>
        public async Task<IEnumerable<SettingDefinition>> GetModuleSettingDefinitionsAsync(string moduleKey)
        {
            string cacheKey = $"{CACHE_KEY_MODULE_SETTINGS_PREFIX}{moduleKey}";

            // Try to get from cache first
            if (!_cache.TryGetValue(cacheKey, out IEnumerable<SettingDefinition> definitions))
            {
                // Get from database
                definitions = await _context.SettingDefinitions
                    .Include(sd => sd.Group)
                    .Where(sd => sd.ModuleName == moduleKey)
                    .OrderBy(sd => sd.Group.DisplayOrder)
                    .ThenBy(sd => sd.DisplayOrder)
                    .ToListAsync();

                // Cache the result
                _cache.Set(cacheKey, definitions, _cacheDuration);
            }

            return definitions;
        }

        /// <summary>
        /// Gets setting definitions that users can configure for themselves
        /// </summary>
        public IEnumerable<SettingDefinition> GetUserConfigurableSettings()
        {
            string cacheKey = "UserConfigurableSettings";

            // Try to get from cache first
            if (!_cache.TryGetValue(cacheKey, out IEnumerable<SettingDefinition> definitions))
            {
                // Get from database
                definitions = _context.SettingDefinitions
                    .Include(sd => sd.Group)
                    .Where(sd => sd.IsUserConfigurable)
                    .OrderBy(sd => sd.Group.DisplayOrder)
                    .ThenBy(sd => sd.DisplayOrder)
                    .ToList();

                // Cache the result
                _cache.Set(cacheKey, definitions, _cacheDuration);
            }

            return definitions;
        }

        /// <summary>
        /// Gets setting definitions that users can configure for themselves asynchronously
        /// </summary>
        public async Task<IEnumerable<SettingDefinition>> GetUserConfigurableSettingsAsync()
        {
            string cacheKey = "UserConfigurableSettings";

            // Try to get from cache first
            if (!_cache.TryGetValue(cacheKey, out IEnumerable<SettingDefinition> definitions))
            {
                // Get from database
                definitions = await _context.SettingDefinitions
                    .Include(sd => sd.Group)
                    .Where(sd => sd.IsUserConfigurable)
                    .OrderBy(sd => sd.Group.DisplayOrder)
                    .ThenBy(sd => sd.DisplayOrder)
                    .ToListAsync();

                // Cache the result
                _cache.Set(cacheKey, definitions, _cacheDuration);
            }

            return definitions;
        }

        #endregion

        #region Cache Management

        /// <summary>
        /// Invalidates the cache for a specific setting or all settings
        /// </summary>
        public void InvalidateCache(string key = null, SettingScope scope = SettingScope.Global, string scopeId = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                // Invalidate all settings
                _cache.Remove(CACHE_KEY_ALL_DEFINITIONS);
                // We could also iterate through all cached keys and remove them, but that's more complex
            }
            else
            {
                // Invalidate specific setting
                string cacheKey = BuildCacheKey(key, scope, scopeId);
                _cache.Remove(cacheKey);

                // Also invalidate the all definitions cache
                _cache.Remove(CACHE_KEY_ALL_DEFINITIONS);

                // If it's a module setting, invalidate the module settings cache
                if (scope == SettingScope.Module && !string.IsNullOrEmpty(scopeId))
                {
                    _cache.Remove($"{CACHE_KEY_MODULE_SETTINGS_PREFIX}{scopeId}");
                }
                // If it's a user setting, invalidate the user settings cache
                else if (scope == SettingScope.User && !string.IsNullOrEmpty(scopeId))
                {
                    _cache.Remove($"{CACHE_KEY_USER_SETTINGS_PREFIX}{scopeId}");
                }
            }
        }

        /// <summary>
        /// Invalidates the cache for a specific setting or all settings asynchronously
        /// </summary>
        public Task InvalidateCacheAsync(string key = null, SettingScope scope = SettingScope.Global, string scopeId = null)
        {
            // Cache operations are synchronous, so just call the synchronous method
            InvalidateCache(key, scope, scopeId);
            return Task.CompletedTask;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets a setting value from the database
        /// </summary>
        private SettingValue GetSettingFromDatabase(string key, SettingScope scope, string scopeId)
        {
            // First try to get the setting value for the specific scope
            var settingValue = _context.SettingValues
                .Include(sv => sv.SettingDefinition)
                .FirstOrDefault(sv => sv.SettingDefinition.Key == key && sv.Scope == scope && sv.ScopeId == scopeId);

            // If not found and we're looking for a specific scope, return null
            if (settingValue == null && (scope != SettingScope.Global || !string.IsNullOrEmpty(scopeId)))
            {
                // Try to get the setting definition to return default value
                var definition = _context.SettingDefinitions
                    .FirstOrDefault(sd => sd.Key == key);

                if (definition != null)
                {
                    // Create a temporary setting value with the default value
                    settingValue = new SettingValue
                    {
                        SettingDefinition = definition,
                        Value = null // Use default value from definition
                    };
                }
            }

            return settingValue;
        }

        /// <summary>
        /// Gets a setting value from the database asynchronously
        /// </summary>
        private async Task<SettingValue> GetSettingFromDatabaseAsync(string key, SettingScope scope, string scopeId)
        {
            // First try to get the setting value for the specific scope
            var settingValue = await _context.SettingValues
                .Include(sv => sv.SettingDefinition)
                .FirstOrDefaultAsync(sv => sv.SettingDefinition.Key == key && sv.Scope == scope && sv.ScopeId == scopeId);

            // If not found and we're looking for a specific scope, return null
            if (settingValue == null && (scope != SettingScope.Global || !string.IsNullOrEmpty(scopeId)))
            {
                // Try to get the setting definition to return default value
                var definition = await _context.SettingDefinitions
                    .FirstOrDefaultAsync(sd => sd.Key == key);

                if (definition != null)
                {
                    // Create a temporary setting value with the default value
                    settingValue = new SettingValue
                    {
                        SettingDefinition = definition,
                        Value = null // Use default value from definition
                    };
                }
            }

            return settingValue;
        }

        /// <summary>
        /// Builds a cache key for a setting
        /// </summary>
        private string BuildCacheKey(string key, SettingScope scope, string scopeId)
        {
            return $"{CACHE_KEY_SETTING_PREFIX}{key}_{scope}_{scopeId ?? "null"}";
        }

        /// <summary>
        /// Gets the current user ID from the HTTP context
        /// </summary>
        private string GetCurrentUser()
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";
        }

        #endregion
    }
}
