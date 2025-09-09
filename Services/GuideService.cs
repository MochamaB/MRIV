using MRIV.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.SyntaxHighlighting;

namespace MRIV.Services
{
    /// <summary>
    /// Service for managing user guide content, navigation, and access control
    /// </summary>
    public class GuideService : IGuideService
    {
        private readonly IEmployeeService _employeeService;
        private readonly IVisibilityAuthorizeService _visibilityService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<GuideService> _logger;
        private readonly MarkdownPipeline _markdownPipeline;
        private GuideConfiguration? _configuration;
        private readonly string _guidesPath;
        private readonly object _configLock = new object();

        public GuideService(
            IEmployeeService employeeService,
            IVisibilityAuthorizeService visibilityService,
            IWebHostEnvironment environment,
            ILogger<GuideService> logger)
        {
            _employeeService = employeeService;
            _visibilityService = visibilityService;
            _environment = environment;
            _logger = logger;
            _guidesPath = Path.Combine(_environment.ContentRootPath, "Guides");
            
            // Configure Markdown pipeline with extensions
            _markdownPipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseBootstrap()
                .UseSyntaxHighlighting()
                .Build();
        }

        public async Task<GuideNavigation> GetNavigationAsync(string? userPayrollNo)
        {
            try
            {
                await EnsureConfigurationLoadedAsync();
                
                var navigation = new GuideNavigation();
                
                if (!string.IsNullOrEmpty(userPayrollNo))
                {
                    var employee = await _employeeService.GetEmployeeByPayrollAsync(userPayrollNo);
                    if (employee != null)
                    {
                        navigation.UserRole = employee.Role ?? string.Empty;
                        // Get user role groups (implement based on your role group service)
                        // navigation.UserRoleGroups = await GetUserRoleGroupsAsync(userPayrollNo);
                    }
                }

                if (_configuration?.Sections != null)
                {
                    foreach (var section in _configuration.Sections.OrderBy(s => s.SortOrder))
                    {
                        if (await HasAccessAsync(section.RequiredRoles, section.RequiredRoleGroups, userPayrollNo))
                        {
                            var accessibleSection = new GuideSection
                            {
                                Id = section.Id,
                                Title = section.Title,
                                Description = section.Description,
                                Icon = section.Icon,
                                SortOrder = section.SortOrder
                            };

                            // Filter pages based on user access
                            foreach (var page in section.Pages.OrderBy(p => p.SortOrder))
                            {
                                if (await HasAccessAsync(page.RequiredRoles, page.RequiredRoleGroups, userPayrollNo))
                                {
                                    accessibleSection.Pages.Add(page);
                                }
                            }

                            if (accessibleSection.Pages.Any() || section.Pages.Count == 0)
                            {
                                navigation.Sections.Add(accessibleSection);
                            }
                        }
                    }
                }

                return navigation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting guide navigation for user {UserPayrollNo}", userPayrollNo);
                return new GuideNavigation();
            }
        }

        public async Task<GuideSection?> GetSectionAsync(string sectionId, string? userPayrollNo)
        {
            try
            {
                await EnsureConfigurationLoadedAsync();
                
                var section = _configuration?.Sections?.FirstOrDefault(s => s.Id.Equals(sectionId, StringComparison.OrdinalIgnoreCase));
                if (section == null || !await HasAccessAsync(section.RequiredRoles, section.RequiredRoleGroups, userPayrollNo))
                {
                    return null;
                }

                // Load content for all pages in the section
                var sectionCopy = new GuideSection
                {
                    Id = section.Id,
                    Title = section.Title,
                    Description = section.Description,
                    Icon = section.Icon,
                    SortOrder = section.SortOrder
                };

                foreach (var page in section.Pages.OrderBy(p => p.SortOrder))
                {
                    if (await HasAccessAsync(page.RequiredRoles, page.RequiredRoleGroups, userPayrollNo))
                    {
                        var loadedPage = await LoadPageContentAsync(sectionId, page);
                        if (loadedPage != null)
                        {
                            sectionCopy.Pages.Add(loadedPage);
                        }
                    }
                }

                return sectionCopy;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting section {SectionId} for user {UserPayrollNo}", sectionId, userPayrollNo);
                return null;
            }
        }

