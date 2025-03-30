using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MRIV.Enums;
using MRIV.Models;
using MRIV.Services;
using MRIV.ViewModels;

namespace MRIV.Controllers
{
    public class ApprovalsController : Controller
    {
        private readonly RequisitionContext _context;
        private readonly IEmployeeService _employeeService;
        private readonly VendorService _vendorService;
        private readonly IDepartmentService _departmentService;
        private readonly IApprovalService _approvalService;
        private readonly ILogger<ApprovalService> _logger;

        public ApprovalsController(RequisitionContext context, IEmployeeService employeeService, VendorService vendorService,
            IApprovalService approvalService, IConfiguration configuration, IDepartmentService departmentService, ILogger<ApprovalService> logger)
        {
            _context = context;
            _employeeService = employeeService;
            _vendorService = vendorService;
            _approvalService = approvalService;
            _departmentService = departmentService;
            _logger = logger;

        }

        // GET: Approvals
        public async Task<IActionResult> Index()
        {
            var approvals = await _context.Approvals
                .Include(a => a.Requisition)
                .OrderBy(a => a.RequisitionId)
                .ThenBy(a => a.StepNumber)
                .ToListAsync();

            // Convert approvals to view models
            var viewModels = new List<ApprovalStepViewModel>();

            foreach (var approval in approvals)
            {
                // Get employee and department information
                var employee = await _employeeService.GetEmployeeByPayrollAsync(approval.PayrollNo);
                var department = await _departmentService.GetDepartmentByIdAsync(approval.DepartmentId);

                // Create the view model
                viewModels.Add(new ApprovalStepViewModel
                {
                    // Existing approval properties
                    Id = approval.Id, // You may need to add this property to your view model
                    RequisitionId = approval.RequisitionId, // You may need to add this property
                    StepNumber = approval.StepNumber,
                    ApprovalStep = approval.ApprovalStep,
                    PayrollNo = approval.PayrollNo,
                    EmployeeName = employee?.Fullname ?? "Unknown",
                    DepartmentId = approval.DepartmentId,
                    DepartmentName = department?.DepartmentName ?? "Unknown Department",
                    ApprovalStatus = approval.ApprovalStatus,
                    CreatedAt = approval.CreatedAt,

                    // Add employee designation if needed
                    EmployeeDesignation = employee?.Designation ?? "" // You may need to add this property
                });
            }

            return View(viewModels);
        }

        // GET: Approvals/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var approval = await _context.Approvals
                .Include(a => a.Requisition)
                .Include(a => a.StepConfig)
                .Include(a => a.WorkflowConfig)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (approval == null)
            {
                return NotFound();
            }

            return View(approval);
        }

        // GET: Approvals/Create
        public IActionResult Create()
        {
            ViewData["RequisitionId"] = new SelectList(_context.Requisitions, "Id", "DeliveryStation");
            ViewData["StepConfigId"] = new SelectList(_context.WorkflowStepConfigs, "Id", "ApproverRole");
            ViewData["WorkflowConfigId"] = new SelectList(_context.WorkflowConfigs, "Id", "DeliveryStationCategory");
            return View();
        }

        // POST: Approvals/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,RequisitionId,WorkflowConfigId,StepConfigId,DepartmentId,StepNumber,ApprovalStep,PayrollNo,ApprovalStatus,Comments,IsAutoGenerated,CreatedAt,UpdatedAt")] Approval approval)
        {
            if (ModelState.IsValid)
            {
                _context.Add(approval);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["RequisitionId"] = new SelectList(_context.Requisitions, "Id", "DeliveryStation", approval.RequisitionId);
            ViewData["StepConfigId"] = new SelectList(_context.WorkflowStepConfigs, "Id", "ApproverRole", approval.StepConfigId);
            ViewData["WorkflowConfigId"] = new SelectList(_context.WorkflowConfigs, "Id", "DeliveryStationCategory", approval.WorkflowConfigId);
            return View(approval);
        }

        // GET: Approvals/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var approval = await _context.Approvals
                .Include(a => a.Requisition)
                .Include(a => a.WorkflowConfig)
                .Include(a => a.StepConfig)
                .FirstOrDefaultAsync(a => a.Id == id);
                
            if (approval == null)
            {
                return NotFound();
            }
            
            // Check if the approval status allows editing (only NotStarted, PendingApproval, or OnHold)
            if ((int)approval.ApprovalStatus != (int)ApprovalStatus.NotStarted && 
                (int)approval.ApprovalStatus != (int)ApprovalStatus.PendingApproval && 
                (int)approval.ApprovalStatus != (int)ApprovalStatus.OnHold)
            {
                TempData["ErrorMessage"] = $"Cannot edit approver for step with status '{approval.ApprovalStatus}'. Only steps with status 'Not Started', 'Pending Approval', or 'On Hold' can be edited.";
                return RedirectToAction(nameof(Index));
            }
            
            // Get the requisition for additional context
            var requisition = await _context.Requisitions.FindAsync(approval.RequisitionId);
            if (requisition == null)
            {
                return NotFound();
            }
            
            // Get the current user's employee record
            var loggedInUserEmployee = await _employeeService.GetEmployeeByPayrollAsync(User.Identity.Name);
            
            // Get vendors (needed for vendor approval steps)
            var vendors = await _vendorService.GetVendorsAsync();
            
            // Convert the approval to a view model
            var approvalSteps = new List<Approval> { approval };
            var viewModels = await _approvalService.ConvertToViewModelsAsync(approvalSteps, requisition, vendors);
            var viewModel = viewModels.FirstOrDefault();
            
            if (viewModel == null)
            {
                return NotFound();
            }
            
            // Get department employees for the dropdown
            var departmentEmployees = await _approvalService.PopulateDepartmentEmployeesAsync(requisition, approvalSteps);
            ViewBag.DepartmentEmployees = departmentEmployees.ContainsKey(approval.ApprovalStep) 
                ? departmentEmployees[approval.ApprovalStep] 
                : new SelectList(new List<object>());
            
            // Set additional ViewBag data
            ViewBag.RequisitionNumber = requisition.Id;
            ViewBag.Comments = approval.Comments;
            
            return View(viewModel);
        }

