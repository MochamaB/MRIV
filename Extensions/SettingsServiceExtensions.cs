using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MRIV.Enums;
using MRIV.Services;

namespace MRIV.Extensions
{
    /// <summary>
    /// Extension methods for the settings service
    /// </summary>
    public static class SettingsServiceExtensions
    {
        /// <summary>
        /// Adds the settings service to the service collection
        /// </summary>
        public static IServiceCollection AddSettingsService(this IServiceCollection services)
        {
            // Register the settings service
            services.AddScoped<ISettingsService, SettingsService>();
            
            // Ensure memory cache is registered
            services.AddMemoryCache();
            
            return services;
        }

        /// <summary>
        /// Gets a setting value from the settings service using the HTTP context for user settings
        /// </summary>
        public static T GetSetting<T>(this ISettingsService service, HttpContext context, string key, string moduleKey = null)
        {
            string userId = context?.User?.Identity?.Name;
            return service.GetEffectiveSetting<T>(key, userId, moduleKey);
        }

        /// <summary>
        /// Gets a setting value asynchronously from the settings service using the HTTP context for user settings
        /// </summary>
        public static Task<T> GetSettingAsync<T>(this ISettingsService service, HttpContext context, string key, string moduleKey = null)
        {
            string userId = context?.User?.Identity?.Name;
            return service.GetEffectiveSettingAsync<T>(key, userId, moduleKey);
        }
    }
}
