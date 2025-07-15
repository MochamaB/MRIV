using MRIV.Models;
using Microsoft.EntityFrameworkCore;
using MRIV.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using MRIV.Enums;
using static MRIV.Constants.SettingConstants;

namespace MRIV.Services
{
    public interface IApprovalService
    {
        Task<List<Approval>> CreateApprovalStepsAsync(Requisition requisition, EmployeeBkp loggedInUserEmployee);
        Task<List<ApprovalStepViewModel>> ConvertToViewModelsAsync(List<Approval> approvalSteps,Requisition requisition,List<Vendor> vendors);
        Task<Dictionary<string, SelectList>> PopulateDepartmentEmployeesAsync(Requisition requisition, List<Approval> approvalSteps);
        Task<Approval> GetMostSignificantApprovalAsync(int requisitionId);
        
        // Phase 2: Core Approval Functions
        Task<bool> ApproveStepAsync(int approvalId, string comments, string approvedBy);
        Task<bool> RejectStepAsync(int approvalId, string comments, string approvedBy);
        
        // New Material Status Functions
        Task<bool> DispatchStepAsync(int approvalId, string comments, string approvedBy);
        Task<bool> ReceiveStepAsync(int approvalId, string comments, string approvedBy);
        Task<bool> PutOnHoldStepAsync(int approvalId, string comments, string approvedBy);
        
        // New dynamic action processor
        Task<bool> ProcessApprovalActionAsync(int approvalId, string action, string comments, string? approvedBy);
        
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
        private readonly INotificationService _notificationService;
        private readonly INotificationManager _notificationManager;

