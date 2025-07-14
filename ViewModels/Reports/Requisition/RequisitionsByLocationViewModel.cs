using System.Collections.Generic;

namespace MRIV.ViewModels.Reports.Requisition
{
    public class RequisitionsByLocationViewModel
    {
        public List<DepartmentSummary> ByDepartment { get; set; } = new();
        public List<StationSummary> ByStation { get; set; } = new();

        public class DepartmentSummary
        {
            public int DepartmentId { get; set; }
            public string DepartmentName { get; set; } = "Unknown";
            public int Count { get; set; }
            public Dictionary<string, int> StatusCounts { get; set; } = new();
        }

        public class StationSummary
        {
            public int StationId { get; set; }
            public string StationName { get; set; } = "Unknown";
            public int Count { get; set; }
            public Dictionary<string, int> StatusCounts { get; set; } = new();
        }
    }
} 