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
using MRIV.Attributes;
using MRIV.ViewModels;
namespace MRIV.Controllers
{
    [CustomAuthorize]
    public class ApprovalsController : Controller
    {
        private readonly RequisitionContext _context;
        private readonly IEmployeeService _employeeService;
        private readonly VendorService _vendorService;
        private readonly IDepartmentService _departmentService;
        private readonly IApprovalService _approvalService;
        private readonly ILocationService _locationService;
        private readonly IVisibilityAuthorizeService _visibilityService;
        private readonly ILogger<ApprovalService> _logger;

        public ApprovalsController(
            RequisitionContext context, 
            IEmployeeService employeeService, 
            VendorService vendorService,
            IApprovalService approvalService, 
            ILocationService locationService,
            IConfiguration configuration, 
            IDepartmentService departmentService,
            IVisibilityAuthorizeService visibilityService,
            ILogger<ApprovalService> logger)
        {
            _context = context;
            _employeeService = employeeService;
            _vendorService = vendorService;
            _approvalService = approvalService;
            _locationService = locationService;
            _departmentService = departmentService;
            _visibilityService = visibilityService;
            _logger = logger;
        }

        // GET: Approvals
        public async Task<IActionResult> Index(string tab = "new", int page = 1, int pageSize = 20)
        {
            var userPayrollNo = HttpContext.Session.GetString("EmployeePayrollNo");
            if (userPayrollNo == null)
                return RedirectToAction("Index", "Login");

            // Get all approvals visible to the user (using new robust visibility logic)
            var approvalsQuery = await _visibilityService.ApplyVisibilityScopeAsync(_context.Approvals.Include(a => a.Requisition), userPayrollNo);

            // Define status groups
            var newStatusInts = new[] {
                (int)ApprovalStatus.NotStarted,
                (int)ApprovalStatus.PendingApproval,
                (int)ApprovalStatus.PendingDispatch,
                (int)ApprovalStatus.PendingReceive,
                (int)ApprovalStatus.OnHold
            };

            IQueryable<Approval> filteredQuery = approvalsQuery;

            switch (tab.ToLower())
            {
                case "new":
                    filteredQuery = approvalsQuery.Where(a =>
                        a.ApprovalStatus == ApprovalStatus.NotStarted ||
                        a.ApprovalStatus == ApprovalStatus.PendingApproval ||
                        a.ApprovalStatus == ApprovalStatus.PendingDispatch ||
                        a.ApprovalStatus == ApprovalStatus.PendingReceive ||
                        a.ApprovalStatus == ApprovalStatus.OnHold
                    );
                    break;
                case "completed":
                    filteredQuery = approvalsQuery.Where(a =>
                        a.ApprovalStatus != ApprovalStatus.NotStarted &&
                        a.ApprovalStatus != ApprovalStatus.PendingApproval &&
                        a.ApprovalStatus != ApprovalStatus.PendingDispatch &&
                        a.ApprovalStatus != ApprovalStatus.PendingReceive &&
                        a.ApprovalStatus != ApprovalStatus.OnHold
                    );
                    break;
                case "all":
                default:
                    // No additional filter
                    break;
            }

            // Pagination
            var totalItems = await filteredQuery.CountAsync();
            var pagedApprovals = await filteredQuery
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Prepare view models as before
            var viewModels = new List<MRIV.ViewModels.ApprovalStepViewModel>();
            foreach (var approval in pagedApprovals)
            {
                var employee = await _employeeService.GetEmployeeByPayrollAsync(approval.PayrollNo);
                var department = await _departmentService.GetDepartmentByIdAsync(approval.DepartmentId);
                string displayName;
                if (employee == null && (approval.ApprovalStep?.ToLower().Contains("vendor") == true ||
                                       approval.ApprovalStep?.ToLower().Contains("dispatch") == true))
                {
                    // Get vendor by ID (using PayrollNo as the ID in this case)
                    var vendor = await _vendorService.GetVendorByIdAsync(approval.PayrollNo);
                    displayName = vendor?.Name ?? "Unknown Vendor"; // Assuming the Vendor class has a Name property
                }
                else
                {
                    displayName = employee?.Fullname ?? "Unknown";
                }
                viewModels.Add(new MRIV.ViewModels.ApprovalStepViewModel
                {
                    Id = approval.Id,
                    RequisitionId = approval.RequisitionId,
                    StepNumber = approval.StepNumber,
                    ApprovalStep = approval.ApprovalStep,
                    ApprovalAction = approval.ApprovalAction,
                    PayrollNo = approval.PayrollNo,
                    EmployeeName = displayName,
                    DepartmentId = approval.DepartmentId,
                    DepartmentName = department?.DepartmentName ?? "Unknown Department",
                    ApprovalStatus = approval.ApprovalStatus,
                    CreatedAt = approval.CreatedAt,
                    EmployeeDesignation = employee?.Designation ?? ""
                });
            }

            ViewBag.Tab = tab;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;
            ViewBag.CurrentPage = page;

            return View(viewModels);
        }

