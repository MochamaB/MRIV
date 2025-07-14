namespace MRIV.ViewModels.Reports.Filters
{
    public class ReportFilterDefinition
    {
        public string PropertyName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string FilterType { get; set; } = "dropdown"; // dropdown, text, date, etc.
        public List<ReportFilterOption> Options { get; set; } = new();
        public string? DefaultValue { get; set; }
        public bool IsMultiSelect { get; set; } = false;
        public bool IsVisible { get; set; } = true;
    }
} 