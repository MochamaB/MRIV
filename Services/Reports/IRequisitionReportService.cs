using MRIV.Models;
using System.Threading.Tasks;
using MRIV.ViewModels.Reports.Requisition;
using MRIV.ViewModels.Reports.Filters;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using MRIV.Services;

namespace MRIV.Services.Reports
{
    public interface IRequisitionReportService
    {
        Task<RequisitionsByLocationViewModel> GetRequisitionsByLocation(RequisitionReportFilterViewModel filters, string userPayrollNo);
    }
    public class RequisitionReportService : IRequisitionReportService
    {
        private readonly RequisitionContext _context;
        private readonly IVisibilityAuthorizeService _visibilityService;
        private readonly IDepartmentService _departmentService;
        public RequisitionReportService(RequisitionContext context, IVisibilityAuthorizeService visibilityService, IDepartmentService departmentService)
        {
            _context = context;
            _visibilityService = visibilityService;
            _departmentService = departmentService;
        }

        public async Task<RequisitionsByLocationViewModel> GetRequisitionsByLocation(RequisitionReportFilterViewModel filters,string userPayrollNo)
        {

            var query = _context.Requisitions.AsQueryable();
            query = await _visibilityService.ApplyVisibilityScopeAsync(query, userPayrollNo);
            // Now apply additional filters (DepartmentId, StationId, Status, etc.)

                var byDepartment = query
                 .GroupBy(r => r.DepartmentId)
                 .Select(g => new {
                     DepartmentId = g.Key,
                     Count = g.Count(),
                     Statuses = g.Select(r => r.Status)
                 })
                 .ToList() // Materialize here!
                 .Select(g => new RequisitionsByLocationViewModel.DepartmentSummary
                 {
                     DepartmentId = g.DepartmentId,
                     Count = g.Count,
                     StatusCounts = g.Statuses
                         .GroupBy(s => s)
                         .ToDictionary(sg => sg.Key.ToString(), sg => sg.Count())
                 })
                 .ToList();
                    var byStation = query
                        .GroupBy(r => r.DeliveryStationId)
                        .Select(g => new
                        {
                            StationId = g.Key,
                            Count = g.Count(),
                            Statuses = g.Select(r => r.Status)
                        })
                        .ToList() // Materialize the query here!
                        .Select(g => new RequisitionsByLocationViewModel.StationSummary
                        {
                            StationId = g.StationId,
                            Count = g.Count,
                            StatusCounts = g.Statuses
                                .GroupBy(s => s)
                                .ToDictionary(sg => sg.Key.ToString(), sg => sg.Count())
                        })
                        .ToList();

            // Enrich with department and station names
            foreach (var dept in byDepartment)
            {
                var department = await _departmentService.GetDepartmentByIdAsync(dept.DepartmentId);
                dept.DepartmentName = department?.DepartmentName ?? $"Department {dept.DepartmentId}";
            }

            foreach (var station in byStation)
            {
                var stationInfo = await _departmentService.GetStationByIdAsync(station.StationId);
                station.StationName = stationInfo?.StationName ?? $"Station {station.StationId}";
            }

            return new RequisitionsByLocationViewModel
            {
                ByDepartment = byDepartment,
                ByStation = byStation
            };
        }
    }
}