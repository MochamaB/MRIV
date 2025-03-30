using MRIV.Models;
using Microsoft.EntityFrameworkCore;
using MRIV.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using MRIV.Enums;

namespace MRIV.Services
{
    public interface IApprovalService
    {
        Task<List<Approval>> CreateApprovalStepsAsync(Requisition requisition, EmployeeBkp loggedInUserEmployee);
        Task<List<ApprovalStepViewModel>> ConvertToViewModelsAsync(List<Approval> approvalSteps,Requisition requisition,List<Vendor> vendors);
        Task<Dictionary<string, SelectList>> PopulateDepartmentEmployeesAsync(Requisition requisition, List<Approval> approvalSteps);
        Task<Approval> GetMostSignificantApprovalAsync(int requisitionId);
        
        // Phase 2: Core Approval Functions
        Task<bool> ApproveStepAsync(int approvalId, string comments);
        Task<bool> RejectStepAsync(int approvalId, string comments);
        
        // New Material Status Functions
        Task<bool> DispatchStepAsync(int approvalId, string comments);
        Task<bool> ReceiveStepAsync(int approvalId, string comments);
        Task<bool> PutOnHoldStepAsync(int approvalId, string comments);
        
        // New dynamic action processor
        Task<bool> ProcessApprovalActionAsync(int approvalId, string action, string comments);
        
        Task<Approval> GetNextStepAsync(int requisitionId, int currentStepNumber);
        Task<List<ApprovalStepViewModel>> GetApprovalHistoryAsync(int requisitionId);
        
        // Get available status options based on approval step
        Task<Dictionary<string, string>> GetAvailableStatusOptionsAsync(int approvalId);
        
        // Get available status options for an approval
        List<ApprovalStatus> GetAvailableStatusOptions(Approval approval);
        
        // Convert list of ApprovalStatus to dictionary for dropdown
        Dictionary<string, string> GetStatusOptionsForDropdown(List<ApprovalStatus> statuses);
    }
    public class ApprovalService : IApprovalService
    {
        private readonly RequisitionContext _context;
        private readonly IEmployeeService _employeeService;
        private readonly IDepartmentService _departmentService;
        private readonly VendorService _vendorService;
        private readonly ILogger<ApprovalService> _logger;

        public ApprovalService(RequisitionContext context,
                              IEmployeeService employeeService,IDepartmentService departmentService,VendorService vendorService
            , ILogger<ApprovalService> logger)
        {
            _context = context;
            _employeeService = employeeService;
            _departmentService = departmentService;
            _vendorService = vendorService;
            _logger = logger;
        }
        public async Task<List<Approval>> CreateApprovalStepsAsync(Requisition requisition, EmployeeBkp loggedInUserEmployee)
        {
            var approvalSteps = new List<Approval>();

            // Get the workflow configuration based on issue and delivery station categories
            var workflowConfig = await _context.WorkflowConfigs
                .Include(wc => wc.Steps.OrderBy(s => s.StepOrder))
                .FirstOrDefaultAsync(wc =>
                    wc.IssueStationCategory.ToLower() == requisition.IssueStationCategory.ToLower() &&
                    wc.DeliveryStationCategory.ToLower() == requisition.DeliveryStationCategory.ToLower());

            if (workflowConfig == null)
            {
                _logger.LogWarning($"No workflow configuration found for {requisition.IssueStationCategory} to {requisition.DeliveryStationCategory}");
                return approvalSteps;
            }

            // Process each step in the workflow configuration
            foreach (var stepConfig in workflowConfig.Steps)
            {
                // Check if the step should be included based on conditions
                if (ShouldIncludeStep(stepConfig, requisition))
                {
                    // For the first step (usually supervisor), use the issue context
                    bool isFirstStep = stepConfig.StepOrder == 1;
                    string locationContext = isFirstStep ? requisition.IssueStation : requisition.DeliveryStation;

                    // Get the appropriate approver
                    object approver;

                    if (stepConfig.ApproverRole.ToLower() == "vendor" && requisition.DispatchType == "vendor")
                    {
                        // Handle vendor case specially
                        approver = requisition.DispatchVendor;
                    }
                    else if (stepConfig.ApproverRole.ToLower() == "dispatchadmin" && requisition.DispatchType == "admin")
                    {
                        // Handle admin dispatch case specially
                        approver = await _employeeService.GetEmployeeByPayrollAsync(requisition.DispatchPayrollNo);
                        _logger.LogInformation($"Using admin dispatch approver: {requisition.DispatchPayrollNo}");
                    }
                    else
                    {
                        // Use new employee service method for dynamic role-based lookup
                        approver = await _employeeService.GetEmployeeByRoleAndLocationAsync(
                            stepConfig.ApproverRole,
                            locationContext,
                            requisition.DepartmentId,
                            isFirstStep,
                            loggedInUserEmployee,
                            stepConfig.RoleParameters
                        );
                    }

                    if (approver != null)
                    {
                        var approval = new Approval
                        {
                            ApprovalStep = stepConfig.StepName,
                            PayrollNo = approver is EmployeeBkp employee ? employee.PayrollNo : approver.ToString(),
                            DepartmentId = approver is EmployeeBkp employee2 ?
                                (string.IsNullOrEmpty(employee2.Department) ? 0 : Convert.ToInt32(employee2.Department)) : 0,
                            WorkflowConfigId = workflowConfig.Id,
                            StepConfigId = stepConfig.Id,
                            IsAutoGenerated = true
                        };

                        approvalSteps.Add(approval);
                    }
                }
            }

            // Assign sequential step numbers and statuses
            for (int i = 0; i < approvalSteps.Count; i++)
            {
                approvalSteps[i].StepNumber = i + 1;
                approvalSteps[i].ApprovalStatus = i == 0? ApprovalStatus.PendingApproval: ApprovalStatus.NotStarted;
                approvalSteps[i].RequisitionId = requisition.Id;
                approvalSteps[i].CreatedAt = DateTime.Now;
                approvalSteps[i].UpdatedAt = i == 0 ? DateTime.Now : null;
            }

            return approvalSteps;
        }

