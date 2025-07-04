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
        private readonly IVisibilityAuthorizeService _visibilityService;
        private readonly ILogger<ApprovalService> _logger;

        public ApprovalsController(
            RequisitionContext context, 
            IEmployeeService employeeService, 
            VendorService vendorService,
            IApprovalService approvalService, 
            IConfiguration configuration, 
            IDepartmentService departmentService,
            IVisibilityAuthorizeService visibilityService,
            ILogger<ApprovalService> logger)
        {
            _context = context;
            _employeeService = employeeService;
            _vendorService = vendorService;
            _approvalService = approvalService;
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

            // Get all approvals visible to the user (use your visibility logic)
            var approvalsQuery = await _visibilityService.ApplyDepartmentScopeAsync(_context.Approvals.Include(a => a.Requisition), userPayrollNo);

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

      
  
        // GET: Approvals/ApprovalWizard/5
        public async Task<IActionResult> ApprovalWizard(int id)
        {
            _logger.LogInformation($"Loading Approval Wizard for approval ID: {id}");

            var approval = await _context.Approvals
                .Include(a => a.Requisition)
                .ThenInclude(r => r.RequisitionItems)
                .ThenInclude(ri => ri.Material)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (approval == null)
            {
                _logger.LogWarning($"Approval with ID {id} not found");
                return NotFound();
            }

            // Get approval history
            var approvalHistory = await _approvalService.GetApprovalHistoryAsync(approval.RequisitionId);

            // Create the wizard view model
            var viewModel = new ApprovalWizardViewModel
            {
                ApprovalId = approval.Id,
                RequisitionId = approval.RequisitionId,
                ApprovalStep = approval.ApprovalStep ?? "Unknown", // Default value to prevent null
                StepNumber = approval.StepNumber,

                // Requisition details
                RequisitionNumber = approval.Requisition.Id,
                IssueStationCategory = approval.Requisition.IssueStationCategory,
                RequestingDepartment = approval.Requisition.DepartmentId,
                RequestingStation = approval.Requisition.IssueStationId,
                DeliveryStationCategory = approval.Requisition.DeliveryStationCategory,
                DeliveryStation = approval.Requisition.DeliveryStationId,
                RequestDate = approval.Requisition.CreatedAt ?? DateTime.Now,
                Status = approval.Requisition.Status.ToString(),

                // Set initial step
                CurrentStep = "ItemConditions",
                IsLastStep = false
            };

            // Log the model to diagnose issues
            _logger.LogInformation($"Created ViewModel - ApprovalId: {viewModel.ApprovalId}, ApprovalStep: {viewModel.ApprovalStep}");

            // Populate requisition items with any existing conditions
            await PopulateRequisitionItemsWithConditions(viewModel, approval);

            // Populate approval history
            PopulateApprovalHistory(viewModel, approvalHistory);

            // Load status options for when we move to the approval action step
            viewModel.StatusOptions = await _approvalService.GetAvailableStatusOptionsAsync(approval.Id);

            return View(viewModel);
        }


        // POST: Approvals/ProcessApprovalWizard
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessApprovalWizard(ApprovalWizardViewModel model, string direction)
        {
            _logger.LogInformation($"ProcessApprovalWizard called with direction: {direction}");
            _logger.LogInformation($"Model state is valid: {ModelState.IsValid}");
            _logger.LogInformation($"Model properties: ApprovalId={model.ApprovalId}, ApprovalStep={model.ApprovalStep}, CurrentStep={model.CurrentStep}");

            // Handle navigation between wizard steps
            if (direction == "next")
            {
                try
                {
                    // No need to save material conditions here as they are now saved immediately via AJAX
                    
                    // Reload the approval to ensure we have fresh data for the next step
                    var approval = await GetApprovalWithBasicIncludes(model.ApprovalId);
                    if (approval == null)
                    {
                        _logger.LogWarning($"Approval with ID {model.ApprovalId} not found during next step transition");
                        return NotFound();
                    }

                    // Re-populate the necessary properties for the next step
                    model.ApprovalStep = approval.ApprovalStep ?? "Unknown"; // Ensure it's not null
                    model.CurrentStep = "ApprovalAction";
                    model.IsLastStep = true;

                    // Get available status options for this approval step
                    model.StatusOptions = await _approvalService.GetAvailableStatusOptionsAsync(model.ApprovalId);

                    // Re-populate the approval history
                    var approvalHistory = await _approvalService.GetApprovalHistoryAsync(approval.RequisitionId);
                    PopulateApprovalHistory(model, approvalHistory);

                    _logger.LogInformation($"Moving to ApprovalAction step with model: ApprovalId={model.ApprovalId}, ApprovalStep={model.ApprovalStep}");
                    return View("ApprovalWizard", model);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during next step transition");
                    ModelState.AddModelError("", "An error occurred while processing your request.");
                    return View("ApprovalWizard", model);
                }
            }
            else if (direction == "previous")
            {
                // Move back to the previous step
                model.CurrentStep = "ItemConditions";
                model.IsLastStep = false;
                _logger.LogInformation("Moving back to ItemConditions step");
                
                try
                {
                    // Reload the approval to ensure we have fresh data for the previous step
                    var approval = await GetApprovalWithRequisitionItems(model.ApprovalId);
                    if (approval == null)
                    {
                        _logger.LogWarning($"Approval with ID {model.ApprovalId} not found during previous step transition");
                        return NotFound();
                    }

                    // Clear and repopulate the items collection
                    model.Items.Clear();
                    
                    // Populate requisition items with any existing conditions
                    await PopulateRequisitionItemsWithConditions(model, approval);

                    _logger.LogInformation($"Reloaded {model.Items.Count} items for the previous step");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during previous step transition");
                    ModelState.AddModelError("", "An error occurred while processing your request.");
                }
                
                return View("ApprovalWizard", model);
            }
            else if (direction == "finish")
            {
                _logger.LogInformation("Processing finish action");

                // Process the approval action
                if (string.IsNullOrEmpty(model.Action))
                {
                    _logger.LogWarning("No action selected");
                    ModelState.AddModelError("Action", "Please select an action");
                    model.CurrentStep = "ApprovalAction";
                    model.IsLastStep = true;
                    model.StatusOptions = await _approvalService.GetAvailableStatusOptionsAsync(model.ApprovalId);
                    return View("ApprovalWizard", model);
                }

                // Check if the action is Rejected or OnHold and validate that comments are provided
                if ((model.Action == ((int)ApprovalStatus.Rejected).ToString() || 
                     model.Action == ((int)ApprovalStatus.OnHold).ToString()) && 
                    string.IsNullOrWhiteSpace(model.Comments))
                {
                    _logger.LogWarning("Comments required for Rejected or OnHold status");
                    ModelState.AddModelError("Comments", "Comments are required when rejecting an approval or putting it on hold.");
                    model.CurrentStep = "ApprovalAction";
                    model.IsLastStep = true;
                    model.StatusOptions = await _approvalService.GetAvailableStatusOptionsAsync(model.ApprovalId);
                    var approvalHistory = await _approvalService.GetApprovalHistoryAsync(model.RequisitionId);
                    PopulateApprovalHistory(model, approvalHistory);
                    return View("ApprovalWizard", model);
                }

                try
                {
                    // No need to save material conditions here as they are now saved immediately via AJAX
                    
                    // Process the approval action
                    bool success = await _approvalService.ProcessApprovalActionAsync(model.ApprovalId, model.Action, model.Comments ?? "");

                    if (success)
                    {
                        _logger.LogInformation($"Successfully processed approval action: {model.Action}");
                        TempData["SuccessMessage"] = "Approval action processed successfully.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        _logger.LogWarning("Failed to process approval action");
                        TempData["ErrorMessage"] = "Failed to process approval action.";
                        return RedirectToAction(nameof(ApprovalWizard), new { id = model.ApprovalId });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing approval action: {model.Action}");
                    ModelState.AddModelError("", "An error occurred while processing your approval action.");
                    model.CurrentStep = "ApprovalAction";
                    model.IsLastStep = true;
                    model.StatusOptions = await _approvalService.GetAvailableStatusOptionsAsync(model.ApprovalId);
                    var approvalHistory = await _approvalService.GetApprovalHistoryAsync(model.RequisitionId);
                    PopulateApprovalHistory(model, approvalHistory);
                    return View("ApprovalWizard", model);
                }
            }

            // Default fallback
            _logger.LogWarning($"Unhandled direction: {direction}");
            return RedirectToAction(nameof(Index));
        }

        // POST: Approvals/SaveItemCondition
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveItemCondition(int approvalId, int requisitionItemId, int? materialId, int? materialConditionId, Condition? condition, string notes, string stage)
        {
            _logger.LogInformation($"Saving material condition for item ID: {requisitionItemId}");
            
            try
            {
                // Get the current approval to access the ApprovalStep and approver's payroll
                var approval = await _context.Approvals
                    .FirstOrDefaultAsync(a => a.Id == approvalId);

                if (approval == null)
                {
                    _logger.LogWarning($"Approval with ID {approvalId} not found when saving material condition");
                    return Json(new { success = false, message = "Approval not found" });
                }

                // Skip if no condition is provided
                if (condition == null)
                {
                    return Json(new { success = false, message = "No condition provided" });
                }

                if (materialConditionId.HasValue)
                {
                    // Update existing condition
                    var existingCondition = await _context.MaterialConditions.FindAsync(materialConditionId.Value);
                    if (existingCondition != null)
                    {
                        _logger.LogInformation($"Updating existing condition ID: {existingCondition.Id}");
                        existingCondition.Condition = condition;
                        existingCondition.Notes = notes;
                        existingCondition.Stage = approval.ApprovalStep ?? stage ?? existingCondition.Stage;
                        existingCondition.InspectionDate = DateTime.Now;
                        existingCondition.InspectedBy = approval.PayrollNo ?? User.Identity?.Name ?? "System";
                        _context.Update(existingCondition);
                    }
                }
                else
                {
                    // Check if the MaterialId is valid
                    int? validMaterialId = await ValidateMaterialId(materialId);
                    
                    // Create new condition record
                    var materialCondition = new MaterialCondition
                    {
                        MaterialId = validMaterialId,
                        RequisitionId = approval.RequisitionId,
                        RequisitionItemId = requisitionItemId,
                        ApprovalId = approvalId,
                        Stage = approval.ApprovalStep ?? stage ?? "Unknown",
                        Condition = condition,
                        Notes = notes,
                        InspectedBy = approval.PayrollNo ?? User.Identity?.Name ?? "System",
                        InspectionDate = DateTime.Now
                    };

                    _context.Add(materialCondition);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Material condition saved successfully");
                
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving material condition");
                return Json(new { success = false, message = ex.Message });
            }
        }

        #region Helper Methods

        /// <summary>
        /// Populates the approval history in the view model
        /// </summary>
        private void PopulateApprovalHistory(ApprovalWizardViewModel model, List<ApprovalStepViewModel> approvalHistory)
        {
            model.ApprovalHistory = approvalHistory
                .Select(a => new ApprovalHistoryViewModel
                {
                    ApprovalId = a.Id,
                    ApprovalStep = a.ApprovalStep,
                    StepNumber = a.StepNumber,
                    Status = a.ApprovalStatus.ToString(),
                    ApproverName = a.EmployeeName,
                    Comments = a.Comments,
                    ApprovedDate = a.UpdatedAt
                })
                .ToList();
        }

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
        /// Populates the requisition items with their material conditions
        /// </summary>
        private async Task PopulateRequisitionItemsWithConditions(ApprovalWizardViewModel model, Approval approval)
        {
            if (approval.Requisition.RequisitionItems == null)
                return;

            foreach (var item in approval.Requisition.RequisitionItems)
            {
                // Check if there's an existing material condition record for this item in this approval step
                var existingCondition = await FindMaterialCondition(item.Id, approval.Id, approval.RequisitionId);

                // Get vendor information if available
                Vendor? vendor = null;
                if (item.Material?.VendorId != null)
                {
                    vendor = await _vendorService.GetVendorByIdAsync(item.Material.VendorId);
                }

                var itemViewModel = new RequisitionItemConditionViewModel
                {
                    RequisitionItemId = item.Id,
                    Name = item.Name ?? "N/A",
                    MaterialId = item.MaterialId,
                    MaterialCode = item.Material?.Code ?? "N/A",
                    Description = item.Description ?? "N/A",
                    Quantity = item.Quantity,
                    RequisitionItemCondition = item.Condition,
                    vendor = vendor
                };

                // If there's an existing condition, populate it
                if (existingCondition != null)
                {
                    itemViewModel.MaterialConditionId = existingCondition.Id;
                    itemViewModel.Condition = existingCondition.Condition;
                    itemViewModel.Notes = existingCondition.Notes;
                    itemViewModel.Stage = existingCondition.Stage;
                }
                else
                {
                    // Set default stage based on the approval step
                    itemViewModel.Stage = GetStageFromApprovalStep(approval.ApprovalStep);
                }

                model.Items.Add(itemViewModel);
            }
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