        public async Task<GuidePage?> GetPageAsync(string sectionId, string pageId, string? userPayrollNo)
        {
            try
            {
                await EnsureConfigurationLoadedAsync();
                
                var section = _configuration?.Sections?.FirstOrDefault(s => s.Id.Equals(sectionId, StringComparison.OrdinalIgnoreCase));
                if (section == null || !await HasAccessAsync(section.RequiredRoles, section.RequiredRoleGroups, userPayrollNo))
                {
                    return null;
                }

                var page = section.Pages?.FirstOrDefault(p => p.Id.Equals(pageId, StringComparison.OrdinalIgnoreCase));
                if (page == null || !await HasAccessAsync(page.RequiredRoles, page.RequiredRoleGroups, userPayrollNo))
                {
                    return null;
                }

                var loadedPage = await LoadPageContentAsync(sectionId, page);
                
                // Track page view
                if (loadedPage != null && !string.IsNullOrEmpty(userPayrollNo))
                {
                    _ = Task.Run(async () =>
                    {
                        await TrackUserInteractionAsync(new GuideAnalytics
                        {
                            SectionId = sectionId,
                            PageId = pageId,
                            UserPayrollNo = userPayrollNo,
                            Action = "view",
                            Timestamp = DateTime.Now
                        });
                    });
                }

                return loadedPage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting page {SectionId}/{PageId} for user {UserPayrollNo}", sectionId, pageId, userPayrollNo);
                return null;
            }
        }

