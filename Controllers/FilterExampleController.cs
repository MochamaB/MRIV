using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MRIV.Extensions;
using MRIV.Models;
using MRIV.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MRIV.Controllers
{
    public class FilterExampleController : Controller
    {
        private readonly RequisitionContext _context;

        public FilterExampleController(RequisitionContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Get filter values from request query string
            var filters = new Dictionary<string, string>();
            foreach (var key in Request.Query.Keys)
            {
                filters[key] = Request.Query[key];
            }

            // Create base query
            var query = _context.Requisitions
              //  .Include(r => r.Department)
                .AsQueryable();

            // Create filter view model with explicit type for the array
            ViewBag.Filters = await query.CreateFiltersAsync(
                new Expression<Func<Requisition, object>>[] {
                    // Select which properties to create filters for
                    r => r.Status,
                    r => r.IssueStationCategory,
                    r => r.DeliveryStationCategory,
                 //   r => r.Department.DepartmentName
                },
                filters
            );

            // Apply filters to query
            query = query.ApplyFilters(filters);

            // Apply any other filtering, sorting, paging, etc.
            var requisitions = await query
                .OrderByDescending(r => r.CreatedAt)
                .Take(100)
                .ToListAsync();

            return View("~/Views/Shared/_FilterUsageExample.cshtml", requisitions);
        }
    }
}
