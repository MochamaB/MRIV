using MRIV.ViewModels.Reports.Filters;
using MRIV.Models;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace MRIV.Services.Reports
{
    public class ReportFilterService : IReportFilterService
    {
        private readonly RequisitionContext _context;
        private readonly IVisibilityAuthorizeService _visibilityService;
        public ReportFilterService(RequisitionContext context, IVisibilityAuthorizeService visibilityService)
        {
            _context = context;
            _visibilityService = visibilityService;
        }


        public async Task<List<ReportFilterDefinition>> GetRequisitionByLocationFiltersAsync(string userPayrollNo, RequisitionReportFilterViewModel selectedFilters = null)
        {
            var visibleDepartments = await _visibilityService.GetVisibleDepartmentsAsync(userPayrollNo);
            var visibleStations = await _visibilityService.GetVisibleStationsAsync(userPayrollNo);

            var statusOptions = System.Enum.GetValues(typeof(MRIV.Enums.RequisitionStatus))
                .Cast<MRIV.Enums.RequisitionStatus>()
                .Select(s => new ReportFilterOption { Value = ((int)s).ToString(), Text = s.ToString() })
                .ToList();

            var filters = new List<ReportFilterDefinition>
                {
                    new ReportFilterDefinition
                    {
                        PropertyName = "DepartmentId",
                        DisplayName = "Department",
                        FilterType = "dropdown",
                        Options = visibleDepartments.Select(d => new ReportFilterOption
                        {
                            Value = d.DepartmentCode.ToString(),
                            Text = d.DepartmentName,
                            Selected = selectedFilters?.DepartmentId.HasValue == true && selectedFilters.DepartmentId.Value == d.DepartmentCode
                        }).ToList()
                    },
                    new ReportFilterDefinition
                    {
                        PropertyName = "StationId",
                        DisplayName = "Station",
                        FilterType = "dropdown",
                        Options = visibleStations.Select(s => new ReportFilterOption
                        {
                            Value = s.StationId.ToString(),
                            Text = s.StationName ?? "",
                            Selected = selectedFilters?.StationId.HasValue == true && selectedFilters.StationId.Value == s.StationId
                        }).ToList()
                    },
                    new ReportFilterDefinition
                    {
                        PropertyName = "Status",
                        DisplayName = "Status",
                        FilterType = "dropdown",
                        Options = statusOptions.Select(s => new ReportFilterOption
                        {
                            Value = s.Value,
                            Text = s.Text,
                            Selected = selectedFilters?.Status.HasValue == true && selectedFilters.Status.Value.ToString() == s.Value
                        }).ToList()
                    }
                };

            return filters;
        }
    }
} 