       private bool ShouldIncludeStep(WorkflowStepConfig stepConfig, Requisition requisition)
{
    _logger.LogInformation($"Checking conditions for step: {stepConfig.StepName}");
    
    // If no conditions are present, always include the step
    if (stepConfig.Conditions == null || !stepConfig.Conditions.Any())
    {
        _logger.LogInformation($"Step {stepConfig.StepName} has no conditions, including it");
        return true;
    }

    // Check each condition
    foreach (var condition in stepConfig.Conditions)
    {
        var propertyName = condition.Key;
        var expectedValue = condition.Value;

        _logger.LogInformation($"Checking condition: {propertyName} = {expectedValue}");
        
        // Use reflection to get the property value - CASE INSENSITIVE
        var propertyInfo = requisition.GetType().GetProperties()
            .FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase));
            
        if (propertyInfo == null)
        {
            _logger.LogWarning($"Property {propertyName} not found on requisition");
            continue;
        }

        var actualValue = propertyInfo.GetValue(requisition)?.ToString();
        _logger.LogInformation($"Actual value: {actualValue}");

        // If the condition doesn't match, skip this step
        if (actualValue != expectedValue)
        {
            _logger.LogInformation($"Condition not matched, excluding step {stepConfig.StepName}");
            return false;
        }
    }

