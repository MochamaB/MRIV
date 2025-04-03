using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MRIV.Attributes;
using MRIV.Enums;
using MRIV.Extensions;
using MRIV.Models;
using MRIV.Services;
using MRIV.ViewModels;

namespace MRIV.Controllers
{
    [CustomAuthorize]
    public class RequisitionsController : Controller
    {
        private readonly RequisitionContext _context;
        private readonly IEmployeeService _employeeService;
        private readonly IDepartmentService _departmentService;
        private readonly IApprovalService _approvalService;
        private readonly VendorService _vendorService;

        public RequisitionsController(RequisitionContext context, IEmployeeService employeeService, IDepartmentService departmentService,
            IApprovalService approvalService, VendorService vendorService)
        {
            _context = context;
            _employeeService = employeeService;
            _departmentService = departmentService;
            _approvalService = approvalService;
            _vendorService = vendorService;
        }

        // GET: Requisitions
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
            var query = _context.Requisitions.AsQueryable();

            // Create filter view model with explicit type for the array
            ViewBag.Filters = await query.CreateFiltersAsync(
                new Expression<Func<Requisition, object>>[] {
            // Select which properties to create filters for
            r => r.Status,
            r => r.IssueStationCategory,
            r => r.DeliveryStationCategory,
                    // Add other properties as needed
                },
                filters
            );

            // Apply filters to query
            query = query.ApplyFilters(filters);

            // Get total count for pagination
            var totalItems = await query.CountAsync();

            // Apply pagination and ordering
            var requisitions = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Create view models for the paginated data
            var viewModels = new List<RequisitionViewModel>();
            foreach (var requisition in requisitions)
            {
                // Get department and employee details
                var department = await _departmentService.GetDepartmentByIdAsync(requisition.DepartmentId);
                var employee = await _employeeService.GetEmployeeByPayrollAsync(requisition.PayrollNo);

                // Get location names
                var issueLocationName = requisition.IssueStation;
                string deliveryLocationName;

                // Special handling for vendor delivery locations
                if (requisition.DeliveryStationCategory?.ToLower() == "vendor" && !string.IsNullOrEmpty(requisition.DeliveryStation))
                {
                    // Try to get vendor by ID
                    var vendor = await _vendorService.GetVendorByIdAsync(requisition.DeliveryStation);
                    deliveryLocationName = vendor?.Name ?? requisition.DeliveryStation;
                }
                else
                {
                    // For non-vendor locations, use the station name directly
                    deliveryLocationName = requisition.DeliveryStation;
                }

                // Calculate days pending
                var daysPending = requisition.CompleteDate.HasValue ? 0 :
                    (DateTime.Now - (requisition.CreatedAt ?? DateTime.Now)).Days;

                // Use the service to get the most significant approval
                var currentApproval = await _approvalService.GetMostSignificantApprovalAsync(requisition.Id);

                // Get approver details if we found a current approval
                string approverName = "Unknown";
                string approverDesignation = "";
                if (currentApproval != null && !string.IsNullOrEmpty(currentApproval.PayrollNo))
                {
                    var approver = await _employeeService.GetEmployeeByPayrollAsync(currentApproval.PayrollNo);
                    if (approver != null)
                    {
                        approverName = approver.Fullname;
                        approverDesignation = approver.Designation ?? "";
                    }
                }

                // Create view model
                viewModels.Add(new RequisitionViewModel
                {
                    Id = requisition.Id,
                    TicketId = requisition.TicketId,
                    IssueStationCategory = requisition.IssueStationCategory,
                    IssueStation = requisition.IssueStation,
                    DeliveryStationCategory = requisition.DeliveryStationCategory,
                    DeliveryStation = requisition.DeliveryStation,
                    CreatedAt = requisition.CreatedAt,
                    CompleteDate = requisition.CompleteDate,
                    Status = requisition.Status,
                    DepartmentName = department?.DepartmentName ?? "Unknown",
                    EmployeeName = employee?.Fullname ?? "Unknown",
                    IssueLocationName = issueLocationName,
                    DeliveryLocationName = deliveryLocationName,
                    DaysPending = daysPending,
                    // Set approval properties
                    CurrentApprovalStepNumber = currentApproval?.StepNumber,
                    CurrentApprovalStepName = currentApproval?.ApprovalStep,
                    CurrentApproverName = approverName,
                    CurrentApproverDesignation = approverDesignation,
                    CurrentApprovalStatus = currentApproval?.ApprovalStatus ?? ApprovalStatus.NotStarted
                });
            }

            // Create pagination view model
            var paginationModel = new PaginationViewModel
            {
                TotalItems = totalItems,
                ItemsPerPage = pageSize,
                CurrentPage = page,
                Action = "Index",
                Controller = "Requisitions",
                RouteData = filters
            };

            // Pass pagination model to view
            ViewBag.Pagination = paginationModel;

            return View(viewModels);
        }

        // GET: Requisitions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requisition = await _context.Requisitions
                .FirstOrDefaultAsync(m => m.Id == id);
            if (requisition == null)
            {
                return NotFound();
            }

            return View(requisition);
        }

        // GET: Requisitions/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Requisitions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,TicketId,DepartmentId,PayrollNo,IssueStationCategory,IssueStation,DeliveryStationCategory,DeliveryStation,Remarks,DispatchType,DispatchPayrollNo,DispatchVendor,CollectorName,CollectorId,Status,CompleteDate,CreatedAt,UpdatedAt,IsExternal,ForwardToAdmin")] Requisition requisition)
        {
            if (ModelState.IsValid)
            {
                _context.Add(requisition);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(requisition);
        }

        // GET: Requisitions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requisition = await _context.Requisitions.FindAsync(id);
            if (requisition == null)
            {
                return NotFound();
            }
            return View(requisition);
        }

        // POST: Requisitions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,TicketId,DepartmentId,PayrollNo,IssueStationCategory,IssueStation,DeliveryStationCategory,DeliveryStation,Remarks,DispatchType,DispatchPayrollNo,DispatchVendor,CollectorName,CollectorId,Status,CompleteDate,CreatedAt,UpdatedAt,IsExternal,ForwardToAdmin")] Requisition requisition)
        {
            if (id != requisition.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(requisition);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RequisitionExists(requisition.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(requisition);
        }

        // GET: Requisitions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requisition = await _context.Requisitions
                .FirstOrDefaultAsync(m => m.Id == id);
            if (requisition == null)
            {
                return NotFound();
            }

            return View(requisition);
        }

        // POST: Requisitions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var requisition = await _context.Requisitions.FindAsync(id);
            if (requisition != null)
            {
                _context.Requisitions.Remove(requisition);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RequisitionExists(int id)
        {
            return _context.Requisitions.Any(e => e.Id == id);
        }
    }
}
