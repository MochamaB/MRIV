using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MRIV.Services;
using MRIV.Models;
using System.Security.Claims;
using MRIV.Attributes;

namespace MRIV.Controllers
{
    [CustomAuthorize]
    public class GuideController : Controller
    {
        private readonly IGuideService _guideService;
        private readonly ILogger<GuideController> _logger;

        public GuideController(IGuideService guideService, ILogger<GuideController> logger)
        {
            _guideService = guideService;
            _logger = logger;
        }

        // GET: /Guide
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var userPayrollNo = User.FindFirst(ClaimTypes.Name)?.Value;
                var guideNavigation = await _guideService.GetNavigationAsync(userPayrollNo);
                
                ViewBag.Title = "User Guide";
                return View(guideNavigation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading guide index");
                return View("Error");
            }
        }

        // GET: /Guide/Section/{section}
        [HttpGet("Guide/Section/{section}")]
        public async Task<IActionResult> Section(string section)
        {
            try
            {
                var userPayrollNo = User.FindFirst(ClaimTypes.Name)?.Value;
                var sectionContent = await _guideService.GetSectionAsync(section, userPayrollNo);
                
                if (sectionContent == null)
                {
                    return NotFound($"Section '{section}' not found");
                }

                ViewBag.Title = sectionContent.Title;
                ViewBag.Section = section;
                return View("Section", sectionContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading guide section: {Section}", section);
                return View("Error");
            }
        }

        // GET: /Guide/Page/{section}/{page}
        [HttpGet("Guide/Page/{section}/{page}")]
        public async Task<IActionResult> Page(string section, string page)
        {
            try
            {
                var userPayrollNo = User.FindFirst(ClaimTypes.Name)?.Value;
                var pageContent = await _guideService.GetPageAsync(section, page, userPayrollNo);
                
                if (pageContent == null)
                {
                    return NotFound($"Page '{section}/{page}' not found");
                }

                ViewBag.Title = pageContent.Title;
                ViewBag.Section = section;
                ViewBag.Page = page;
                
                // Get navigation for breadcrumbs
                var navigation = await _guideService.GetNavigationAsync(userPayrollNo);
                ViewBag.Navigation = navigation;
                
                return View("Page", pageContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading guide page: {Section}/{Page}", section, page);
                return View("Error");
            }
        }

        // GET: /Guide/Search
        [HttpGet("Guide/Search")]
        public async Task<IActionResult> Search(string query, int page = 1)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return View("SearchResults", new GuideSearchResult { Query = query, Results = new List<GuideSearchItem>() });
                }

                var userPayrollNo = User.FindFirst(ClaimTypes.Name)?.Value;
                var searchResults = await _guideService.SearchAsync(query, userPayrollNo, page);
                
                ViewBag.Title = $"Search Results for '{query}'";
                return View("SearchResults", searchResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching guides: {Query}", query);
                return View("Error");
            }
        }

        // GET: /Guide/QuickHelp/{context}
        [HttpGet("Guide/QuickHelp/{context}")]
        public async Task<IActionResult> QuickHelp(string context)
        {
            try
            {
                var userPayrollNo = User.FindFirst(ClaimTypes.Name)?.Value;
                var helpContent = await _guideService.GetContextHelpAsync(context, userPayrollNo);
                
                if (helpContent == null)
                {
                    return PartialView("_QuickHelpNotFound", context);
                }

                return PartialView("_QuickHelp", helpContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading context help: {Context}", context);
                return PartialView("_QuickHelpError", context);
            }
        }

        // POST: /Guide/Feedback
        [HttpPost("Guide/Feedback")]
        public async Task<IActionResult> SubmitFeedback([FromBody] GuideFeedback feedback)
        {
            try
            {
                var userPayrollNo = User.FindFirst(ClaimTypes.Name)?.Value;
                feedback.UserPayrollNo = userPayrollNo;
                feedback.SubmittedAt = DateTime.Now;
                
                var success = await _guideService.SubmitFeedbackAsync(feedback);
                
                return Json(new { success = success, message = success ? "Feedback submitted successfully" : "Failed to submit feedback" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting guide feedback");
                return Json(new { success = false, message = "An error occurred while submitting feedback" });
            }
        }

        // GET: /Guide/PrintVersion/{section}/{page?}
        [HttpGet("Guide/PrintVersion/{section}/{page?}")]
        public async Task<IActionResult> PrintVersion(string section, string page = null)
        {
            try
            {
                var userPayrollNo = User.FindFirst(ClaimTypes.Name)?.Value;
                
                if (!string.IsNullOrEmpty(page))
                {
                    var pageContent = await _guideService.GetPageAsync(section, page, userPayrollNo);
                    return View("PrintPage", pageContent);
                }
                else
                {
                    var sectionContent = await _guideService.GetSectionAsync(section, userPayrollNo);
                    return View("PrintSection", sectionContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating print version: {Section}/{Page}", section, page);
                return View("Error");
            }
        }
    }
}