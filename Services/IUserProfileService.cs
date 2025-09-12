using MRIV.Models;

namespace MRIV.Services
{
    public interface IUserProfileService
    {
        /// <summary>
        /// Builds comprehensive user profile from cross-context database lookups
        /// This is called at login time to create cached user context
        /// </summary>
        Task<UserProfile> BuildUserProfileAsync(string payrollNo);

        /// <summary>
        /// Gets cached user profile from session/memory cache
        /// Returns null if not found or expired
        /// </summary>
        Task<UserProfile> GetCachedProfileAsync(string payrollNo);

        /// <summary>
        /// Refreshes user profile by rebuilding from database and updating cache
        /// Used when user permissions change or cache expires
        /// </summary>
        Task<UserProfile> RefreshUserProfileAsync(string payrollNo);

        /// <summary>
        /// Invalidates user profile cache
        /// Called when role group memberships change
        /// </summary>
        Task InvalidateProfileAsync(string payrollNo);

        /// <summary>
        /// Builds user profile for current session user
        /// Convenience method that gets payroll from session
        /// </summary>
        Task<UserProfile> GetCurrentUserProfileAsync();
    }

    public interface IUserProfileCacheService
    {
        /// <summary>
        /// Stores user profile in session and memory cache
        /// </summary>
        Task SetProfileAsync(UserProfile profile);

        /// <summary>
        /// Retrieves user profile from cache (session first, then memory)
        /// </summary>
        Task<UserProfile> GetProfileAsync(string payrollNo);

        /// <summary>
        /// Removes user profile from all cache layers
        /// </summary>
        Task InvalidateAsync(string payrollNo);

        /// <summary>
        /// Updates existing cached profile
        /// </summary>
        Task RefreshAsync(string payrollNo, UserProfile newProfile);
    }
}