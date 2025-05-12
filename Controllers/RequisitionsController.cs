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
        private readonly KtdaleaveContext _ktdaleavecontext;
        private readonly IEmployeeService _employeeService;
        private readonly IDepartmentService _departmentService;
        private readonly IApprovalService _approvalService;
        private readonly VendorService _vendorService;
        private readonly ILocationService _locationService;

        public RequisitionsController(RequisitionContext context, IEmployeeService employeeService, IDepartmentService departmentService,
            IApprovalService approvalService, VendorService vendorService, KtdaleaveContext ktdaleavecontext, ILocationService location)
        {
            _context = context;
            _employeeService = employeeService;
            _departmentService = departmentService;
            _approvalService = approvalService;
            _vendorService = vendorService;
            _ktdaleavecontext = ktdaleavecontext;
            _locationService = location;

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
                // Skip if the value is null or empty
                if (!string.IsNullOrEmpty(Request.Query[key]))
                {
                    filters[key] = Request.Query[key];
                }
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

                // Get Issue location names
                var issueStation = await _locationService.GetStationByIdAsync(requisition.IssueStationId);
                var issueDepartment = await _locationService.GetDepartmentByIdAsync(requisition.IssueDepartmentId);
                string deliveryStationName;

                // Get Delivery Department Location names
                var deliveryStation = await _locationService.GetStationByIdAsync(requisition.DeliveryStationId);
                var deliveryDepartment = await _locationService.GetDepartmentByIdAsync(requisition.DeliveryDepartmentId);

                // Special handling for vendor delivery locations
                if (requisition.DeliveryStationCategory?.ToLower() == "vendor" && requisition.DeliveryStationId != 0)
                {
                    // Try to get vendor by ID
                    var vendor = await _vendorService.GetVendorByIdAsync(requisition.DeliveryStationId.ToString());
                    deliveryStationName = vendor?.Name ?? "Unknown Vendor";
                }
                else
                {
                    // For non-vendor locations, use the station name directly
                    // For non-vendor locations, use the station name directly
                   
                    deliveryStationName = deliveryStation?.StationName ?? "Unknown Station";
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

                // Create view model with proper null checks
                viewModels.Add(new RequisitionViewModel
                {
                    Id = requisition.Id,
                    TicketId = requisition.TicketId,
                    IssueStationCategory = requisition.IssueStationCategory,
                    IssueStation = issueStation?.StationName ?? "Unknown Station",
                    IssueDepartment = issueDepartment.DepartmentName ?? "Unknown Department",
                    DeliveryStationCategory = requisition?.DeliveryStationCategory,
                    DeliveryStation = deliveryStation?.StationName ?? "Unknown Station",
                    DeliveryDepartment = deliveryDepartment.DepartmentName ?? "Unknown Department",
                    CreatedAt = requisition.CreatedAt,
                    CompleteDate = requisition.CompleteDate,
                    Status = requisition.Status,
                    DepartmentName = department?.DepartmentName ?? "Unknown",
                    EmployeeName = employee?.Fullname ?? "Unknown",
                    DeliveryLocationName = department?.DepartmentName ?? "Unknown",
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
             .Include(r => r.RequisitionItems)
                 .ThenInclude(ri => ri.Material)
                     .ThenInclude(m => m.MaterialCategory)  // Include MaterialCategory if needed
             .Include(r => r.RequisitionItems)
                 .ThenInclude(ri => ri.Material)
                     .ThenInclude(m => m.MaterialSubcategory)  // Directly include MaterialSubcategory from Material
             .Include(r => r.Approvals.OrderBy(a => a.StepNumber))
             .FirstOrDefaultAsync(m => m.Id == id);

            if (requisition == null)
            {
                return NotFound();
            }
            // Get Issue location names
            var issueStation = await _locationService.GetStationByIdAsync(requisition.IssueStationId);
            var issueDepartment = await _locationService.GetDepartmentByIdAsync(requisition.IssueDepartmentId);

            // Get Delivery Department Location names
            var deliveryStation = await _locationService.GetStationByIdAsync(requisition.DeliveryStationId);
            var deliveryDepartment = await _locationService.GetDepartmentByIdAsync(requisition.DeliveryDepartmentId);

            // Create the view model
            var viewModel = new RequisitionDetailsViewModel
            {
                Requisition = requisition,
                EmployeeDetail = await _employeeService.GetEmployeeByPayrollAsync(requisition.PayrollNo),
                DepartmentDetail = await _departmentService.GetDepartmentByIdAsync(requisition.DepartmentId),
                RequisitionItems = requisition.RequisitionItems?.ToList(),
                IssueStation = requisition.IssueStationId == 0 ? "HQ": (await _locationService.GetStationByIdAsync(requisition.IssueStationId))?.StationName ?? "Unknown Station",
                IssueDepartment = issueDepartment?.DepartmentName ?? "Unknown Department",
                DeliveryStation = requisition.DeliveryStationId == 0 ? "HQ": (await _locationService.GetStationByIdAsync(requisition.DeliveryStationId))?.StationName ?? "Unknown Station",
                DeliveryDepartment = deliveryDepartment?.DepartmentName ?? "Unknown Department",
            };



            //  var Station = await _locationService.GetStationByIdAsync(requisition.DeliveryStationId);
            //   var Department = await _locationService.GetDepartmentByIdAsync(requisition.DeliveryDepartmentId);
            // Get issue and delivery station details
            if (requisition.IssueStationId != 0)
            {
               
                if (requisition.DeliveryStationId != 0)
                {
                    if (requisition.DispatchType?.ToLower() == "vendor")
                    {
                        viewModel.Vendor = await _vendorService.GetVendorByIdAsync(requisition.DispatchVendor);
                    }
                    else
                    {
                        viewModel.DispatchEmployee = await _employeeService.GetEmployeeByPayrollAsync(requisition.DispatchPayrollNo);

                    }
                }

              
            }

            // Convert approval steps to view models
            var vendors = await _vendorService.GetVendorsAsync();
            if (requisition.Approvals != null)
            {
                viewModel.ApprovalSteps = await _approvalService.ConvertToViewModelsAsync(requisition.Approvals.ToList(), requisition, vendors);
            }

            return View(viewModel);
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
