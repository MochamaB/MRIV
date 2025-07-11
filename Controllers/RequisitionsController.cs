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
        private readonly IVisibilityAuthorizeService _visibilityService;

        public RequisitionsController(RequisitionContext context, IEmployeeService employeeService, IDepartmentService departmentService,
            IApprovalService approvalService, VendorService vendorService, KtdaleaveContext ktdaleavecontext, ILocationService location, IVisibilityAuthorizeService visibilityService)
        {
            _context = context;
            _employeeService = employeeService;
            _departmentService = departmentService;
            _approvalService = approvalService;
            _vendorService = vendorService;
            _ktdaleavecontext = ktdaleavecontext;
            _locationService = location;
            _visibilityService = visibilityService;
        }

        // GET: Requisitions
        public async Task<IActionResult> Index(string tab = "pendingreceipt", int page = 1, int pageSize = 10, string searchTerm = "")
        {
            // Ensure valid pagination parameters
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : (pageSize > 100 ? 100 : pageSize);

            // Get filter values from request query string
            var filters = new Dictionary<string, string>();
            foreach (var key in Request.Query.Keys.Where(k => k != "page" && k != "pageSize" && k != "searchTerm" && k != "tab"))
            {
                if (!string.IsNullOrEmpty(Request.Query[key]))
                {
                    filters[key] = Request.Query[key];
                }
            }

            // Get current user's payroll number from session
            var userPayrollNo = HttpContext.Session.GetString("EmployeePayrollNo");
            if (string.IsNullOrEmpty(userPayrollNo))
                return RedirectToAction("Index", "Login");

            // Create base query
            var query = _context.Requisitions.AsQueryable();

            // 1. Apply visibility scope (new logic)
            query = await _visibilityService.ApplyVisibilityScopeAsync(query, userPayrollNo);

            // 2. Apply tab logic (group by status)
            switch (tab.ToLower())
            {
                case "notstarted":
                    query = query.Where(r => r.Status == RequisitionStatus.NotStarted);
                    break;
                case "pendingdispatch":
                    query = query.Where(r => r.Status == RequisitionStatus.PendingDispatch);
                    break;
                case "pendingreceipt":
                    query = query.Where(r => r.Status == RequisitionStatus.PendingReceipt);
                    break;
                case "cancelled":
                    query = query.Where(r => r.Status == RequisitionStatus.Cancelled);
                    break;
                case "completed":
                    query = query.Where(r => r.Status == RequisitionStatus.Completed);
                    break;
                case "all":
                default:
                    // No additional filter
                    break;
            }

            // 3. Apply search and filters to the full visible dataset
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.Trim().ToLower();
                query = query.Where(r =>
                    (r.TicketId.ToString().Contains(searchTerm)) ||
                    (r.IssueStationCategory != null && r.IssueStationCategory.ToLower().Contains(searchTerm)) ||
                    (r.DeliveryStationCategory != null && r.DeliveryStationCategory.ToLower().Contains(searchTerm)) ||
                    (r.Status != null && r.Status.ToString().ToLower().Contains(searchTerm))
                );
            }

            // Apply additional filters if any
            query = query.ApplyFilters(filters);

            // 4. Get total count for pagination after filtering
            var totalItems = await query.CountAsync();

            // 5. Apply pagination and ordering
            var requisitions = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // 6. Create view models for the paginated data
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
                    var vendor = await _vendorService.GetVendorByIdAsync(requisition.DeliveryStationId.ToString());
                    deliveryStationName = vendor?.Name ?? "Unknown Vendor";
                }
                else
                {
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

            // 7. Create pagination view model
            var paginationModel = new PaginationViewModel
            {
                TotalItems = totalItems,
                ItemsPerPage = pageSize,
                CurrentPage = page,
                Action = "Index",
                Controller = "Requisitions",
                RouteData = filters
            };

            // 8. Pass tab, pagination, and search info to the view
            ViewBag.Pagination = paginationModel;
            ViewBag.Tab = tab;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;
            ViewBag.CurrentPage = page;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.Filters = filters;

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
