using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MRIV.Attributes;
using MRIV.Models;
using MRIV.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace MRIV.Controllers
{
    [CustomAuthorize]
    public class LocationController : Controller
    {
        private readonly ILocationService _locationService;
        private readonly IStationCategoryService _stationCategoryService;

        public LocationController(ILocationService locationService, IStationCategoryService stationCategoryService)
        {
            _locationService = locationService;
            _stationCategoryService = stationCategoryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetDepartmentsSelectList(string selectedId = null)
        {
            try
            {
                // Use StationCategoryService to get departments
                var result = await _stationCategoryService.GetLocationItemsForJsonAsync("headoffice", selectedId);
                
                // Log the result for debugging
                Console.WriteLine($"GetDepartmentsSelectList result count: {result?.Count ?? 0}");
                
                return Json(result);
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error in GetDepartmentsSelectList: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Return an empty list instead of an error
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetStationsSelectList(string category = "factory", int? selectedId = null, int? filterByDepartmentId = null)
        {
            try
            {
                // Use StationCategoryService to get stations with the specified category
                var result = await _stationCategoryService.GetLocationItemsForJsonAsync(category, selectedId?.ToString());
                
                // Log the result for debugging
                Console.WriteLine($"GetStationsSelectList for category '{category}' result count: {result?.Count ?? 0}");
                
                return Json(result);
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error in GetStationsSelectList: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Return an empty list instead of an error
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLocationsForCategory(string category, string selectedValue = null)
        {
            try
            {
                if (string.IsNullOrEmpty(category))
                {
                    return Json(new List<object>());
                }

                var result = await _stationCategoryService.GetLocationItemsForJsonAsync(category, selectedValue);
                return Json(result);
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error in GetLocationsForCategory: {ex.Message}");
                
                // Return an empty list instead of an error
                return Json(new List<object>());
            }
        }
    }
}