        public async Task<GuideSearchResult> SearchAsync(string query, string? userPayrollNo, int page = 1)
        {
            try
            {
                var result = new GuideSearchResult
                {
                    Query = query,
                    CurrentPage = page,
                    PageSize = 10
                };

                if (string.IsNullOrWhiteSpace(query))
                {
                    return result;
                }

                await EnsureConfigurationLoadedAsync();
                
                var searchItems = new List<GuideSearchItem>();
                var searchTerms = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (_configuration?.Sections != null)
                {
                    foreach (var section in _configuration.Sections)
                    {
                        if (!await HasAccessAsync(section.RequiredRoles, section.RequiredRoleGroups, userPayrollNo))
                            continue;

                        foreach (var pageConfig in section.Pages)
                        {
                            if (!await HasAccessAsync(pageConfig.RequiredRoles, pageConfig.RequiredRoleGroups, userPayrollNo))
                                continue;

                            var guidepage = await LoadPageContentAsync(section.Id, pageConfig);
                            if (guidepage?.Content == null) continue;

                            var relevance = CalculateRelevance(guidepage.Title, guidepage.Content, searchTerms);
                            if (relevance > 0)
                            {
                                searchItems.Add(new GuideSearchItem
                                {
                                    SectionId = section.Id,
                                    SectionTitle = section.Title,
                                    PageId = guidepage.Id,
                                    PageTitle = guidepage.Title,
                                    Excerpt = ExtractExcerpt(guidepage.Content, searchTerms, 200),
                                    Url = $"/Guide/Page/{section.Id}/{guidepage.Id}",
                                    Relevance = relevance,
                                    MatchedTerms = searchTerms.ToList()
                                });
                            }
                        }
                    }
                }

                // Sort by relevance and paginate
                var totalResults = searchItems.Count;
                var pagedResults = searchItems
                    .OrderByDescending(x => x.Relevance)
                    .Skip((page - 1) * result.PageSize)
                    .Take(result.PageSize)
                    .ToList();

                result.Results = pagedResults;
                result.TotalResults = totalResults;
                result.TotalPages = (int)Math.Ceiling(totalResults / (double)result.PageSize);

                // Track search
                if (!string.IsNullOrEmpty(userPayrollNo))
                {
                    _ = Task.Run(async () =>
                    {
                        await TrackUserInteractionAsync(new GuideAnalytics
                        {
                            UserPayrollNo = userPayrollNo,
                            Action = "search",
                            Timestamp = DateTime.Now
                        });
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching guides with query '{Query}' for user {UserPayrollNo}", query, userPayrollNo);
                return new GuideSearchResult { Query = query };
            }
        }

        public async Task<GuideContextHelp?> GetContextHelpAsync(string context, string? userPayrollNo)
        {
            try
            {
                var helpFilePath = Path.Combine(_guidesPath, "ContextHelp", $"{context}.md");
                if (!File.Exists(helpFilePath))
                {
                    return null;
                }

                var content = await File.ReadAllTextAsync(helpFilePath);
                var htmlContent = Markdown.ToHtml(content, _markdownPipeline);

                return new GuideContextHelp
                {
                    Context = context,
                    Title = ExtractTitle(content),
                    Content = content,
                    HtmlContent = htmlContent
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting context help for {Context}", context);
                return null;
            }
        }

        public async Task<bool> SubmitFeedbackAsync(GuideFeedback feedback)
        {
            try
            {
                // In a real implementation, you would save this to a database
                // For now, we'll log it
                _logger.LogInformation("Guide feedback received: {Feedback}", JsonSerializer.Serialize(feedback));
                
                // Could also save to file or database here
                var feedbackPath = Path.Combine(_guidesPath, "Feedback");
                Directory.CreateDirectory(feedbackPath);
                
                var feedbackFile = Path.Combine(feedbackPath, $"feedback_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.json");
                await File.WriteAllTextAsync(feedbackFile, JsonSerializer.Serialize(feedback, new JsonSerializerOptions { WriteIndented = true }));
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting guide feedback");
                return false;
            }
        }

        public async Task<GuideTableOfContents> GetTableOfContentsAsync(string sectionId, string pageId)
        {
            try
            {
                var page = await GetPageAsync(sectionId, pageId, null); // No access control for TOC
                if (page?.Content == null)
                {
                    return new GuideTableOfContents();
                }

                var toc = new GuideTableOfContents();
                var headerRegex = new Regex(@"^(#{1,6})\s+(.+)$", RegexOptions.Multiline);
                var matches = headerRegex.Matches(page.Content);

                foreach (Match match in matches)
                {
                    var level = match.Groups[1].Value.Length;
                    var title = match.Groups[2].Value.Trim();
                    var anchor = GenerateAnchor(title);

                    toc.Items.Add(new GuideTocItem
                    {
                        Title = title,
                        Anchor = anchor,
                        Level = level
                    });
                }

                return toc;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating table of contents for {SectionId}/{PageId}", sectionId, pageId);
                return new GuideTableOfContents();
            }
        }

        public async Task<List<GuidePopularContent>> GetPopularContentAsync(string? userPayrollNo, int count = 5)
        {
            // In a real implementation, this would query analytics data
            // For now, return a dummy list
            return new List<GuidePopularContent>
            {
                new GuidePopularContent
                {
                    SectionId = "authorization",
                    SectionTitle = "Authorization System",
                    PageId = "quick-reference",
                    PageTitle = "Quick Reference",
                    ViewCount = 150,
                    AverageRating = 4.5,
                    LastAccessed = DateTime.Now.AddDays(-1)
                }
            };
        }

        public async Task<bool> TrackUserInteractionAsync(GuideAnalytics analytics)
        {
            try
            {
                // In a real implementation, save to database or analytics service
                _logger.LogInformation("Guide interaction: {Analytics}", JsonSerializer.Serialize(analytics));
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking user interaction");
                return false;
            }
        }

        public async Task<bool> HasAccessAsync(List<string> requiredRoles, List<string> requiredRoleGroups, string? userPayrollNo)
        {
            try
            {
                // If no requirements specified, allow access
                if ((requiredRoles == null || !requiredRoles.Any()) && 
                    (requiredRoleGroups == null || !requiredRoleGroups.Any()))
                {
                    return true;
                }

                if (string.IsNullOrEmpty(userPayrollNo))
                {
                    return false;
                }

                var employee = await _employeeService.GetEmployeeByPayrollAsync(userPayrollNo);
                if (employee == null)
                {
                    return false;
                }

                // Check role requirements
                if (requiredRoles != null && requiredRoles.Any())
                {
                    if (requiredRoles.Contains(employee.Role, StringComparer.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                // Check role group requirements (implement based on your role group service)
                if (requiredRoleGroups != null && requiredRoleGroups.Any())
                {
                    // TODO: Implement role group checking
                    // var userRoleGroups = await GetUserRoleGroupsAsync(userPayrollNo);
                    // return requiredRoleGroups.Any(rg => userRoleGroups.Contains(rg, StringComparer.OrdinalIgnoreCase));
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking access for user {UserPayrollNo}", userPayrollNo);
                return false;
            }
        }

        public async Task<bool> ReloadConfigurationAsync()
        {
            try
            {
                lock (_configLock)
                {
                    _configuration = null;
                }
                await EnsureConfigurationLoadedAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reloading guide configuration");
                return false;
            }
        }

        #region Private Helper Methods

        private async Task EnsureConfigurationLoadedAsync()
        {
            if (_configuration != null) return;

            lock (_configLock)
            {
                if (_configuration != null) return;

                try
                {
                    var configPath = Path.Combine(_guidesPath, "_config.json");
                    if (File.Exists(configPath))
                    {
                        var configJson = File.ReadAllText(configPath);
                        _configuration = JsonSerializer.Deserialize<GuideConfiguration>(configJson);
                    }
                    else
                    {
                        _configuration = CreateDefaultConfiguration();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading guide configuration, using default");
                    _configuration = CreateDefaultConfiguration();
                }
            }
        }

        private GuideConfiguration CreateDefaultConfiguration()
        {
            return new GuideConfiguration
            {
                Title = "MRIV User Guide",
                Description = "User guide for the Material Requisition and Inventory Verification system",
                Version = "1.0",
                Sections = new List<GuideSection>
                {
                    new GuideSection
                    {
                        Id = "authorization",
                        Title = "Authorization System",
                        Description = "Understanding role groups and permissions",
                        Icon = "fas fa-shield-alt",
                        SortOrder = 1,
                        Pages = new List<GuidePage>
                        {
                            new GuidePage
                            {
                                Id = "quick-reference",
                                Title = "Quick Reference",
                                FileName = "authorization/quick-reference.md",
                                SortOrder = 1
                            }
                        }
                    }
                }
            };
        }

        private async Task<GuidePage?> LoadPageContentAsync(string sectionId, GuidePage page)
        {
            try
            {
                var filePath = Path.Combine(_guidesPath, page.FileName);
                if (!File.Exists(filePath))
                {
                    return null;
                }

                var content = await File.ReadAllTextAsync(filePath);
                var fileInfo = new FileInfo(filePath);

                return new GuidePage
                {
                    Id = page.Id,
                    Title = page.Title,
                    Description = page.Description,
                    FileName = page.FileName,
                    Content = content,
                    HtmlContent = Markdown.ToHtml(content, _markdownPipeline),
                    Tags = page.Tags,
                    RequiredRoles = page.RequiredRoles,
                    RequiredRoleGroups = page.RequiredRoleGroups,
                    SortOrder = page.SortOrder,
                    IsVisible = page.IsVisible,
                    LastModified = fileInfo.LastWriteTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading page content for {FileName}", page.FileName);
                return null;
            }
        }

        private double CalculateRelevance(string title, string content, string[] searchTerms)
        {
            var relevance = 0.0;
            var titleLower = title.ToLowerInvariant();
            var contentLower = content.ToLowerInvariant();

            foreach (var term in searchTerms)
            {
                // Title matches are worth more
                if (titleLower.Contains(term))
                {
                    relevance += 10.0;
                }

                // Content matches
                var contentMatches = Regex.Matches(contentLower, Regex.Escape(term)).Count;
                relevance += contentMatches * 1.0;
            }

            return relevance;
        }

        private string ExtractExcerpt(string content, string[] searchTerms, int maxLength)
        {
            var contentLower = content.ToLowerInvariant();
            var firstMatchIndex = -1;

            foreach (var term in searchTerms)
            {
                var index = contentLower.IndexOf(term);
                if (index >= 0 && (firstMatchIndex == -1 || index < firstMatchIndex))
                {
                    firstMatchIndex = index;
                }
            }

            var startIndex = Math.Max(0, firstMatchIndex - maxLength / 4);
            var endIndex = Math.Min(content.Length, startIndex + maxLength);
            
            var excerpt = content.Substring(startIndex, endIndex - startIndex);
            if (startIndex > 0) excerpt = "..." + excerpt;
            if (endIndex < content.Length) excerpt += "...";

            return excerpt;
        }

        private string ExtractTitle(string content)
        {
            var titleMatch = Regex.Match(content, @"^#\s+(.+)$", RegexOptions.Multiline);
            return titleMatch.Success ? titleMatch.Groups[1].Value.Trim() : "Help";
        }

        private string GenerateAnchor(string title)
        {
            return Regex.Replace(title.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        }

        #endregion
    }
}