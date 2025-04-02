using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace MRIV.ViewModels
{
    public class FilterViewModel
    {
        public List<FilterDefinition> Filters { get; set; } = new List<FilterDefinition>();
    }

    public class FilterDefinition
    {
        public string PropertyName { get; set; }
        public string DisplayName { get; set; }
        public List<SelectListItem> Options { get; set; } = new List<SelectListItem>();
    }
}
