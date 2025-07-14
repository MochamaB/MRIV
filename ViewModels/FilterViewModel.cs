using Microsoft.AspNetCore.Mvc.Rendering;
using MRIV.ViewModels.Reports.Filters;
using System.Collections.Generic;

namespace MRIV.ViewModels
{
    public class FilterViewModel
    {
        public List<FilterDefinition> Filters { get; set; } = new List<FilterDefinition>();
        public List<ReportFilterDefinition> ReportFilters { get; set; } = new();
        public Dictionary<string, string> SelectedValues { get; set; } = new();
    }

    public class FilterDefinition
    {
        public string PropertyName { get; set; }
        public string DisplayName { get; set; }
        public List<SelectListItem> Options { get; set; } = new List<SelectListItem>();
    }
}
