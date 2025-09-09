using System.ComponentModel.DataAnnotations;

namespace MRIV.Models
{
    /// <summary>
    /// Represents the main navigation structure for the guide system
    /// </summary>
    public class GuideNavigation
    {
        public List<GuideSection> Sections { get; set; } = new List<GuideSection>();
        public string UserRole { get; set; } = string.Empty;
        public List<string> UserRoleGroups { get; set; } = new List<string>();
    }

    /// <summary>
    /// Represents a major section in the guide (e.g., Authorization System, User Guides)
    /// </summary>
    public class GuideSection
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "fas fa-book";
        public List<GuidePage> Pages { get; set; } = new List<GuidePage>();
        public List<string> RequiredRoles { get; set; } = new List<string>();
        public List<string> RequiredRoleGroups { get; set; } = new List<string>();
        public int SortOrder { get; set; }
        public bool IsVisible { get; set; } = true;
    }

    /// <summary>
    /// Represents an individual page within a section
    /// </summary>
    public class GuidePage
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string HtmlContent { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new List<string>();
        public List<string> RequiredRoles { get; set; } = new List<string>();
        public List<string> RequiredRoleGroups { get; set; } = new List<string>();
        public int SortOrder { get; set; }
        public bool IsVisible { get; set; } = true;
        public DateTime LastModified { get; set; }
        public string LastModifiedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents search functionality for guides
    /// </summary>
    public class GuideSearchResult
    {
        public string Query { get; set; } = string.Empty;
        public List<GuideSearchItem> Results { get; set; } = new List<GuideSearchItem>();
        public int TotalResults { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    /// <summary>
    /// Individual search result item
    /// </summary>
    public class GuideSearchItem
    {
        public string SectionId { get; set; } = string.Empty;
        public string SectionTitle { get; set; } = string.Empty;
        public string PageId { get; set; } = string.Empty;
        public string PageTitle { get; set; } = string.Empty;
        public string Excerpt { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public double Relevance { get; set; }
        public List<string> MatchedTerms { get; set; } = new List<string>();
    }

    /// <summary>
    /// Context-sensitive help content
    /// </summary>
    public class GuideContextHelp
    {
        public string Context { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string HtmlContent { get; set; } = string.Empty;
        public List<GuideQuickLink> QuickLinks { get; set; } = new List<GuideQuickLink>();
        public List<GuideTip> Tips { get; set; } = new List<GuideTip>();
    }

    /// <summary>
    /// Quick navigation links for context help
    /// </summary>
    public class GuideQuickLink
    {
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "fas fa-link";
    }

    /// <summary>
    /// Helpful tips for context help
    /// </summary>
    public class GuideTip
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Type { get; set; } = "info"; // info, warning, tip, important
        public string Icon { get; set; } = "fas fa-info-circle";
    }

    /// <summary>
    /// User feedback model for guide improvement
    /// </summary>
    public class GuideFeedback
    {
        public int Id { get; set; }
        public string UserPayrollNo { get; set; } = string.Empty;
        public string SectionId { get; set; } = string.Empty;
        public string PageId { get; set; } = string.Empty;
        public string FeedbackType { get; set; } = string.Empty; // helpful, not-helpful, suggestion, error
        
        [Required]
        [StringLength(1000)]
        public string Comment { get; set; } = string.Empty;
        
        [Range(1, 5)]
        public int? Rating { get; set; }
        
        public DateTime SubmittedAt { get; set; }
        public bool IsResolved { get; set; } = false;
        public string Resolution { get; set; } = string.Empty;
        public string ResolvedBy { get; set; } = string.Empty;
        public DateTime? ResolvedAt { get; set; }
    }

    /// <summary>
    /// Guide configuration loaded from JSON
    /// </summary>
    public class GuideConfiguration
    {
        public string Title { get; set; } = "MRIV User Guide";
        public string Description { get; set; } = "Comprehensive guide for the Material Requisition and Inventory Verification system";
        public string Version { get; set; } = "1.0";
        public List<GuideSection> Sections { get; set; } = new List<GuideSection>();
        public GuideSettings Settings { get; set; } = new GuideSettings();
    }

    /// <summary>
    /// Guide system settings
    /// </summary>
    public class GuideSettings
    {
        public bool EnableSearch { get; set; } = true;
        public bool EnableFeedback { get; set; } = true;
        public bool EnableContextHelp { get; set; } = true;
        public bool EnablePrintVersion { get; set; } = true;
        public int SearchResultsPerPage { get; set; } = 10;
        public List<string> AdminRoles { get; set; } = new List<string> { "Admin" };
        public List<string> SupportRoles { get; set; } = new List<string> { "Support", "Admin" };
    }

    /// <summary>
    /// Table of contents for navigation within pages
    /// </summary>
    public class GuideTableOfContents
    {
        public List<GuideTocItem> Items { get; set; } = new List<GuideTocItem>();
    }

    /// <summary>
    /// Individual table of contents item
    /// </summary>
    public class GuideTocItem
    {
        public string Title { get; set; } = string.Empty;
        public string Anchor { get; set; } = string.Empty;
        public int Level { get; set; } = 1; // H1=1, H2=2, etc.
        public List<GuideTocItem> Children { get; set; } = new List<GuideTocItem>();
    }

    /// <summary>
    /// Breadcrumb navigation
    /// </summary>
    public class GuideBreadcrumb
    {
        public List<GuideBreadcrumbItem> Items { get; set; } = new List<GuideBreadcrumbItem>();
    }

    /// <summary>
    /// Individual breadcrumb item
    /// </summary>
    public class GuideBreadcrumbItem
    {
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public bool IsActive { get; set; } = false;
    }

    /// <summary>
    /// Guide analytics for tracking usage
    /// </summary>
    public class GuideAnalytics
    {
        public string SectionId { get; set; } = string.Empty;
        public string PageId { get; set; } = string.Empty;
        public string UserPayrollNo { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; // view, search, feedback, print
        public DateTime Timestamp { get; set; }
        public string UserAgent { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public int TimeSpentSeconds { get; set; }
    }

    /// <summary>
    /// Popular content tracking
    /// </summary>
    public class GuidePopularContent
    {
        public string SectionId { get; set; } = string.Empty;
        public string SectionTitle { get; set; } = string.Empty;
        public string PageId { get; set; } = string.Empty;
        public string PageTitle { get; set; } = string.Empty;
        public int ViewCount { get; set; }
        public int SearchCount { get; set; }
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }
        public DateTime LastAccessed { get; set; }
    }
}