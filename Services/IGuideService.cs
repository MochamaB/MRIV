using MRIV.Models;

namespace MRIV.Services
{
    /// <summary>
    /// Interface for the Guide service that handles user guide content management
    /// </summary>
    public interface IGuideService
    {
        /// <summary>
        /// Get the main navigation structure based on user permissions
        /// </summary>
        /// <param name="userPayrollNo">User's payroll number for role-based filtering</param>
        /// <returns>Guide navigation structure</returns>
        Task<GuideNavigation> GetNavigationAsync(string? userPayrollNo);

        /// <summary>
        /// Get content for a specific section
        /// </summary>
        /// <param name="sectionId">Section identifier</param>
        /// <param name="userPayrollNo">User's payroll number for access control</param>
        /// <returns>Section content or null if not found/accessible</returns>
        Task<GuideSection?> GetSectionAsync(string sectionId, string? userPayrollNo);

        /// <summary>
        /// Get content for a specific page within a section
        /// </summary>
        /// <param name="sectionId">Section identifier</param>
        /// <param name="pageId">Page identifier</param>
        /// <param name="userPayrollNo">User's payroll number for access control</param>
        /// <returns>Page content or null if not found/accessible</returns>
        Task<GuidePage?> GetPageAsync(string sectionId, string pageId, string? userPayrollNo);

        /// <summary>
        /// Search guide content based on query and user permissions
        /// </summary>
        /// <param name="query">Search query</param>
        /// <param name="userPayrollNo">User's payroll number for filtering results</param>
        /// <param name="page">Page number for pagination</param>
        /// <returns>Search results</returns>
        Task<GuideSearchResult> SearchAsync(string query, string? userPayrollNo, int page = 1);

        /// <summary>
        /// Get context-sensitive help for a specific application context
        /// </summary>
        /// <param name="context">Application context (e.g., "requisition-create", "material-assign")</param>
        /// <param name="userPayrollNo">User's payroll number for role-based content</param>
        /// <returns>Context help or null if not found</returns>
        Task<GuideContextHelp?> GetContextHelpAsync(string context, string? userPayrollNo);

        /// <summary>
        /// Submit user feedback for guide content
        /// </summary>
        /// <param name="feedback">Feedback details</param>
        /// <returns>True if feedback was successfully submitted</returns>
        Task<bool> SubmitFeedbackAsync(GuideFeedback feedback);

        /// <summary>
        /// Get table of contents for a specific page
        /// </summary>
        /// <param name="sectionId">Section identifier</param>
        /// <param name="pageId">Page identifier</param>
        /// <returns>Table of contents</returns>
        Task<GuideTableOfContents> GetTableOfContentsAsync(string sectionId, string pageId);

        /// <summary>
        /// Get popular/frequently accessed content
        /// </summary>
        /// <param name="userPayrollNo">User's payroll number for personalized results</param>
        /// <param name="count">Number of items to return</param>
        /// <returns>List of popular content</returns>
        Task<List<GuidePopularContent>> GetPopularContentAsync(string? userPayrollNo, int count = 5);

        /// <summary>
        /// Track user interaction with guide content
        /// </summary>
        /// <param name="analytics">Analytics data</param>
        /// <returns>True if tracking was successful</returns>
        Task<bool> TrackUserInteractionAsync(GuideAnalytics analytics);

        /// <summary>
        /// Check if user has access to specific guide content
        /// </summary>
        /// <param name="requiredRoles">Required roles for access</param>
        /// <param name="requiredRoleGroups">Required role groups for access</param>
        /// <param name="userPayrollNo">User's payroll number</param>
        /// <returns>True if user has access</returns>
        Task<bool> HasAccessAsync(List<string> requiredRoles, List<string> requiredRoleGroups, string? userPayrollNo);

        /// <summary>
        /// Reload guide configuration from files (for administrators)
        /// </summary>
        /// <returns>True if reload was successful</returns>
        Task<bool> ReloadConfigurationAsync();
    }
}