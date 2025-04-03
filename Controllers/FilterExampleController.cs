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

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            // Ensure valid pagination parameters
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : (pageSize > 100 ? 100 : pageSize);

            // Get filter values from request query string
            var filters = new Dictionary<string, string>();
            foreach (var key in Request.Query.Keys.Where(k => k != "page" && k != "pageSize"))
            {
                filters[key] = Request.Query[key];
            }

            // Create base query
            var query = _context.Requisitions
            //    .Include(r => r.Department)
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

            // Get total count for pagination
            var totalItems = await query.CountAsync();

            // Apply pagination
            var requisitions = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Create pagination view model
            var paginationModel = new PaginationViewModel
            {
                TotalItems = totalItems,
                ItemsPerPage = pageSize,
                CurrentPage = page,
                Action = "Index",
                Controller = "FilterExample",
                RouteData = filters
            };

            // Pass pagination model to view
            ViewBag.Pagination = paginationModel;

            return View("~/Views/Shared/_FilterUsageExample.cshtml", requisitions);
        }
    }
}
