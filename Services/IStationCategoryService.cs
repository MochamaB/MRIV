using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MRIV.Models;

namespace MRIV.Services
{
    public interface IStationCategoryService
    {
        Task<List<StationCategory>> GetAllCategoriesAsync();
        Task<SelectList> GetLocationsForCategoryAsync(string categoryCode, string selectedValue = null);
        Task<SelectList> GetStationCategoriesSelectListAsync(string stationPoint);
    }

    public class StationCategoryService : IStationCategoryService
    {
        private readonly KtdaleaveContext _ktdaContext;
        private readonly RequisitionContext _requisitionContext;
        private readonly VendorService _vendorService;

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

        public async Task<SelectList> GetLocationsForCategoryAsync(string categoryCode, string selectedValue = null)
        {
            if (string.IsNullOrEmpty(categoryCode))
                return new SelectList(Enumerable.Empty<SelectListItem>());

            var categories = await GetAllCategoriesAsync();
            var category = categories.FirstOrDefault(c => c.Code.Equals(categoryCode, StringComparison.OrdinalIgnoreCase));

            if (category == null)
                return new SelectList(Enumerable.Empty<SelectListItem>());

            switch (category.DataSource.ToLower())
            {
                case "department":
                    var departments = await _ktdaContext.Departments.ToListAsync();
                    return new SelectList(departments, "DepartmentName", "DepartmentName", selectedValue);

                case "station":
                    var stations = await _ktdaContext.Stations.ToListAsync();

                    // Apply filters if specified
                    if (!string.IsNullOrEmpty(category.FilterCriteria))
                    {
                        var filters = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, List<string>>>(
                            category.FilterCriteria);

                        if (filters.ContainsKey("include"))
                        {
                            stations = stations.Where(s =>
                                filters["include"].Any(term =>
                                    s.StationName.Contains(term, StringComparison.OrdinalIgnoreCase)))
                                .ToList();
                        }

                        if (filters.ContainsKey("exclude"))
                        {
                            stations = stations.Where(s =>
                                !filters["exclude"].Any(term =>
                                    s.StationName.Contains(term, StringComparison.OrdinalIgnoreCase)))
                                .ToList();
                        }
                    }

                    return new SelectList(stations, "StationName", "StationName", selectedValue);

                case "vendor":
                    var vendors = await _vendorService.GetVendorsAsync();
                    return new SelectList(vendors, "VendorID", "Name", selectedValue);

                default:
                    return new SelectList(Enumerable.Empty<SelectListItem>());
            }
        }
    }
}
