using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using MRIV.Extensions;
using MRIV.Models;

namespace MRIV.Services
{
    public class UserProfileCacheService : IUserProfileCacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<UserProfileCacheService> _logger;

        public UserProfileCacheService(
            IMemoryCache memoryCache,
            IHttpContextAccessor httpContextAccessor,
            ILogger<UserProfileCacheService> logger)
        {
            _memoryCache = memoryCache;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task SetProfileAsync(UserProfile profile)
        {
            if (profile == null) return;

            var cacheKey = GetCacheKey(profile.BasicInfo.PayrollNo);
            
            try
            {
                // Store in memory cache with TTL
                var memoryCacheOptions = new MemoryCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(30),
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2),
                    Priority = CacheItemPriority.High
                };

                _memoryCache.Set(cacheKey, profile, memoryCacheOptions);

                // Store in session if available
                var session = _httpContextAccessor.HttpContext?.Session;
                if (session != null)
                {
                    var profileJson = JsonSerializer.Serialize(profile);
                    session.SetString($"UserProfile_{profile.BasicInfo.PayrollNo}", profileJson);
                }

                _logger.LogDebug("Cached user profile for PayrollNo: {PayrollNo}", profile.BasicInfo.PayrollNo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error caching user profile for PayrollNo: {PayrollNo}", profile.BasicInfo.PayrollNo);
                throw;
            }
        }

        public async Task<UserProfile> GetProfileAsync(string payrollNo)
        {
            if (string.IsNullOrEmpty(payrollNo)) return null;

            var cacheKey = GetCacheKey(payrollNo);

            try
            {
                // Try session first (fastest)
                var session = _httpContextAccessor.HttpContext?.Session;
                if (session != null)
                {
                    var sessionProfile = session.GetString($"UserProfile_{payrollNo}");
                    if (!string.IsNullOrEmpty(sessionProfile))
                    {
                        var profile = JsonSerializer.Deserialize<UserProfile>(sessionProfile);
                        if (profile != null && !profile.CacheInfo.IsExpired)
                        {
                            _logger.LogDebug("Retrieved user profile from session for PayrollNo: {PayrollNo}", payrollNo);
                            return profile;
                        }
                    }
                }

                // Try memory cache next
                if (_memoryCache.TryGetValue(cacheKey, out UserProfile cachedProfile))
                {
                    if (!cachedProfile.CacheInfo.IsExpired)
                    {
                        // Refresh session cache from memory cache
                        if (session != null)
                        {
                            var profileJson = JsonSerializer.Serialize(cachedProfile);
                            session.SetString($"UserProfile_{payrollNo}", profileJson);
                        }

                        _logger.LogDebug("Retrieved user profile from memory cache for PayrollNo: {PayrollNo}", payrollNo);
                        return cachedProfile;
                    }
                    else
                    {
                        // Remove expired profile
                        _memoryCache.Remove(cacheKey);
                    }
                }

                _logger.LogDebug("User profile not found in cache for PayrollNo: {PayrollNo}", payrollNo);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user profile from cache for PayrollNo: {PayrollNo}", payrollNo);
                return null;
            }
        }

        public async Task InvalidateAsync(string payrollNo)
        {
            if (string.IsNullOrEmpty(payrollNo)) return;

            var cacheKey = GetCacheKey(payrollNo);

            try
            {
                // Remove from memory cache
                _memoryCache.Remove(cacheKey);

                // Remove from session
                var session = _httpContextAccessor.HttpContext?.Session;
                if (session != null)
                {
                    session.Remove($"UserProfile_{payrollNo}");
                }

                _logger.LogInformation("Invalidated user profile cache for PayrollNo: {PayrollNo}", payrollNo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating user profile cache for PayrollNo: {PayrollNo}", payrollNo);
            }
        }

        public async Task RefreshAsync(string payrollNo, UserProfile newProfile)
        {
            if (newProfile == null) return;

            // Invalidate old cache first
            await InvalidateAsync(payrollNo);

            // Set new profile
            await SetProfileAsync(newProfile);

            _logger.LogInformation("Refreshed user profile cache for PayrollNo: {PayrollNo}", payrollNo);
        }

        private static string GetCacheKey(string payrollNo)
        {
            return $"UserProfile_{payrollNo}";
        }
    }
}