        // GET: Approvals/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userPayrollNo = HttpContext.Session.GetString("EmployeePayrollNo");
            if (userPayrollNo == null)
                return RedirectToAction("Index", "Login");

            var approval = await _context.Approvals
                .Include(a => a.Requisition)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (approval == null)
            {
                return NotFound();
            }

            // Get the logged in user's info
            var user = await _employeeService.GetEmployeeByPayrollAsync(userPayrollNo);
            var userRole = user?.Role ?? string.Empty;

            // Admin can view all
            if (userRole != "Admin")
            {
                // Department check
                var departmentId = int.Parse(user.Department);
                if (approval.DepartmentId != departmentId)
                    return Forbid();

                // Role check
                if (!await _visibilityService.UserHasRoleForStep(approval.StepConfigId, userRole) &&
                    approval.PayrollNo != userPayrollNo)
                    return Forbid();
            }

            // Get employee and department information
            var employee = await _employeeService.GetEmployeeByPayrollAsync(approval.PayrollNo);
            var department = await _departmentService.GetDepartmentByIdAsync(approval.DepartmentId);

            // Create the view model
            var viewModel = new ApprovalStepViewModel
            {
                Id = approval.Id,
                RequisitionId = approval.RequisitionId,
                StepNumber = approval.StepNumber,
                ApprovalStep = approval.ApprovalStep,
                PayrollNo = approval.PayrollNo,
                EmployeeName = employee?.Fullname ?? "Unknown",
                DepartmentId = approval.DepartmentId,
                DepartmentName = department?.DepartmentName ?? "Unknown Department",
                ApprovalStatus = approval.ApprovalStatus,
                CreatedAt = approval.CreatedAt,
                EmployeeDesignation = employee?.Designation ?? ""
            };

            return View(viewModel);
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
                (int)approval.ApprovalStatus != (int)ApprovalStatus.PendingReceive &&
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

      
  