        // POST: Approvals/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ApprovalStepViewModel viewModel)
        {
            // Get the existing approval from database
            var approval = await _context.Approvals
                .Include(a => a.Requisition)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (approval == null)
            {
                TempData["ErrorMessage"] = "Approval not found.";
                return RedirectToAction(nameof(Index));
            }

            // Check if status allows editing
            if ((int)approval.ApprovalStatus != (int)ApprovalStatus.NotStarted &&
                (int)approval.ApprovalStatus != (int)ApprovalStatus.PendingApproval &&
                (int)approval.ApprovalStatus != (int)ApprovalStatus.OnHold)
            {
                TempData["ErrorMessage"] = $"Cannot edit approver with status '{approval.ApprovalStatus}'.";
                return RedirectToAction(nameof(Index));
            }

            // Validate the input
            if (string.IsNullOrEmpty(viewModel.PayrollNo))
            {
                ModelState.AddModelError("PayrollNo", "Approver must be selected");

                // Prepare view data for redisplay
                var requisition = await _context.Requisitions.FindAsync(approval.RequisitionId);
                var vendors = await _vendorService.GetVendorsAsync();
                var approvalSteps = new List<Approval> { approval };

                // Convert back to view model and set up ViewBag data
                var viewModels = await _approvalService.ConvertToViewModelsAsync(approvalSteps, requisition, vendors);
                ViewBag.DepartmentEmployees = await _approvalService.PopulateDepartmentEmployeesAsync(requisition, approvalSteps);
                ViewBag.RequisitionNumber = requisition.Id;

                return View(viewModels.FirstOrDefault());
            }

            // Update the approval
            try
            {
                // Update approval with new payroll number
                approval.PayrollNo = viewModel.PayrollNo;
                approval.UpdatedAt = DateTime.Now;

                // If this is an employee approver, update department
                if (approval.ApprovalStep != "Vendor Dispatch")
                {
                    var employee = await _employeeService.GetEmployeeByPayrollAsync(viewModel.PayrollNo);
                    if (employee != null && !string.IsNullOrEmpty(employee.Department))
                    {
                        approval.DepartmentId = int.Parse(employee.Department);
                    }
                }

                _context.Update(approval);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Approver updated successfully";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating approver: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Approvals/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var approval = await _context.Approvals
                .Include(a => a.Requisition)
                .Include(a => a.StepConfig)
                .Include(a => a.WorkflowConfig)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (approval == null)
            {
                return NotFound();
            }

            return View(approval);
        }

        // POST: Approvals/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var approval = await _context.Approvals.FindAsync(id);
            if (approval != null)
            {
                _context.Approvals.Remove(approval);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ApprovalExists(int id)
        {
            return _context.Approvals.Any(e => e.Id == id);
        }

        // GET: Approvals/Approve/5
        public async Task<IActionResult> Approve(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var approval = await _context.Approvals
                .Include(a => a.Requisition)
                .Include(a => a.StepConfig)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (approval == null)
            {
                return NotFound();
            }

            // Check if the approval is in a state that can be processed
            if (approval.ApprovalStatus != ApprovalStatus.PendingApproval && 
                approval.ApprovalStatus != ApprovalStatus.PendingDispatch && 
                approval.ApprovalStatus != ApprovalStatus.PendingReceive)
            {
                TempData["ErrorMessage"] = $"This approval step is not in a pending state. Current status: {approval.ApprovalStatus}";
                return RedirectToAction(nameof(Index));
            }

            // Get approval history for this requisition
            var approvalHistory = await _approvalService.GetApprovalHistoryAsync(approval.RequisitionId);

            // Get available status options for this approval step
            var statusOptions = await _approvalService.GetAvailableStatusOptionsAsync(approval.Id);
            ViewBag.StatusOptions = statusOptions;

            // Create the view model
            var viewModel = new ApprovalActionViewModel
            {
                Id = approval.Id,
                RequisitionId = approval.RequisitionId,
                ApprovalStep = approval.ApprovalStep,
                CurrentStatus = approval.ApprovalStatus,
                RequisitionDetails = $"Requisition #{approval.RequisitionId} - From {approval.Requisition?.IssueStation} to {approval.Requisition?.DeliveryStation}",
                IssueCategory = approval.Requisition.IssueStationCategory,
                IssueStation = approval.Requisition.IssueStation,
                DeliveryCategory = approval.Requisition.DeliveryStationCategory,
                DeliveryStation = approval.Requisition.DeliveryStation,
                ApprovalHistory = approvalHistory
            };

            // Get employee and department information
            var employee = await _employeeService.GetEmployeeByPayrollAsync(approval.PayrollNo);
            var department = await _departmentService.GetDepartmentByIdAsync(approval.DepartmentId);

            viewModel.EmployeeName = employee?.Fullname ?? "Unknown";
            viewModel.DepartmentName = department?.DepartmentName ?? "Unknown Department";

            return View(viewModel);
        }

        // POST: Approvals/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, ApprovalActionViewModel viewModel)
        {
            Console.WriteLine($"Approve action called for ID {id} with action {viewModel.Action}");

            if (id != viewModel.Id)
            {
                Console.WriteLine($"ID mismatch: URL ID {id} vs Model ID {viewModel.Id}");
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if comments are required for rejection
                    if ((viewModel.Action == "3" || viewModel.Action.ToLower() == "reject") && string.IsNullOrWhiteSpace(viewModel.Comments))
                    {
                        ModelState.AddModelError("Comments", "Comments are required when rejecting an approval.");
                        Console.WriteLine("Comments missing for rejection");
                    }
                    else
                    {
                        // Process the approval action using the new dynamic method
                        bool success = await _approvalService.ProcessApprovalActionAsync(id, viewModel.Action, viewModel.Comments);
                        
                        if (success)
                        {
                            // Determine the success message based on the action
                            string successMessage = "Action completed successfully.";
                            
                            if (int.TryParse(viewModel.Action, out int statusValue))
                            {
                                var status = (ApprovalStatus)statusValue;
                                successMessage = $"Step {GetActionVerb(status)} successfully.";
                            }
                            else
                            {
                                successMessage = $"Step {viewModel.Action}d successfully.";
                            }
                            
                            TempData["SuccessMessage"] = successMessage;
                            return RedirectToAction(nameof(Index));
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Failed to process the action. Please try again.";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception in Approve action: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    TempData["ErrorMessage"] = "An error occurred while processing your request.";
                }
            }
            else
            {
                Console.WriteLine("ModelState is invalid");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Model error: {error.ErrorMessage}");
                }
            }

            // If we get here, reload the view with error messages
            Console.WriteLine("Reloading the view due to errors or failed operation");
            var approval = await _context.Approvals
                .Include(a => a.Requisition)
                .Include(a => a.StepConfig)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (approval == null)
            {
                Console.WriteLine($"Approval not found for ID {id}");
                return NotFound();
            }

            // Get approval history and other data needed for the view
            var approvalHistory = await _approvalService.GetApprovalHistoryAsync(approval.RequisitionId);
            var statusOptions = await _approvalService.GetAvailableStatusOptionsAsync(approval.Id);
            ViewBag.StatusOptions = statusOptions;

            // Update the view model
            viewModel.ApprovalHistory = approvalHistory;
            viewModel.RequisitionDetails = $"Requisition #{approval.RequisitionId} - From {approval.Requisition?.IssueStation} to {approval.Requisition?.DeliveryStation}";
            viewModel.IssueCategory = approval.Requisition.IssueStationCategory;
            viewModel.IssueStation = approval.Requisition.IssueStation;
            viewModel.DeliveryCategory = approval.Requisition.DeliveryStationCategory;
            viewModel.DeliveryStation = approval.Requisition.DeliveryStation;
            viewModel.StepNumber = approval.StepNumber;

            // Get employee and department information
            var employee = await _employeeService.GetEmployeeByPayrollAsync(approval.PayrollNo);
            var department = await _departmentService.GetDepartmentByIdAsync(approval.DepartmentId);

            viewModel.EmployeeName = employee?.Fullname ?? "Unknown";
            viewModel.DepartmentName = department?.DepartmentName ?? "Unknown Department";

            return View(viewModel);
        }
        
        // Helper method to get the verb form of an action based on the status
        private string GetActionVerb(ApprovalStatus status)
        {
            switch (status)
            {
                case ApprovalStatus.Approved:
                    return "approved";
                case ApprovalStatus.Rejected:
                    return "rejected";
                case ApprovalStatus.Dispatched:
                    return "dispatched";
                case ApprovalStatus.Received:
                    return "received";
                case ApprovalStatus.OnHold:
                    return "put on hold";
                default:
                    return "processed";
            }
        }
    }
}
