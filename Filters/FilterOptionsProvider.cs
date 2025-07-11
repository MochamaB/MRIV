using Microsoft.AspNetCore.Mvc.Rendering;
using MRIV.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MRIV.Filters
{
    public interface IFilterOptionsProvider
    {
        Task<MaterialFilterOptions> GetMaterialFilterOptionsAsync();
        Task<RequisitionFilterOptions> GetRequisitionFilterOptionsAsync();
    }

    public class FilterOptionsProvider : IFilterOptionsProvider
    {
        // Replace with your actual context/services as needed
        // private readonly AppDbContext _context;

        public FilterOptionsProvider(/*AppDbContext context*/)
        {
            // _context = context;
        }

        public async Task<MaterialFilterOptions> GetMaterialFilterOptionsAsync()
        {
            // Example static/mock data
            return new MaterialFilterOptions
            {
                StatusOptions = Enum.GetValues<MaterialStatus>()
                    .Select(s => new SelectListItem(s.ToString(), ((int)s).ToString())),
                CategoryOptions = new List<SelectListItem> { new SelectListItem("Category1", "1"), new SelectListItem("Category2", "2") }
            };
        }

        public async Task<RequisitionFilterOptions> GetRequisitionFilterOptionsAsync()
        {
            // Example: RequisitionType from enum
            var typeOptions = Enum.GetValues(typeof(RequisitionType))
                .Cast<RequisitionType>()
                .Select(t => new SelectListItem(t.ToString(), ((int)t).ToString()))
                .ToList();

            // Example: DispatchType static
            var dispatchTypeOptions = new List<SelectListItem>
            {
                new SelectListItem("admin", "admin"),
                new SelectListItem("vendor", "vendor")
            };

            // TODO: Add station and department options from your services
            var stationOptions = new List<SelectListItem> { new SelectListItem("HQ", "0"), new SelectListItem("Factory 1", "001") };
            var departmentOptions = new List<SelectListItem> { new SelectListItem("ICT", "114"), new SelectListItem("HR", "101") };

            return new RequisitionFilterOptions
            {
                TypeOptions = typeOptions,
                DispatchTypeOptions = dispatchTypeOptions,
                StationOptions = stationOptions,
                DepartmentOptions = departmentOptions
            };
        }
    }

    public class MaterialFilterOptions
    {
        public IEnumerable<SelectListItem> StatusOptions { get; set; }
        public IEnumerable<SelectListItem> CategoryOptions { get; set; }
        public IEnumerable<SelectListItem> SubcategoryOptions { get; set; }
    }

    public class RequisitionFilterOptions
    {
        public IEnumerable<SelectListItem> TypeOptions { get; set; }
        public IEnumerable<SelectListItem> DispatchTypeOptions { get; set; }
        public IEnumerable<SelectListItem> StationOptions { get; set; }
        public IEnumerable<SelectListItem> DepartmentOptions { get; set; }
    }
}