        // GET: Approvals/ApprovalSummary/5
        public async Task<IActionResult> ApprovalSummary(int id)
        {
            _logger.LogInformation($"Loading approval summary for approval ID: {id}");
            
            var approval = await _context.Approvals
                .Include(a => a.Requisition)
                .ThenInclude(r => r.RequisitionItems)
                .ThenInclude(ri => ri.Material)
                .Include(a => a.StepConfig)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (approval == null)
            {
                _logger.LogWarning($"Approval with ID {id} not found");
                return NotFound();
            }

            var requisition = approval.Requisition;
            if (requisition == null)
            {
                _logger.LogWarning($"Requisition not found for approval ID {id}");
                return NotFound();
            }
            
            var department = await _departmentService.GetDepartmentByIdAsync(requisition.DepartmentId);
            var employee = await _employeeService.GetEmployeeByPayrollAsync(requisition.PayrollNo);
            var issueStation = await _locationService.GetStationByIdAsync(requisition.IssueStationId);
            var deliveryStation = await _locationService.GetStationByIdAsync(requisition.DeliveryStationId);
            var deliveryDepartment = await _locationService.GetDepartmentByIdAsync(requisition.DeliveryDepartmentId);
            var vendor = requisition.DispatchType?.ToLower() == "vendor" && !string.IsNullOrEmpty(requisition.DispatchVendor)
                ? await _vendorService.GetVendorByIdAsync(requisition.DispatchVendor)
                : null;

            // Approval steps and history
            _logger.LogInformation($"Fetching approval history for requisition ID: {requisition.Id}");
            var approvalSteps = await _approvalService.GetApprovalHistoryAsync(requisition.Id);
            
            if (approvalSteps == null || !approvalSteps.Any())
            {
                _logger.LogWarning($"No approval steps found for requisition ID {requisition.Id}");
            }
            else
            {
                _logger.LogInformation($"Found {approvalSteps.Count} approval steps");
            }
            
            var currentStep = approvalSteps.FirstOrDefault(s => s.Id == approval.Id);

            // Status options for the action dropdown
            _logger.LogInformation($"Getting available status options for approval ID: {approval.Id}");
            var statusOptions = await _approvalService.GetAvailableStatusOptionsAsync(approval.Id);
            
            if (statusOptions == null || !statusOptions.Any())
            {
                _logger.LogWarning("No status options available for the approval");
                statusOptions = new Dictionary<string, string>(); // Initialize to empty dictionary to prevent null reference
            }
            else
            {
                _logger.LogInformation($"Found {statusOptions.Count} status options: {string.Join(", ", statusOptions.Values)}");
            }

            // Requisition items
            var items = requisition.RequisitionItems.Select(item => {
                var latestCondition = _context.MaterialConditions
                    .Where(mc => mc.RequisitionItemId == item.Id)
                    .OrderByDescending(mc => mc.InspectionDate)
                    .FirstOrDefault();
                var currentUser = HttpContext.Session.GetString("EmployeePayrollNo");
                return new RequisitionItemViewModel
                {
                    RequisitionId = item.Id,
                    MaterialName = item.Material?.Name ?? "Unknown",
                    Description = item.Description,
                    Quantity = item.Quantity,
                    MaterialId = item.MaterialId,
                    MaterialCode = item.Material?.Code ?? "",
                    RequisitionItemCondition = item.Condition,
                    Vendor = vendor?.Name ?? "",
                    CurrentCondition = latestCondition != null ? new MRIV.ViewModels.MaterialConditionViewModel {
                        RequisitionId = latestCondition.RequisitionId,
                        MaterialId = latestCondition.MaterialId,
                        MaterialAssignmentId = latestCondition.MaterialAssignmentId,
                        RequisitionItemId = latestCondition.RequisitionItemId ?? item.Id,
                        ApprovalId = approval?.Id ?? latestCondition?.ApprovalId ?? 0,
                        Condition = latestCondition.Condition,
                        FunctionalStatus = latestCondition.FunctionalStatus,
                        CosmeticStatus = latestCondition.CosmeticStatus,
                        Notes = " Condition of material requisition item on receiveing",
                        ConditionCheckType = latestCondition.ConditionCheckType,
                        Stage = "At Receiving",
                        InspectionDate = DateTime.Now,
                        InspectedBy = currentUser
                    } : new MRIV.ViewModels.MaterialConditionViewModel {
                        MaterialId = item.MaterialId,
                        RequisitionItemId = item.Id,
                        ConditionCheckType = MRIV.Enums.ConditionCheckType.AtDispatch,
                        Stage = "At-Receive",
                        InspectionDate = DateTime.Now,
                        InspectedBy = currentUser
                    }
                };
            }).ToList();

            // Find all relevant current steps (all pending steps for this requisition)
            var relevantCurrentSteps = approvalSteps.Where(s => 
                s.ApprovalStatus == ApprovalStatus.PendingApproval || 
                s.ApprovalStatus == ApprovalStatus.PendingDispatch || 
                s.ApprovalStatus == ApprovalStatus.PendingReceive)
                .ToList();
                
            _logger.LogInformation($"Found {relevantCurrentSteps.Count} relevant current steps that need action");

            var viewModel = new ApprovalSummaryViewModel
            {
                ApprovalId = approval.Id,
                RequisitionId = requisition.Id,
                ApprovalStep = approval.ApprovalStep,
                StepNumber = approval.StepNumber,
                RequisitionNumber = requisition.TicketId,
                IssueStationCategory = requisition.IssueStationCategory,
                RequestingDepartment = requisition.DepartmentId,
                RequestingStation = requisition.IssueStationId,
                DeliveryStationCategory = requisition.DeliveryStationCategory,
                DeliveryStation = requisition.DeliveryStationId,
                RequestDate = requisition.CreatedAt ?? DateTime.Now,
                Status = requisition.Status.ToString(),
                DepartmentName = department?.DepartmentName,
                RequestingEmployeeName = employee?.Fullname,
                IssueStationName = issueStation?.StationName,
                DeliveryStationName = deliveryStation?.StationName,
                DeliveryDepartmentName = deliveryDepartment?.DepartmentName,
                Remarks = requisition.Remarks,
                DispatchType = requisition.DispatchType,
                DispatcherName = null, // To be filled if needed
                VendorName = vendor?.Name,
                CollectorName = requisition.CollectorName,
                CollectorId = requisition.CollectorId,
                StatusOptions = statusOptions,
                CurrentApprovalSteps = relevantCurrentSteps, // Use all pending steps instead of just the current one
                ApprovalHistory = approvalSteps,
                RequisitionItems = items
            };

            // Set the current step in ViewBag for the partial view
            ViewBag.CurrentStep = approval.ApprovalStep;

            return View("ApprovalSummary", viewModel);
        }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApprovalSummary(ApprovalSummaryViewModel model)
    {
        Console.WriteLine($"=== CONTROLLER DEBUG: POST ApprovalSummary called ===");
        Console.WriteLine($"ApprovalId: {model.ApprovalId}");
        Console.WriteLine($"Action: '{model.Action}'");
        Console.WriteLine($"Comments: '{model.Comments}'");
        Console.WriteLine($"Action is null: {model.Action == null}");
        Console.WriteLine($"Action is empty: {string.IsNullOrEmpty(model.Action)}");
        Console.WriteLine($"Action is whitespace: {string.IsNullOrWhiteSpace(model.Action)}");
    
        if (model.Action != null)
        {
            Console.WriteLine($"Action length: {model.Action.Length}");
            Console.WriteLine($"Action trimmed: '{model.Action.Trim()}'");
        }

        if (string.IsNullOrEmpty(model.Action))
        {
            Console.WriteLine("Adding ModelState error: Action is required");
            ModelState.AddModelError("Action", "Please select an action");
        }
        
        if ((model.Action == ((int)ApprovalStatus.Rejected).ToString() || 
            model.Action == ((int)ApprovalStatus.OnHold).ToString()) && 
            string.IsNullOrWhiteSpace(model.Comments))
        {
            Console.WriteLine("Adding ModelState error: Comments required for Reject/OnHold");
            ModelState.AddModelError("Comments", "Comments are required when rejecting an approval or putting it on hold.");
        }
        
        if (!ModelState.IsValid)
        {
            Console.WriteLine("ModelState is invalid, reloading approval summary");
            Console.WriteLine($"ModelState errors count: {ModelState.ErrorCount}");
            foreach (var error in ModelState)
            {
                Console.WriteLine($"Key: {error.Key}, Errors: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
            }
            return await ApprovalSummary(model.ApprovalId);
        }
        
        try
        {
            var approvedBy = HttpContext.Session.GetString("EmployeePayrollNo");
            Console.WriteLine($"Calling ProcessApprovalActionAsync with ApprovalId: {model.ApprovalId}, Action: '{model.Action}'");
            
            bool success = await _approvalService.ProcessApprovalActionAsync(
                model.ApprovalId, 
                model.Action, 
                model.Comments ?? "",
                approvedBy);

            Console.WriteLine($"ProcessApprovalActionAsync returned: {success}");
            
            if (success)
            {
                Console.WriteLine("Success! Redirecting to Index");
                TempData["SuccessMessage"] = "Approval action processed successfully.";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                Console.WriteLine("Failed to process approval action");
                TempData["ErrorMessage"] = "Failed to process approval action.";
                return await ApprovalSummary(model.ApprovalId);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"EXCEPTION in controller: {ex.GetType().Name}");
            Console.WriteLine($"Exception message: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            TempData["ErrorMessage"] = $"Exception: {ex.Message}";
            return await ApprovalSummary(model.ApprovalId);
        }
    }

        // POST: Approvals/SaveItemCondition
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveItemCondition(MRIV.ViewModels.MaterialConditionViewModel model)
        {
            Console.WriteLine($"Saving material condition for item ID: {model.RequisitionItemId}");
            try
            {
                var approval = await _context.Approvals.FirstOrDefaultAsync(a => a.Id == model.ApprovalId);
                if (approval == null)
                {
                    Console.WriteLine($"Approval with ID {model.ApprovalId} not found when saving material condition");
                    return Json(new { success = false, message = "Approval not found" });
                }

                // Always create a new MaterialCondition
                var materialCondition = new MaterialCondition
                {
                    MaterialId = model.MaterialId,
                    MaterialAssignmentId = model.MaterialAssignmentId,
                    RequisitionId = model.RequisitionId,
                    RequisitionItemId = model.RequisitionItemId,
                    ApprovalId = model.ApprovalId,
                    Condition = model.Condition,
                    FunctionalStatus = model.FunctionalStatus,
                    CosmeticStatus = model.CosmeticStatus,
                    ConditionCheckType = model.ConditionCheckType,
                    Stage = model.Stage,
                    Notes = model.Notes,
                    ComponentStatuses = model.ComponentStatuses,
                    ActionRequired = model.ActionRequired,
                    ActionDueDate = model.ActionDueDate,
                    InspectionDate = DateTime.Now,
                    InspectedBy = approval.PayrollNo ?? User.Identity?.Name ?? "System"
                };
                _context.Add(materialCondition);
                await _context.SaveChangesAsync();
                Console.WriteLine("Material condition saved successfully");

                TempData["SuccessMessage"] = "Material condition saved successfully.";
                 return RedirectToAction("ApprovalSummary", new { id = model.ApprovalId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving material condition: {ex.Message}");
               TempData["ErrorMessage"] = "Error saving material condition: " + ex.Message;
                return RedirectToAction("ApprovalSummary", new { id = model.ApprovalId });
            }
        }

        #region Helper Methods

        /// <summary>
        /// Gets an approval with basic includes (Requisition and StepConfig)
        /// </summary>
        private async Task<Approval> GetApprovalWithBasicIncludes(int approvalId)
        {
            return await _context.Approvals
                .Include(a => a.Requisition)
                .Include(a => a.StepConfig)
                .FirstOrDefaultAsync(a => a.Id == approvalId);
        }

        /// <summary>
        /// Gets an approval with requisition items includes
        /// </summary>
        private async Task<Approval> GetApprovalWithRequisitionItems(int approvalId)
        {
            return await _context.Approvals
                .Include(a => a.Requisition)
                .ThenInclude(r => r.RequisitionItems)
                .ThenInclude(ri => ri.Material)
                .FirstOrDefaultAsync(a => a.Id == approvalId);
        }

        /// <summary>
        /// Finds a material condition for a requisition item, first checking the current approval step,
        /// then looking for any condition from the same requisition
        /// </summary>
        private async Task<MaterialCondition> FindMaterialCondition(int requisitionItemId, int approvalId, int requisitionId)
        {
            // First check if there's a condition for this approval step
            var condition = await _context.MaterialConditions
                .Where(mc => mc.RequisitionItemId == requisitionItemId && mc.ApprovalId == approvalId)
                .FirstOrDefaultAsync();

            // If no condition found for this approval, check if there's any condition for this item from any approval
            if (condition == null)
            {
                condition = await _context.MaterialConditions
                    .Where(mc => mc.RequisitionItemId == requisitionItemId && mc.RequisitionId == requisitionId)
                    .OrderByDescending(mc => mc.InspectionDate)
                    .FirstOrDefaultAsync();
                
                if (condition != null)
                {
                    _logger.LogInformation($"Found previous condition for item {requisitionItemId} from another approval step");
                }
            }

            return condition;
        }

        /// <summary>
        /// Determines the appropriate stage based on the approval step
        /// </summary>
        private string GetStageFromApprovalStep(string approvalStep)
        {
            if (string.IsNullOrEmpty(approvalStep))
                return "Inspection";

            if (approvalStep.ToLower().Contains("dispatch"))
                return "Pre-Dispatch";
            else if (approvalStep.ToLower().Contains("receiv"))
                return "Post-Receive";
            else
                return "Inspection";
        }

        /// <summary>
        /// Validates if a material ID exists in the database
        /// </summary>
        private async Task<int?> ValidateMaterialId(int? materialId)
        {
            if (!materialId.HasValue)
                return null;
                
            var materialExists = await _context.Materials.AnyAsync(m => m.Id == materialId.Value);
            if (materialExists)
            {
                return materialId.Value;
            }
            else
            {
                _logger.LogWarning($"Material with ID {materialId.Value} not found. Setting MaterialId to null.");
                return null;
            }
        }

        #endregion
    }
}
