using Microsoft.AspNetCore.Mvc.Rendering;
using MRIV.Models;
using MRIV.ViewModels;

namespace MRIV.ViewModels
{
    public class MaterialSearchResult
    {
        public List<Material> Materials { get; set; }
        public int TotalCount { get; set; }
    }

    public class MaterialsIndexViewModel
    {
        public IEnumerable<Material> Materials { get; set; }
        public PaginationViewModel Pagination { get; set; }
        public FilterViewModel Filters { get; set; }

        // Lookup dictionaries
        public Dictionary<int, string> StationNames { get; set; } = new();
        public Dictionary<int, string> DepartmentNames { get; set; } = new();
        public Dictionary<string, string> VendorNames { get; set; } = new();
        public Dictionary<string, (string Name, string Designation)> EmployeeInfo { get; set; } = new();
        public Dictionary<int, MaterialCondition> MaterialConditions { get; set; } = new();

        // For media handling
        public Dictionary<int, string> MaterialImageUrls { get; set; } = new();
        public Dictionary<int, string> CategoryImageUrls { get; set; } = new();
    }
}