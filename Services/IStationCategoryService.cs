using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MRIV.Models;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace MRIV.Services
{
    public interface IStationCategoryService
    {
        Task<List<StationCategory>> GetAllCategoriesAsync();
        Task<SelectList> GetLocationsForCategoryAsync(string categoryCode, string selectedValue = null);
        Task<SelectList> GetStationCategoriesSelectListAsync(string stationPoint);
        Task InitializeUserLocationAsync(Requisition requisition, EmployeeBkp employee, Department department, Station station);
        Task<List<object>> GetLocationItemsForJsonAsync(string categoryCode = null, string selectedValue = null);
    }

    public class StationCategoryService : IStationCategoryService
    {
        private readonly KtdaleaveContext _ktdaContext;
        private readonly RequisitionContext _requisitionContext;
        private readonly VendorService _vendorService;
        private readonly ILogger<StationCategoryService> _logger;

        public StationCategoryService(
            KtdaleaveContext ktdaContext,
            RequisitionContext requisitionContext,
            VendorService vendorService)
        {
            _ktdaContext = ktdaContext;
            _requisitionContext = requisitionContext;
            _vendorService = vendorService;
        }

        public async Task<List<StationCategory>> GetAllCategoriesAsync()
        {
            // Return predefined list or from database
            return new List<StationCategory>
        {
            new StationCategory { Id = 1, Code = "headoffice", StationName = "Head Office", DataSource = "Department" },
            new StationCategory { Id = 2, Code = "factory", StationName = "Factory", DataSource = "Station", FilterCriteria = "{\"exclude\": [\"region\", \"zonal\"]}" },
            new StationCategory { Id = 3, Code = "region", StationName = "Region", DataSource = "Station", FilterCriteria = "{\"include\": [\"region\"]}" },
            new StationCategory { Id = 4, Code = "vendor", StationName = "Vendor", DataSource = "Vendor" }
        };
        }
        public async Task<SelectList> GetStationCategoriesSelectListAsync(string stationPoint)
        {
            // Default to an empty SelectList if no data is found
            var categories = await _requisitionContext.StationCategories
                .Where(sc => sc.StationPoint == "both" || sc.StationPoint == stationPoint)
                .ToListAsync();

            // Always return a valid SelectList, even if empty
            return new SelectList(categories ?? new List<StationCategory>(), "Code", "StationName");
        }

        public async Task<SelectList> GetLocationsForCategoryAsync(string categoryCode = null, string selectedValue = null)
        {
            if (string.IsNullOrEmpty(categoryCode))
                return new SelectList(Enumerable.Empty<SelectListItem>());

            // Get the specific category from the database
            var category = await _requisitionContext.StationCategories
                .FirstOrDefaultAsync(c => c.Code.ToLower() == categoryCode.ToLower());

            if (category == null)
                return new SelectList(Enumerable.Empty<SelectListItem>());

            // Use the DataSource property to determine which data to load
            switch (category.DataSource?.ToLower())
            {
                case "department":
                    var departments = await _ktdaContext.Departments.ToListAsync();
                    return new SelectList(departments, "DepartmentName", "DepartmentName", selectedValue);

                case "station":
                    var stations = await _ktdaContext.Stations.ToListAsync();

                    // Apply filters if specified
                    if (!string.IsNullOrEmpty(category.FilterCriteria))
                    {
                        try
                        {
                            var filters = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, List<string>>>(
                                category.FilterCriteria);

                            if (filters != null)
                            {
                                if (filters.ContainsKey("include") && filters["include"] != null)
                                {
                                    stations = stations.Where(s =>
                                        filters["include"].Any(term =>
                                            s.StationName.Contains(term, StringComparison.OrdinalIgnoreCase)))
                                        .ToList();
                                }

                                if (filters.ContainsKey("exclude") && filters["exclude"] != null)
                                {
                                    stations = stations.Where(s =>
                                        !filters["exclude"].Any(term =>
                                            s.StationName.Contains(term, StringComparison.OrdinalIgnoreCase)))
                                        .ToList();
                                }
                            }
                        }
                        catch (JsonException ex)
                        {
                            // Log the error
                            _logger.LogError(ex, "Error parsing FilterCriteria JSON for category {CategoryCode}: {FilterCriteria}",
                                categoryCode, category.FilterCriteria);
                        }
                    }

                    return new SelectList(stations, "StationName", "StationName", selectedValue);

                case "vendor":
                    var vendors = await _vendorService.GetVendorsAsync();
                    return new SelectList(vendors, "VendorID", "Name", selectedValue);

                default:
                    _logger.LogWarning("Unknown DataSource '{DataSource}' for category '{CategoryCode}'",
                        category.DataSource, categoryCode);
                    return new SelectList(Enumerable.Empty<SelectListItem>());
            }
        }

        // CODE FOR AJAX METHOD
        public async Task<List<object>> GetLocationItemsForJsonAsync(string categoryCode = null, string selectedValue = null)
        {
            if (string.IsNullOrEmpty(categoryCode))
                return new List<object>();

            // Get all categories and filter in memory for case-insensitive comparison
            var categories = await _requisitionContext.StationCategories.ToListAsync();
            var category = categories.FirstOrDefault(c =>
                string.Equals(c.Code, categoryCode, StringComparison.OrdinalIgnoreCase));

            if (category == null)
                return new List<object>();

            // Return the appropriate data format based on the data source
            switch (category.DataSource?.ToLower())
            {
                case "department":
                    var departments = await _ktdaContext.Departments.ToListAsync();
                    return departments.Select(d => new { value = d.DepartmentName, text = d.DepartmentName }).ToList<object>();

                case "station":
                    var stations = await _ktdaContext.Stations.ToListAsync();

                    // Apply filters if specified
                    if (!string.IsNullOrEmpty(category.FilterCriteria))
                    {
                        try
                        {
                            var filters = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, List<string>>>(
                                category.FilterCriteria);

                            if (filters != null)
                            {
                                if (filters.ContainsKey("include") && filters["include"] != null)
                                {
                                    stations = stations.Where(s =>
                                        filters["include"].Any(term =>
                                            s.StationName.Contains(term, StringComparison.OrdinalIgnoreCase)))
                                        .ToList();
                                }

                                if (filters.ContainsKey("exclude") && filters["exclude"] != null)
                                {
                                    stations = stations.Where(s =>
                                        !filters["exclude"].Any(term =>
                                            s.StationName.Contains(term, StringComparison.OrdinalIgnoreCase)))
                                        .ToList();
                                }
                            }
                        }
                        catch (JsonException ex)
                        {
                            // Log the error
                            _logger.LogError(ex, "Error parsing FilterCriteria JSON for category {CategoryCode}", categoryCode);
                        }
                    }

                    return stations.Select(s => new { value = s.StationName, text = s.StationName }).ToList<object>();

                case "vendor":
                    var vendors = await _vendorService.GetVendorsAsync();
                    return vendors.Select(v => new { value = v.VendorID.ToString(), text = v.Name }).ToList<object>();

                default:
                    return new List<object>();
            }
        }
        public async Task InitializeUserLocationAsync(Requisition requisition, EmployeeBkp employee, Department department, Station station)
        {
            // Only initialize if values are not already set
            if (!string.IsNullOrEmpty(requisition.IssueStationCategory) && !string.IsNullOrEmpty(requisition.IssueStation))
                return;

            // Determine if user is at HQ/headoffice or factory/region
            if (station != null && station.StationName?.ToLower() == "hq")
            {
                // User is at head office
                requisition.IssueStationCategory = "headoffice";

                if (department != null)
                {
                    requisition.IssueStation = department.DepartmentName;
                }
            }
            else if (station != null)
            {
                // Check if it's a region or factory station
                var stationName = station.StationName?.ToLower() ?? "";

                if (stationName.Contains("region"))
                {
                    requisition.IssueStationCategory = "region";
                    requisition.IssueStation = station.StationName;
                }
                else
                {
                    requisition.IssueStationCategory = "factory";
                    requisition.IssueStation = station.StationName;
                }
            }
        }
    }
}