        public ApprovalService(RequisitionContext context,
                              IEmployeeService employeeService, IDepartmentService departmentService, VendorService vendorService,
                              ILogger<ApprovalService> logger, INotificationService notificationService, INotificationManager notificationManager)
        {
            _context = context;
            _employeeService = employeeService;
            _departmentService = departmentService;
            _vendorService = vendorService;
            _logger = logger;
            _notificationService = notificationService;
            _notificationManager = notificationManager;
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
                    // Get location context with all necessary information
                    var (locationName, contextStationId, contextDepartmentId, contextType) =
                            await GetLocationContextAsync(requisition, stepConfig);

                    // Get the appropriate approver using the enhanced method
                    EmployeeBkp employeeApprover = null;
                    string approverPayrollNo = null;

                    // Special handling for vendor dispatch
                    if (stepConfig.ApproverRole.ToLower() == "vendor" && requisition.DispatchType == "vendor")
                    {
                        // For vendor dispatch, use the vendor ID
                        approverPayrollNo = requisition.DispatchVendor;
                    }
                    else
                    {
                        // For all other cases, use the new context-based approver selection
                        employeeApprover = await _employeeService.GetApproverByContextAsync(
                            contextType,
                            contextStationId,
                            contextDepartmentId,
                            loggedInUserEmployee,
                            requisition.DispatchType,
                            requisition.DispatchPayrollNo
                        );

                        // Handle special case for vendor dispatch context type
                        if (employeeApprover == null && contextType == "Dispatch" && requisition.DispatchType == "vendor")
                        {
                            approverPayrollNo = "ICTdispatch";
                        }
                        else if (employeeApprover != null)
                        {
                            approverPayrollNo = employeeApprover.PayrollNo;
                        }
                    }


                  
                        var approval = new Approval
                        {
                            ApprovalStep = stepConfig.StepName,
                            ApprovalAction = stepConfig.StepAction,
                            PayrollNo = approverPayrollNo,
                            // Use context-based location information
                            StationId = contextStationId,
                            DepartmentId = contextDepartmentId,
                            WorkflowConfigId = workflowConfig.Id,
                            StepConfigId = stepConfig.Id,
                            IsAutoGenerated = true
                        };

                        approvalSteps.Add(approval);
                    
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

        // Helper Method to get the station and department
        private async Task<(string locationName, int stationId, int departmentId, string contextType)>
     GetLocationContextAsync(Requisition requisition, WorkflowStepConfig stepConfig)
        {
            // Determine context based on step properties
            bool isIssueContext = false;
            bool isDispatchContext = false;

            // Check if it's an issue context
            if (stepConfig.StepOrder == 1 ||
                stepConfig.StepName.Contains("Supervisor", StringComparison.OrdinalIgnoreCase) ||
                stepConfig.StepAction.Equals("Approve", StringComparison.OrdinalIgnoreCase))
            {
                isIssueContext = true;
            }

            // Check if it's a dispatch context
            if (stepConfig.StepName.Contains("Dispatch", StringComparison.OrdinalIgnoreCase) ||
                stepConfig.StepAction.Contains("Dispatch", StringComparison.OrdinalIgnoreCase) ||
                stepConfig.ApproverRole.Contains("Dispatch", StringComparison.OrdinalIgnoreCase))
            {
                isDispatchContext = true;
                isIssueContext = false; // Dispatch takes precedence over issue
            }

            // Set location information based on context
            if (isIssueContext)
            {
                // Issue context (typically first approval steps)
                int stationId = requisition.IssueStationId;
                int departmentId = int.TryParse(requisition.IssueDepartmentId, out var deptId) ? deptId : 114;
                string locationName = await _departmentService.GetLocationNameFromIdsAsync(
                    requisition.IssueStationId, requisition.IssueDepartmentId);

                return (locationName, stationId, departmentId, "Issue");
            }
            else if (isDispatchContext)
            {
                // Dispatch context
                if (requisition.DispatchType == "vendor")
                {
                    // For vendor dispatch, use delivery location (destination)
                    int stationId = requisition.IssueStationId;
                    int departmentId = int.TryParse(requisition.DeliveryDepartmentId, out var deptId) ? deptId : 114;
                    string locationName = await _departmentService.GetLocationNameFromIdsAsync(
                        requisition.DeliveryStationId, requisition.DeliveryDepartmentId);

                    return (locationName, stationId, departmentId, "Dispatch");
                }
                else if (requisition.DispatchType == "admin")
                {
                    // For admin dispatch, use admin department (106) but delivery station
                    int stationId = requisition.IssueStationId;
                    int departmentId = 106; // Admin department
                    string locationName = "Admin Dispatch";

                    return (locationName, stationId, departmentId, "Dispatch");
                }
            }

            // Default to delivery context (typically receipt steps)
            int deliveryStationId = requisition.DeliveryStationId;
            int deliveryDepartmentId = int.TryParse(requisition.DeliveryDepartmentId, out var deliveryDeptId) ? deliveryDeptId : 114;
            string deliveryLocationName = await _departmentService.GetLocationNameFromIdsAsync(
                requisition.DeliveryStationId, requisition.DeliveryDepartmentId);

            return (deliveryLocationName, deliveryStationId, deliveryDepartmentId, "Delivery");
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

                // Handle special cases for the new properties
                if (propertyName.Equals("IssueStation", StringComparison.OrdinalIgnoreCase))
                {
                    // Use the station ID or department ID as appropriate
                    var stationName = _departmentService.GetLocationNameFromIdsAsync(
                        requisition.IssueStationId, requisition.IssueDepartmentId).Result;

                    if (stationName != expectedValue)
                    {
                        _logger.LogInformation($"Condition not matched, excluding step {stepConfig.StepName}");
                        return false;
                    }
                    continue;
                }
                else if (propertyName.Equals("DeliveryStation", StringComparison.OrdinalIgnoreCase))
                {
                    // Use the station ID or department ID as appropriate
                    var stationName = _departmentService.GetLocationNameFromIdsAsync(
                        requisition.DeliveryStationId, requisition.DeliveryDepartmentId).Result;

                    if (stationName != expectedValue)
                    {
                        _logger.LogInformation($"Condition not matched, excluding step {stepConfig.StepName}");
                        return false;
                    }
                    continue;
                }

                // Use reflection for other properties
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

        #region Phase 2: Display Approvals in the view


        /// <summary>
        /// Approves an approval step and activates the next step if available
        /// </summary>
        /// <param name="approvalId">The ID of the approval to approve</param>
        /// <param name="comments">Optional comments for the approval</param>
        /// <returns>True if successful, false otherwise</returns>


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

        private async Task<ApprovalStepViewModel> ProcessStep(Approval step, Requisition requisition, List<Vendor> vendors)
        {
            // Get department and employee information
            var department = await _departmentService.GetDepartmentByIdAsync(step.DepartmentId);
            var employee = await _employeeService.GetEmployeeByPayrollAsync(step.PayrollNo);

            // Get station information if available
            string stationName = "Unknown";
            if (step.StationId > 0)
            {
                var station = await _departmentService.GetStationByIdAsync(step.StationId);
                stationName = station?.StationName ?? "Unknown";
            }
            else if (step.StationId == 0)
            {
                stationName = "HQ - HEAD OFFICE";
            }

            string departmentName = department?.DepartmentName ?? "Unknown";
            string employeeName = employee?.Fullname ?? "Unknown";

            // Vendor Dispatch handling
            if (step.ApprovalStep == "Vendor Dispatch")
            {
                employeeName = GetVendorName(step.PayrollNo, vendors);
                departmentName = "N/A";
            }

            // Resolve delivery location name from IDs
            string deliveryLocationName = await _departmentService.GetLocationNameFromIdsAsync(
                requisition.DeliveryStationId, 
                requisition.DeliveryDepartmentId);

            // Factory Employee Receipt handling
            if (step.ApprovalStep == "Factory Employee Receipt" && !string.IsNullOrEmpty(deliveryLocationName))
            {
                departmentName = $"{departmentName} ({deliveryLocationName})";
            }
            // HO Employee Receipt handling
            if (step.ApprovalStep == "HO Employee Receipt" && !string.IsNullOrEmpty(deliveryLocationName))
            {
                departmentName = $"{departmentName}";
            }

            return new ApprovalStepViewModel
            {
                StepNumber = step.StepNumber,
                ApprovalStep = step.ApprovalStep,
                ApprovalAction = step.ApprovalAction,
                PayrollNo = step.PayrollNo,
                EmployeeName = employeeName,
                DepartmentId = step.DepartmentId,
                DepartmentName = departmentName,
                StationId = step.StationId,
                StationName = stationName,
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
            // Defensive: If for some reason the step info is missing, return empty list
            if (step == null)
                return Enumerable.Empty<EmployeeBkp>();

            // Use the new EmployeeService method to get employees filtered by location
            // Role filtering will be handled later in PopulateDepartmentEmployeesAsync
            return await _employeeService.GetEmployeesByLocationAsync(step.StationId, step.DepartmentId);
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

        #region Phase 3: Core Approval Functions


        /// <summary>
        /// Approves an approval step and activates the next step if available
        /// </summary>
        /// <param name="approvalId">The ID of the approval to approve</param>
        /// <param name="comments">Optional comments for the approval</param>
        /// <returns>True if successful, false otherwise</returns>

       public async Task<bool> ProcessApprovalActionAsync(int approvalId, string action, string comments, string? approvedBy)
        {
            Console.WriteLine($"=== DEBUG: Processing approval action '{action}' for ID {approvalId} ===");
    
            if (string.IsNullOrEmpty(action))
            {
                Console.WriteLine("ERROR: Action is null or empty");
                return false;
            }

            Console.WriteLine($"Action value: '{action}'");
            Console.WriteLine($"Action length: {action.Length}");
            Console.WriteLine($"Action type: {action.GetType().Name}");

            try
            {
                // Try to parse the action as an enum value
                if (int.TryParse(action, out int statusValue))
                {
                    Console.WriteLine($"Successfully parsed action to int: {statusValue}");
            
                    // Validate that the parsed value is within the valid range of ApprovalStatus enum
                    if (!Enum.IsDefined(typeof(ApprovalStatus), statusValue))
                    {
                        Console.WriteLine($"ERROR: Invalid approval status value: {statusValue}");
                        Console.WriteLine($"Valid enum values are: {string.Join(", ", Enum.GetValues(typeof(ApprovalStatus)).Cast<int>())}");
                        return false;
                    }

                    var approvalStatus = (ApprovalStatus)statusValue;
                    Console.WriteLine($"Parsed action value: {statusValue} as {approvalStatus}");

                    // Call the appropriate method based on the status
                    switch (approvalStatus)
                    {
                        case ApprovalStatus.Approved:
                            Console.WriteLine("Calling ApproveStepAsync");
                            return await ApproveStepAsync(approvalId, comments, approvedBy);

                        case ApprovalStatus.Rejected:
                            Console.WriteLine("Calling RejectStepAsync");
                            return await RejectStepAsync(approvalId, comments, approvedBy);

                        case ApprovalStatus.Dispatched:
                            Console.WriteLine("Calling DispatchStepAsync");
                            return await DispatchStepAsync(approvalId, comments, approvedBy);

                        case ApprovalStatus.Received:
                            Console.WriteLine("Calling ReceiveStepAsync");
                            return await ReceiveStepAsync(approvalId, comments, approvedBy);

                        case ApprovalStatus.OnHold:
                            Console.WriteLine("Calling PutOnHoldStepAsync");
                            return await PutOnHoldStepAsync(approvalId, comments, approvedBy);

                        case ApprovalStatus.PendingReceive:
                            Console.WriteLine("Handling PendingReceive - calling ReceiveStepAsync");
                            return await ReceiveStepAsync(approvalId, comments, approvedBy);

                        case ApprovalStatus.PendingDispatch:
                            Console.WriteLine("Handling PendingDispatch - calling DispatchStepAsync");
                            return await DispatchStepAsync(approvalId, comments, approvedBy);

                        case ApprovalStatus.PendingApproval:
                            Console.WriteLine("Handling PendingApproval - calling ApproveStepAsync");
                            return await ApproveStepAsync(approvalId, comments, approvedBy);

                        case ApprovalStatus.NotStarted:
                        case ApprovalStatus.Cancelled:
                            Console.WriteLine($"ERROR: Cannot process action for status: {approvalStatus}");
                            return false;

                        default:
                            Console.WriteLine($"ERROR: Unsupported approval status: {approvalStatus} (value: {statusValue})");
                            return false;
                    }
                }
                else
                {
                    Console.WriteLine($"Failed to parse '{action}' as integer, trying string-based actions");
            
                    // Handle string-based actions as fallback
                    string actionLower = action.ToLowerInvariant().Trim();
                    Console.WriteLine($"Processing string-based action: '{actionLower}'");

                    return actionLower switch
                    {
                        "approve" or "approved" => await ApproveStepAsync(approvalId, comments, approvedBy),
                        "reject" or "rejected" => await RejectStepAsync(approvalId, comments, approvedBy),
                        "dispatch" or "dispatched" => await DispatchStepAsync(approvalId, comments, approvedBy),
                        "receive" or "received" => await ReceiveStepAsync(approvalId, comments, approvedBy),
                        "hold" or "onhold" or "on hold" => await PutOnHoldStepAsync(approvalId, comments, approvedBy),
                        _ => throw new ArgumentException($"Invalid action format: {action}")
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION: Error processing approval action '{action}' for ID {approvalId}");
                Console.WriteLine($"Exception type: {ex.GetType().Name}");
                Console.WriteLine($"Exception message: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<bool> ApproveStepAsync(int approvalId, string comments, string approvedByPayrollNo)
        {
            Console.WriteLine($"ApproveStepAsync called for ID {approvalId}");

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var approval = await _context.Approvals
                        .Include(a => a.Requisition)
                        .Include(a => a.StepConfig)
                        .FirstOrDefaultAsync(a => a.Id == approvalId);

                    if (approval == null)
                        return false;

                    if (approval.ApprovalStatus != ApprovalStatus.PendingApproval)
                        return false;

                    approval.ApprovalStatus = ApprovalStatus.Approved;
                    approval.ApprovedBy = approvedByPayrollNo;
                    approval.Comments = comments;
                    approval.UpdatedAt = DateTime.Now;

                    _context.Update(approval);
                    await _context.SaveChangesAsync();

                    // Get next step name for notification
                    var nextStep = await GetNextStepAsync(approval.RequisitionId, approval.StepNumber);
                    string nextStepName = nextStep?.ApprovalStep ?? "Completed";
                    await _notificationManager.NotifyApprovalStepApproved(approval, approvedByPayrollNo, nextStepName);

                    await HandleNextStepOrCompleteRequisitionAsync(approval, DetermineNextStepStatus(nextStep));

                    // Update requisition status if this is the first approval step
                    var requisition = approval.Requisition;
                    if (requisition != null)
                    {
                        // If this is the first approval and there is a dispatch step, set to PendingDispatch
                        var hasDispatchStep = _context.Approvals.Any(a => a.RequisitionId == requisition.Id && a.ApprovalStep.ToLower().Contains("dispatch"));
                        if (approval.StepNumber == 1 && hasDispatchStep)
                        {
                            requisition.Status = RequisitionStatus.PendingDispatch;
                        }
                        // If this is the last step and no more steps remain, set to Completed
                        var nextStepAfterThis = await GetNextStepAsync(approval.RequisitionId, approval.StepNumber);
                        if (nextStepAfterThis == null)
                        {
                            requisition.Status = RequisitionStatus.Completed;
                        }
                        requisition.UpdatedAt = DateTime.Now;
                        _context.Update(requisition);
                    }

                    await transaction.CommitAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return false;
                }
            });
        }

        /// <summary>
        /// Determines the appropriate pending status for the next step based on the step name or role
        /// </summary>
        /// <param name="nextStep">The next step in the workflow</param>
        /// <returns>The appropriate ApprovalStatus for the next step</returns>
        private ApprovalStatus DetermineNextStepStatus(Approval nextStep)
        {
            // Default status
            ApprovalStatus nextStepStatus = ApprovalStatus.PendingApproval;
            
            if (nextStep != null)
            {
                // Check the step name or role to determine the appropriate pending status
                string stepNameLower = nextStep.ApprovalStep.ToLower();
                string approverRoleLower = nextStep.StepConfig?.ApproverRole?.ToLower() ?? "";
                
                if (stepNameLower.Contains("dispatch") || approverRoleLower.Contains("dispatch"))
                {
                    nextStepStatus = ApprovalStatus.PendingDispatch;
                    _logger.LogInformation($"Next step is a dispatch step, setting status to PendingDispatch");
                }
                else if (stepNameLower.Contains("recei") || approverRoleLower.Contains("recei"))
                {
                    nextStepStatus = ApprovalStatus.PendingReceive;
                    _logger.LogInformation($"Next step is a receive step, setting status to PendingReceive");
                }
                else
                {
                    _logger.LogInformation($"Next step is a standard approval step, setting status to PendingApproval");
                }
            }
            
            return nextStepStatus;
        }

        public async Task<bool> RejectStepAsync(int approvalId, string comments, string rejectedByPayrollNo)
        {
            _logger.LogInformation($"Rejecting step with ID {approvalId}");
            if (string.IsNullOrWhiteSpace(comments))
                return false;

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var approval = await _context.Approvals
                        .Include(a => a.Requisition)
                        .Include(a => a.StepConfig)
                        .FirstOrDefaultAsync(a => a.Id == approvalId);

                    if (approval == null)
                        return false;

                    approval.ApprovalStatus = ApprovalStatus.Rejected;
                    approval.Comments = comments;
                    approval.UpdatedAt = DateTime.Now;
                    _context.Update(approval);

                    var requisition = approval.Requisition;
                    if (requisition != null)
                    {
                        requisition.Status = RequisitionStatus.Cancelled;
                        requisition.UpdatedAt = DateTime.Now;
                        _context.Update(requisition);
                    }

                    await _context.SaveChangesAsync();

                    await _notificationManager.NotifyApprovalStepRejected(approval, rejectedByPayrollNo, comments);

                    await transaction.CommitAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return false;
                }
            });
        }

        public async Task<bool> DispatchStepAsync(int approvalId, string comments, string dispatcherPayrollNo)
        {
            _logger.LogInformation($"Dispatching step with ID {approvalId}");
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var approval = await _context.Approvals
                        .Include(a => a.Requisition)
                        .Include(a => a.StepConfig)
                        .FirstOrDefaultAsync(a => a.Id == approvalId);

                    if (approval == null)
                        return false;

                    if (approval.ApprovalStatus != ApprovalStatus.PendingDispatch && approval.ApprovalStatus != ApprovalStatus.PendingApproval)
                        return false;

                    approval.ApprovalStatus = ApprovalStatus.Dispatched;
                    approval.Comments = comments;
                    approval.UpdatedAt = DateTime.Now;
                    _context.Update(approval);

                    var deliveryStation = await _departmentService.GetStationByIdAsync(approval.Requisition.DeliveryStationId);
                    string deliveryStationName = deliveryStation?.StationName ?? "Unknown";

                    await _context.SaveChangesAsync();

                    await _notificationManager.NotifyApprovalStepDispatched(approval, dispatcherPayrollNo, deliveryStationName);

                    await HandleNextStepOrCompleteRequisitionAsync(approval, DetermineNextStepStatus(await GetNextStepAsync(approval.RequisitionId, approval.StepNumber)));

                    var requisition = approval.Requisition;
                    if (requisition != null)
                    {
                        // Set to PendingReceipt after dispatch
                        requisition.Status = RequisitionStatus.PendingReceipt;
                        requisition.UpdatedAt = DateTime.Now;
                        _context.Update(requisition);
                    }

                    await transaction.CommitAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return false;
                }
            });
        }

