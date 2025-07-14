using MRIV.ViewModels.Reports.Filters;
using System.Collections.Generic;

namespace MRIV.Services.Reports
{
    public interface IReportFilterService
    {
        Task<List<ReportFilterDefinition>> GetRequisitionByLocationFiltersAsync(string userPayrollNo, RequisitionReportFilterViewModel selectedFilters = null);
        
           
        // Add similar methods for other reports/modules as needed
    }
} 