    // All conditions match
    _logger.LogInformation($"All conditions matched, including step {stepConfig.StepName}");
    return true;
}
        // GET APPROVAl STEPS IN VIEW
        public async Task<List<ApprovalStepViewModel>> ConvertToViewModelsAsync(List<Approval> approvalSteps,Requisition requisition,List<Vendor> vendors)
        {
            var viewModels = new List<ApprovalStepViewModel>();

            foreach (var step in approvalSteps)
            {
                var viewModel = await ProcessStep(step, requisition, vendors);
                viewModels.Add(viewModel);
            }

            return viewModels;
        }

        private async Task<ApprovalStepViewModel> ProcessStep(Approval step,Requisition requisition,List<Vendor> vendors)
        {
            // Get department and employee information
            var department = await _departmentService.GetDepartmentByIdAsync(step.DepartmentId);
            var employee = await _employeeService.GetEmployeeByPayrollAsync(step.PayrollNo);

            string departmentName = department?.DepartmentName ?? "Unknown";
            string employeeName = employee?.Fullname ?? "Unknown";

            // Vendor Dispatch handling
            if (step.ApprovalStep == "Vendor Dispatch")
            {
                employeeName = GetVendorName(step.PayrollNo, vendors);
                departmentName = "N/A";
            }

            // Factory Employee Receipt handling
            if (step.ApprovalStep == "Factory Employee Receipt" && !string.IsNullOrEmpty(requisition.DeliveryStation))
            {
                var station = await _departmentService.GetStationByStationNameAsync(requisition.DeliveryStation);
                departmentName = $"{departmentName} ({station?.StationName ?? "Unknown Station"})";
            }
            // Factory Employee Receipt handling
            if (step.ApprovalStep == "HO Employee Receipt" && !string.IsNullOrEmpty(requisition.DeliveryStation))
            {
                var station = await _departmentService.GetDepartmentByNameAsync(requisition.DeliveryStation);
                departmentName = $"{departmentName}";
            }


            return new ApprovalStepViewModel
            {
                StepNumber = step.StepNumber,
                ApprovalStep = step.ApprovalStep,
                PayrollNo = step.PayrollNo,
                EmployeeName = employeeName,
                DepartmentId = step.DepartmentId,
                DepartmentName = departmentName,
                ApprovalStatus = step.ApprovalStatus,
                CreatedAt = step.CreatedAt
            };
        }

      
        private string GetVendorName(string payrollNo, List<Vendor> vendors)
        {
            if (int.TryParse(payrollNo, out int vendorId))
            {
                return vendors.FirstOrDefault(v => v.VendorID == vendorId)?.Name
                       ?? "Unknown Vendor";
            }
            return "Invalid Vendor ID";
        }

        public async Task<Dictionary<string, SelectList>> PopulateDepartmentEmployeesAsync(
     Requisition requisition,
     List<Approval> approvalSteps)
        {
            var departmentEmployees = new Dictionary<string, SelectList>();

            if (approvalSteps == null) return departmentEmployees;

            foreach (var step in approvalSteps)
            {
                // Get appropriate employees for this step
                var employees = await GetAppropriateEmployeesForStepAsync(step, requisition);

                // Filter by roles if the step has a StepConfigId
                if (step.StepConfigId.HasValue)
                {
                    // Fetch the step config directly
                    var stepConfig = await _context.WorkflowStepConfigs
                        .FirstOrDefaultAsync(s => s.Id == step.StepConfigId.Value);

                    if (stepConfig != null &&
                        stepConfig.RoleParameters != null &&
                        stepConfig.RoleParameters.TryGetValue("roles", out var rolesString))
                    {
                        var allowedRoles = rolesString.Split(',').Select(r => r.Trim()).ToList();
                        employees = employees.Where(e =>
                            e.Role != null &&
                            allowedRoles.Contains(e.Role, StringComparer.OrdinalIgnoreCase))
                            .ToList();
                    }
                }

                // Create SelectList if we have valid employees
                if (employees != null && employees.Any())
                {
                    departmentEmployees[step.ApprovalStep] = new SelectList(
                        employees.Select(e => new {
                            PayrollNo = e.PayrollNo,
                            DisplayName = $"{e.Fullname} - {e.Designation}"
                        }),
                        "PayrollNo",
                        "DisplayName"
                    );
                }
                else
                {
                    // Add an empty SelectList to prevent null reference
                    departmentEmployees[step.ApprovalStep] = new SelectList(new List<EmployeeBkp>());
                }
            }

            return departmentEmployees;
        }

        private async Task<IEnumerable<EmployeeBkp>> GetAppropriateEmployeesForStepAsync(Approval step, Requisition requisition)
        {
            if (string.IsNullOrEmpty(step.ApprovalStep))
                return new List<EmployeeBkp>();

            // Handle office employee scenarios
            if (step.ApprovalStep == "Supervisor Approval" ||
                step.ApprovalStep == "Admin Dispatch Approval" ||
                step.ApprovalStep == "HO Employee Receipt")
            {
                // Try to get from department ID first
                var departmentEmployees = await _employeeService.GetEmployeesByDepartmentAsync(step.DepartmentId);

                // If we have a delivery station and no department employees, try by department name
                if ((departmentEmployees == null || !departmentEmployees.Any()) &&
                    !string.IsNullOrEmpty(requisition.DeliveryStation))
                {
                    return await _employeeService.GetEmployeesByDepartmentNameAsync(requisition.DeliveryStation);
                }

                return departmentEmployees ?? new List<EmployeeBkp>();
            }
            // Handle factory employee receipt
            else if (step.ApprovalStep == "Factory Employee Receipt" &&
                     !string.IsNullOrEmpty(requisition.DeliveryStation))
            {
                return await _employeeService.GetFactoryEmployeesByStationAsync(requisition.DeliveryStation);
            }

            // Return empty list for other cases
            return new List<EmployeeBkp>();
        }

        public async Task<Approval> GetMostSignificantApprovalAsync(int requisitionId)
        {
            var approvals = await _context.Approvals
                .Where(a => a.RequisitionId == requisitionId)
                .OrderBy(a => a.StepNumber)
                .ToListAsync();

            // First look for a pending approval
            var pendingApproval = approvals.FirstOrDefault(a => a.ApprovalStatus == ApprovalStatus.PendingApproval);
            if (pendingApproval != null)
            {
                return pendingApproval;
            }

            // If no pending approval, look for the most recent rejected approval
            var rejectedApproval = approvals
                .Where(a => a.ApprovalStatus == ApprovalStatus.Rejected)
                .OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt)
                .FirstOrDefault();
            if (rejectedApproval != null)
            {
                return rejectedApproval;
            }

            // If no pending or rejected approvals, return the last approval in the sequence
            return approvals.OrderByDescending(a => a.StepNumber).FirstOrDefault();
        }

        #region Phase 2: Core Approval Functions


        /// <summary>
        /// Approves an approval step and activates the next step if available
        /// </summary>
        /// <param name="approvalId">The ID of the approval to approve</param>
        /// <param name="comments">Optional comments for the approval</param>
        /// <returns>True if successful, false otherwise</returns>

        public async Task<bool> ProcessApprovalActionAsync(int approvalId, string action, string comments)
        {
            _logger.LogInformation($"Processing approval action '{action}' for ID {approvalId}");

            // Try to parse the action as an enum value
            if (int.TryParse(action, out int statusValue))
            {
                var approvalStatus = (ApprovalStatus)statusValue;
                _logger.LogInformation($"Parsed action value: {statusValue} as {approvalStatus}");

                // Call the appropriate method based on the status
                switch (approvalStatus)
                {
                    case ApprovalStatus.Approved:
                        return await ApproveStepAsync(approvalId, comments);

                    case ApprovalStatus.Rejected:
                        return await RejectStepAsync(approvalId, comments);

                    case ApprovalStatus.Dispatched:
                        return await DispatchStepAsync(approvalId, comments);

                    case ApprovalStatus.Received:
                        return await ReceiveStepAsync(approvalId, comments);

                    case ApprovalStatus.OnHold:
                        return await PutOnHoldStepAsync(approvalId, comments);

                    default:
                        _logger.LogWarning($"Unsupported approval status: {approvalStatus}");
                        return false;
                }
            }
            else
            {
                // Handle string-based actions
                string actionLower = action.ToLowerInvariant();
                _logger.LogInformation($"Processing string-based action: {actionLower}");

                if (actionLower == "approve")
                {
                    return await ApproveStepAsync(approvalId, comments);
                }
                else if (actionLower == "reject")
                {
                    return await RejectStepAsync(approvalId, comments);
                }
                else if (actionLower == "dispatch")
                {
                    return await DispatchStepAsync(approvalId, comments);
                }
                else if (actionLower == "receive")
                {
                    return await ReceiveStepAsync(approvalId, comments);
                }
                else if (actionLower == "hold" || actionLower == "onhold")
                {
                    return await PutOnHoldStepAsync(approvalId, comments);
                }
                else
                {
                    _logger.LogWarning($"Invalid action format: {action}");
                    return false;
                }
            }
        }

        public async Task<bool> ApproveStepAsync(int approvalId, string comments)
        {
            Console.WriteLine($"ApproveStepAsync called for ID {approvalId}");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Get the approval
                var approval = await _context.Approvals
                    .Include(a => a.Requisition)
                    .Include(a => a.StepConfig)
                    .FirstOrDefaultAsync(a => a.Id == approvalId);

                if (approval == null)
                {
                    Console.WriteLine($"Approval with ID {approvalId} not found");
                    return false;
                }

                // Check if the approval is in a state that can be approved
                if (approval.ApprovalStatus != ApprovalStatus.PendingApproval)
                {
                    Console.WriteLine($"Approval with ID {approvalId} is not pending approval. Current status: {approval.ApprovalStatus}");
                    return false;
                }

                // Update the approval
                approval.ApprovalStatus = ApprovalStatus.Approved;
                approval.Comments = comments;
                approval.UpdatedAt = DateTime.Now;

                Console.WriteLine($"Setting approval ID {approvalId} status to Approved");
                _context.Update(approval);
                await _context.SaveChangesAsync();
                Console.WriteLine("Approval updated in database");

                // Get the next step
                var nextStep = await GetNextStepAsync(approval.RequisitionId, approval.StepNumber);

                if (nextStep != null)
                {
                    // Determine the appropriate pending status based on the next step's role
                    ApprovalStatus nextStepStatus = ApprovalStatus.PendingApproval; // Default
                    
                    // Check the step name or role to determine the appropriate pending status
                    string stepNameLower = nextStep.ApprovalStep.ToLower();
                    string approverRoleLower = nextStep.StepConfig?.ApproverRole?.ToLower() ?? "";
                    
                    if (stepNameLower.Contains("dispatch") || approverRoleLower.Contains("dispatch"))
                    {
                        nextStepStatus = ApprovalStatus.PendingDispatch;
                        Console.WriteLine($"Next step is a dispatch step, setting status to PendingDispatch");
                    }
                    else if (stepNameLower.Contains("recei") || approverRoleLower.Contains("recei"))
                    {
                        nextStepStatus = ApprovalStatus.PendingReceive;
                        Console.WriteLine($"Next step is a receive step, setting status to PendingReceive");
                    }
                    else
                    {
                        Console.WriteLine($"Next step is a standard approval step, setting status to PendingApproval");
                    }

                    // Activate the next step
                    Console.WriteLine($"Activating next step ID {nextStep.Id} for requisition {approval.RequisitionId}");
                    nextStep.ApprovalStatus = nextStepStatus;
                    nextStep.UpdatedAt = DateTime.Now;

                    _context.Update(nextStep);
                    await _context.SaveChangesAsync();

                    Console.WriteLine($"Next step activated: ID: {nextStep.Id}, Step: {nextStep.ApprovalStep}, Status: {nextStepStatus}");
                }
                else
                {
                    // This was the last step, update requisition status to Completed
                    Console.WriteLine($"No next step found - this was the last step. Completing requisition {approval.RequisitionId}");
                    var requisition = approval.Requisition;
                    if (requisition != null)
                    {
                        requisition.Status = RequisitionStatus.Completed;
                        requisition.CompleteDate = DateTime.Now;
                        requisition.UpdatedAt = DateTime.Now;

                        _context.Update(requisition);
                        await _context.SaveChangesAsync();

                        Console.WriteLine($"Requisition {requisition.Id} marked as Completed");
                    }
                    else
                    {
                        Console.WriteLine($"Requisition is null for approval ID {approvalId}");
                    }
                }

                Console.WriteLine("Committing transaction");
                await transaction.CommitAsync();
                Console.WriteLine("Transaction committed successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in ApproveStepAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await transaction.RollbackAsync();
                Console.WriteLine("Transaction rolled back due to exception");
                return false;
            }
        }

        /// <summary>
        /// Rejects an approval step
        /// </summary>
        /// <param name="approvalId">The ID of the approval to reject</param>
        /// <param name="comments">Required comments explaining the rejection</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> RejectStepAsync(int approvalId, string comments)
        {
            _logger.LogInformation($"Rejecting step with ID {approvalId}");

            // Check if comments are provided when rejecting
            if (string.IsNullOrWhiteSpace(comments))
            {
                _logger.LogWarning("Comments are required when rejecting an approval");
                return false;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Get the approval
                var approval = await _context.Approvals
                    .Include(a => a.Requisition)
                    .FirstOrDefaultAsync(a => a.Id == approvalId);

                if (approval == null)
                {
                    _logger.LogWarning($"Approval with ID {approvalId} not found");
                    return false;
                }

                // Check if the approval is in a state that can be rejected
                if (approval.ApprovalStatus != ApprovalStatus.PendingApproval)
                {
                    _logger.LogWarning($"Approval with ID {approvalId} is not pending approval. Current status: {approval.ApprovalStatus}");
                    return false;
                }

                // Update the approval
                approval.ApprovalStatus = ApprovalStatus.Rejected;
                approval.Comments = comments;
                approval.UpdatedAt = DateTime.Now;

                _context.Update(approval);

                // Update the requisition status to Cancelled
                var requisition = approval.Requisition;
                if (requisition != null)
                {
                    requisition.Status = RequisitionStatus.Cancelled;
                    requisition.UpdatedAt = DateTime.Now;

                    _context.Update(requisition);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Successfully rejected approval step with ID {approvalId}");
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error rejecting step with ID {approvalId}");
                return false;
            }
        }

        /// <summary>
        /// Dispatches an approval step
        /// </summary>
        /// <param name="approvalId">The ID of the approval to dispatch</param>
        /// <param name="comments">Optional comments for the dispatch</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> DispatchStepAsync(int approvalId, string comments)
        {
            _logger.LogInformation($"Dispatching step with ID {approvalId}");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Get the approval
                var approval = await _context.Approvals
                    .Include(a => a.Requisition)
                    .Include(a => a.StepConfig)
                    .FirstOrDefaultAsync(a => a.Id == approvalId);

                if (approval == null)
                {
                    _logger.LogWarning($"Approval with ID {approvalId} not found");
                    return false;
                }

                // Check if the approval is in a state that can be dispatched
                if (approval.ApprovalStatus != ApprovalStatus.PendingDispatch && approval.ApprovalStatus != ApprovalStatus.PendingApproval)
                {
                    _logger.LogWarning($"Approval with ID {approvalId} is not pending dispatch or approval. Current status: {approval.ApprovalStatus}");
                    return false;
                }

                // Update the approval
                approval.ApprovalStatus = ApprovalStatus.Dispatched;
                approval.Comments = comments;
                approval.UpdatedAt = DateTime.Now;

                _context.Update(approval);
                await _context.SaveChangesAsync();

                // Get the next step
                var nextStep = await GetNextStepAsync(approval.RequisitionId, approval.StepNumber);

                if (nextStep != null)
                {
                    // Set the next step to PendingReceive since after dispatch comes receive
                    _logger.LogInformation($"Activating next step ID {nextStep.Id} for requisition {approval.RequisitionId}");
                    nextStep.ApprovalStatus = ApprovalStatus.PendingReceive;
                    nextStep.UpdatedAt = DateTime.Now;

                    _context.Update(nextStep);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Next step activated: ID: {nextStep.Id}, Step: {nextStep.ApprovalStep}, Status: {nextStep.ApprovalStatus}");
                }
                else
                {
                    // This was the last step, update requisition status to Completed
                    _logger.LogInformation($"No next step found - this was the last step. Completing requisition {approval.RequisitionId}");
                    var requisition = approval.Requisition;
                    if (requisition != null)
                    {
                        requisition.Status = RequisitionStatus.Completed;
                        requisition.CompleteDate = DateTime.Now;
                        requisition.UpdatedAt = DateTime.Now;

                        _context.Update(requisition);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation($"Requisition {requisition.Id} marked as Completed");
                    }
                    else
                    {
                        _logger.LogInformation($"Requisition is null for approval ID {approvalId}");
                    }
                }

                await transaction.CommitAsync();

                _logger.LogInformation($"Successfully dispatched approval step with ID {approvalId}");
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error dispatching step with ID {approvalId}");
                return false;
            }
        }

        /// <summary>
        /// Receives an approval step
        /// </summary>
        /// <param name="approvalId">The ID of the approval to receive</param>
        /// <param name="comments">Optional comments for the receive</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> ReceiveStepAsync(int approvalId, string comments)
        {
            _logger.LogInformation($"Receiving step with ID {approvalId}");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Get the approval
                var approval = await _context.Approvals
                    .Include(a => a.Requisition)
                    .Include(a => a.StepConfig)
                    .FirstOrDefaultAsync(a => a.Id == approvalId);

                if (approval == null)
                {
                    _logger.LogWarning($"Approval with ID {approvalId} not found");
                    return false;
                }

                // Check if the approval is in a state that can be received
                if (approval.ApprovalStatus != ApprovalStatus.PendingReceive && approval.ApprovalStatus != ApprovalStatus.PendingApproval)
                {
                    _logger.LogWarning($"Approval with ID {approvalId} is not pending receive or approval. Current status: {approval.ApprovalStatus}");
                    return false;
                }

                // Update the approval
                approval.ApprovalStatus = ApprovalStatus.Received;
                approval.Comments = comments;
                approval.UpdatedAt = DateTime.Now;

                _context.Update(approval);

                // Get the next step
                var nextStep = await GetNextStepAsync(approval.RequisitionId, approval.StepNumber);

                if (nextStep != null)
                {
                    // Set the next step to PendingApproval for final approval
                    _logger.LogInformation($"Activating next step ID {nextStep.Id} for requisition {approval.RequisitionId}");
                    nextStep.ApprovalStatus = ApprovalStatus.PendingApproval;
                    nextStep.UpdatedAt = DateTime.Now;

                    _context.Update(nextStep);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Next step activated: ID: {nextStep.Id}, Step: {nextStep.ApprovalStep}, Status: {nextStep.ApprovalStatus}");
                }
                else
                {
                    // This was the last step, update requisition status to Completed
                    _logger.LogInformation($"No next step found - this was the last step. Completing requisition {approval.RequisitionId}");
                    var requisition = approval.Requisition;
                    if (requisition != null)
                    {
                        requisition.Status = RequisitionStatus.Completed;
                        requisition.CompleteDate = DateTime.Now;
                        requisition.UpdatedAt = DateTime.Now;

                        _context.Update(requisition);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Successfully received approval step with ID {approvalId}");
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error receiving step with ID {approvalId}");
                return false;
            }
        }

        /// <summary>
        /// Puts an approval step on hold
        /// </summary>
        /// <param name="approvalId">The ID of the approval to put on hold</param>
        /// <param name="comments">Optional comments for the hold</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> PutOnHoldStepAsync(int approvalId, string comments)
        {
            _logger.LogInformation($"Putting step with ID {approvalId} on hold");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Get the approval
                var approval = await _context.Approvals
                    .Include(a => a.Requisition)
                    .FirstOrDefaultAsync(a => a.Id == approvalId);

                if (approval == null)
                {
                    _logger.LogWarning($"Approval with ID {approvalId} not found");
                    return false;
                }

                // Check if the approval is in a state that can be put on hold
                if (approval.ApprovalStatus != ApprovalStatus.PendingApproval)
                {
                    _logger.LogWarning($"Approval with ID {approvalId} is not pending approval. Current status: {approval.ApprovalStatus}");
                    return false;
                }

                // Update the approval
                approval.ApprovalStatus = ApprovalStatus.OnHold;
                approval.Comments = comments;
                approval.UpdatedAt = DateTime.Now;

                _context.Update(approval);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Successfully put approval step with ID {approvalId} on hold");
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error putting step with ID {approvalId} on hold");
                return false;
            }
        }

        /// <summary>
        /// Processes an approval action dynamically based on the action parameter
        /// </summary>
        /// <param name="approvalId">The ID of the approval to process</param>
        /// <param name="action">The action to perform (can be enum value or string)</param>
        /// <param name="comments">Comments for the action</param>
        /// <returns>True if successful, false otherwise</returns>
     

        /// <summary>
        /// Gets the next approval step for a requisition
        /// </summary>
        /// <param name="requisitionId">The requisition ID</param>
        /// <param name="currentStepNumber">The current step number</param>
        /// <returns>The next approval step or null if there is no next step</returns>
        public async Task<Approval> GetNextStepAsync(int requisitionId, int currentStepNumber)
        {
            _logger.LogInformation($"Getting next step after step {currentStepNumber} for requisition {requisitionId}");

            return await _context.Approvals
                .Where(a => a.RequisitionId == requisitionId && a.StepNumber > currentStepNumber)
                .OrderBy(a => a.StepNumber)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Gets the approval history for a requisition
        /// </summary>
        /// <param name="requisitionId">The requisition ID</param>
        /// <returns>List of approval steps with their details</returns>
        public async Task<List<ApprovalStepViewModel>> GetApprovalHistoryAsync(int requisitionId)
        {
            _logger.LogInformation($"Getting approval history for requisition {requisitionId}");

            try
            {
                // Get the approval steps
                var approvalSteps = await _context.Approvals
                    .Where(a => a.RequisitionId == requisitionId)
                    .OrderBy(a => a.StepNumber)
                    .ToListAsync();

                if (approvalSteps == null || !approvalSteps.Any())
                {
                    _logger.LogWarning($"No approval steps found for requisition {requisitionId}");
                    return new List<ApprovalStepViewModel>();
                }

                // Get the requisition
                var requisition = await _context.Requisitions
                    .FirstOrDefaultAsync(r => r.Id == requisitionId);

                if (requisition == null)
                {
                    _logger.LogWarning($"Requisition {requisitionId} not found");
                    return new List<ApprovalStepViewModel>();
                }

                // Get vendors for vendor dispatch steps
                var vendors = await _vendorService.GetVendorsAsync();

                // Convert to view models
                return await ConvertToViewModelsAsync(approvalSteps, requisition, vendors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting approval history for requisition {requisitionId}");
                return new List<ApprovalStepViewModel>();
            }
        }

        /// <summary>
        /// Gets available status options based on the approval step type
        /// </summary>
        /// <param name="approvalId">The ID of the approval</param>
        /// <returns>A dictionary of available status options</returns>
        public async Task<Dictionary<string, string>> GetAvailableStatusOptionsAsync(int approvalId)
        {
            // Get the approval
            var approval = await _context.Approvals
                .Include(a => a.StepConfig)
                .FirstOrDefaultAsync(a => a.Id == approvalId);

            if (approval == null)
            {
                // Return default options if approval not found
                return GetStatusOptionsForDropdown(new List<ApprovalStatus>
                {
                    ApprovalStatus.Approved,
                    ApprovalStatus.Rejected
                });
            }

            // Get available status options for this approval
            var statusOptions = GetAvailableStatusOptions(approval);

            // Convert to dictionary for dropdown
            return GetStatusOptionsForDropdown(statusOptions);
        }

        public List<ApprovalStatus> GetAvailableStatusOptions(Approval approval)
        {
            // Default fallback statuses if nothing is configured
            var defaultStatuses = new List<ApprovalStatus>
            {
                ApprovalStatus.Approved,
                ApprovalStatus.Rejected,
                ApprovalStatus.Dispatched,
                ApprovalStatus.Received,
                ApprovalStatus.OnHold
            };

            if (approval?.StepConfig == null || approval.StepConfig.Conditions == null)
            {
                return defaultStatuses;
            }

            // Try to get custom status options from the step configuration
            if (approval.StepConfig.Conditions.TryGetValue("ValidStatusTransitions", out var statusOptionsString))
            {
                try
                {
                    var customStatuses = statusOptionsString.Split(',')
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .Select(s => (ApprovalStatus)int.Parse(s))
                        .ToList();

                    return customStatuses.Any() ? customStatuses : defaultStatuses;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parsing approval status options");
                    return defaultStatuses;
                }
            }

            return defaultStatuses;
        }

        public Dictionary<string, string> GetStatusOptionsForDropdown(List<ApprovalStatus> statuses)
        {
            var statusOptions = new Dictionary<string, string>();

            foreach (var status in statuses)
            {
                // Use the integer value of the enum as the key
                var enumValue = ((int)status).ToString();

                // Get the description from the enum using reflection
                var description = GetEnumDescription(status);

                statusOptions.Add(enumValue, description);
            }

            return statusOptions;
        }

        private string GetEnumDescription(ApprovalStatus status)
        {
            // Get description from enum using reflection
            var fieldInfo = status.GetType().GetField(status.ToString());
            if (fieldInfo == null) return status.ToString();

            var attributes = fieldInfo.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false);

            if (attributes.Length > 0)
            {
                return ((System.ComponentModel.DescriptionAttribute)attributes[0]).Description;
            }

            return status.ToString();
        }

        #endregion
    }
}