        public async Task<bool> ReceiveStepAsync(int approvalId, string comments, string receiverPayrollNo)
        {
            _logger.LogInformation($"Receiving step with ID {approvalId}");
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var approval = await _context.Approvals
                        .Include(a => a.Requisition)
                        .Include(a => a.StepConfig)
                        .FirstOrDefaultAsync(a => a.Id == approvalId);

                    if (approval == null)
                        return false;

                    if (approval.ApprovalStatus != ApprovalStatus.PendingReceive && approval.ApprovalStatus != ApprovalStatus.PendingApproval)
                        return false;

                    approval.ApprovalStatus = ApprovalStatus.Received;
                    approval.Comments = comments;
                    approval.UpdatedAt = DateTime.Now;
                    _context.Update(approval);

                    var deliveryStation = await _departmentService.GetStationByIdAsync(approval.Requisition.DeliveryStationId);
                    string deliveryStationName = deliveryStation?.StationName ?? "Unknown";

                    await _context.SaveChangesAsync();

                    await _notificationManager.NotifyApprovalStepReceived(approval, receiverPayrollNo, deliveryStationName);

                    await HandleNextStepOrCompleteRequisitionAsync(approval, DetermineNextStepStatus(await GetNextStepAsync(approval.RequisitionId, approval.StepNumber)));

                    var requisition = approval.Requisition;
                    if (requisition != null)
                    {
                        // If this is the last step, set to Completed
                        var nextStep = await GetNextStepAsync(approval.RequisitionId, approval.StepNumber);
                        if (nextStep == null)
                        {
                            requisition.Status = RequisitionStatus.Completed;
                        }
                        requisition.UpdatedAt = DateTime.Now;
                        _context.Update(requisition);
                    }

                    await transaction.CommitAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return false;
                }
            });
        }

        public async Task<bool> PutOnHoldStepAsync(int approvalId, string comments, string putOnHoldByPayrollNo)
        {
            _logger.LogInformation($"Putting step with ID {approvalId} on hold");
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var approval = await _context.Approvals
                        .Include(a => a.Requisition)
                        .Include(a => a.StepConfig)
                        .FirstOrDefaultAsync(a => a.Id == approvalId);

                    if (approval == null)
                        return false;

                    approval.ApprovalStatus = ApprovalStatus.OnHold;
                    approval.Comments = comments;
                    approval.UpdatedAt = DateTime.Now;
                    _context.Update(approval);

                    await _context.SaveChangesAsync();

                    await _notificationManager.NotifyApprovalStepOnHold(approval, putOnHoldByPayrollNo, comments);

                    await transaction.CommitAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return false;
                }
            });
        }

        /// <summary>
        /// Handles the next step in the workflow or completes the requisition if this is the last step
        /// </summary>
        /// <param name="approval">The current approval that was processed</param>
        /// <param name="nextStepStatus">The status to set for the next step (if any)</param>
        /// <returns>True if successful, false otherwise</returns>
        private async Task<bool> HandleNextStepOrCompleteRequisitionAsync(Approval approval, ApprovalStatus nextStepStatus)
        {
            // Get the next step
            var nextStep = await GetNextStepAsync(approval.RequisitionId, approval.StepNumber);

            if (nextStep != null)
            {
                // Activate the next step
                _logger.LogInformation($"Activating next step ID {nextStep.Id} for requisition {approval.RequisitionId}");
                nextStep.ApprovalStatus = nextStepStatus;
                nextStep.UpdatedAt = DateTime.Now;

                _context.Update(nextStep);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Next step activated: ID: {nextStep.Id}, Step: {nextStep.ApprovalStep}, Status: {nextStepStatus}");
                return true;
            }
            else
            {
                // This was the last step, complete the requisition and update material location
                _logger.LogInformation($"No next step found - this was the last step. Completing requisition {approval.RequisitionId}");
                
                var requisition = approval.Requisition;
                if (requisition != null)
                {
                    // Update requisition status
                    requisition.Status = RequisitionStatus.Completed;
                    requisition.CompleteDate = DateTime.Now;
                    requisition.UpdatedAt = DateTime.Now;
                    
                    _context.Update(requisition);
                    
                    // Update material locations for all items in the requisition
                    await UpdateMaterialLocationsAsync(requisition, approval.PayrollNo);
                    
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation($"Requisition {requisition.Id} marked as Completed and material locations updated");
                    return true;
                }
                else
                {
                    _logger.LogWarning($"Requisition is null for approval ID {approval.Id}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Updates the current location of materials associated with a completed requisition
        /// </summary>
        /// <param name="requisition">The completed requisition</param>
        private async Task UpdateMaterialLocationsAsync(Requisition requisition, string loggedInPayrollNo)
        {
            try
            {
                // Get all requisition items with material IDs
                var requisitionItems = await _context.RequisitionItems
                    .Include(ri => ri.Material)
                    .Where(ri => ri.RequisitionId == requisition.Id && ri.MaterialId.HasValue)
                    .ToListAsync();
                
                if (requisitionItems.Any())
                {
                    // Get the delivery station and department IDs
                    var deliveryStationId = requisition.DeliveryStationId;
                    var deliveryDepartmentId = requisition.DeliveryDepartmentId;
                    string locationName = await _departmentService.GetLocationNameFromIdsAsync(deliveryStationId, deliveryDepartmentId);
                    
                    if (deliveryStationId != 0)
                    {   
                        foreach (var item in requisitionItems)
                        {
                            if (item.MaterialId.HasValue && item.Material != null)
                            {
                                // Update material status to Assigned
                                item.Material.Status = MaterialStatus.Assigned;
                                item.Material.UpdatedAt = DateTime.Now;
                                _context.Update(item.Material);


                                // First, deactivate any existing active assignments for this material
                                var existingAssignments = await _context.MaterialAssignments
                                    .Where(ma => ma.MaterialId == item.MaterialId.Value && ma.IsActive)
                                    .ToListAsync();
                                
                                foreach (var existing in existingAssignments)
                                {
                                    existing.IsActive = false;
                                    existing.ReturnDate = DateTime.UtcNow;
                                    _context.MaterialAssignments.Update(existing);
                                }

                                // Create a new material assignment to track the location
                                var materialAssignment = new MaterialAssignment
                                {
                                    MaterialId = item.MaterialId.Value,
                                    PayrollNo = loggedInPayrollNo,
                                    AssignmentDate = DateTime.UtcNow,
                                    StationCategory = requisition.DeliveryStationCategory,
                                    StationId = requisition.IssueStationId,
                                    DepartmentId = requisition.DepartmentId,
                                    AssignmentType = requisition.RequisitionType,
                                    RequisitionId = requisition.Id,
                                    AssignedByPayrollNo = requisition.PayrollNo,
                                    IsActive = true
                                };

                                
                                
                                _context.MaterialAssignments.Add(materialAssignment);
                                
                                _logger.LogInformation($"Updated material ID {item.MaterialId} status to Assigned and created new location assignment at station ID {deliveryStationId}");
                                
                                // If there's a specific employee assigned, send them a notification
                                if (!string.IsNullOrEmpty(materialAssignment.PayrollNo))
                                {
                                    await _notificationService.CreateNotificationAsync(
                                        "MaterialAssigned",
                                        new Dictionary<string, string>
                                        {
                                            { "MaterialName", item.Material.Name ?? "Unknown Material" },
                                            { "MaterialCode", item.Material.Code ?? "N/A" },
                                            { "RequisitionId", requisition.Id.ToString() },
                                            { "DeliveryStation", locationName }
                                        },
                                        materialAssignment.PayrollNo
                                    );
                                }
                            }
                        }
                        
                        _logger.LogInformation($"Updated statuses and created location assignments for {requisitionItems.Count} materials to location ID {deliveryStationId}");
                        
                        // Send notification about requisition completion
                        var creator = await _employeeService.GetEmployeeByPayrollAsync(requisition.PayrollNo);
                        if (creator != null)
                        {
                            await _notificationService.CreateNotificationAsync(
                                "RequisitionCompleted",
                                new Dictionary<string, string>  
                                {
                                    { "RequisitionId", requisition.Id.ToString() },
                                    { "DeliveryStation", locationName }
                                },
                                creator.PayrollNo
                            );
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Could not find location ID for delivery station {requisition.DeliveryStationId}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating material locations for requisition {requisition.Id}");
            }
        }

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
            // If approval is null, return empty list
            if (approval == null)
            {
                return new List<ApprovalStatus>();
            }

            // Check if we have custom status options from the step configuration
            if (approval.StepConfig?.Conditions != null && 
                approval.StepConfig.Conditions.TryGetValue("ValidStatusTransitions", out var statusOptionsString))
            {
                try
                {
                    var customStatuses = statusOptionsString.Split(',')
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .Select(s => (ApprovalStatus)int.Parse(s))
                        .ToList();

                    if (customStatuses.Any())
                    {
                        return customStatuses;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parsing approval status options");
                    // Continue to default status mapping
                }
            }

            // If no custom configuration, map based on current approval status
            var currentStatus = approval.ApprovalStatus;
            
            // Map status options based on current approval status
            switch (currentStatus)
            {
                case ApprovalStatus.PendingApproval:
                    return new List<ApprovalStatus>
                    {
                        ApprovalStatus.Approved,
                        ApprovalStatus.Rejected,
                        ApprovalStatus.OnHold
                    };
                
                case ApprovalStatus.PendingDispatch:
                    return new List<ApprovalStatus>
                    {
                        ApprovalStatus.Dispatched,
                        ApprovalStatus.Rejected,
                        ApprovalStatus.OnHold
                    };
                
                case ApprovalStatus.PendingReceive:
                    return new List<ApprovalStatus>
                    {
                        ApprovalStatus.Received,
                        ApprovalStatus.Rejected,
                        ApprovalStatus.OnHold
                    };
                
                default:
                    // Default fallback statuses for any other status
                    return new List<ApprovalStatus>
                    {
                        ApprovalStatus.Approved,
                        ApprovalStatus.Rejected,
                        ApprovalStatus.Dispatched,
                        ApprovalStatus.Received,
                        ApprovalStatus.OnHold
                    };
            }
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
#endregion