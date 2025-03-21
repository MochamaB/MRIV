﻿using MRIV.Models;
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
            // Get all approvals for this requisition
            var approvals = await _context.Approvals
                .Where(a => a.RequisitionId == requisitionId)
                .OrderBy(a => a.StepNumber)
                .ToListAsync();

            return GetMostSignificantApproval(approvals);
        }

        // Helper method for internal use
        private Approval GetMostSignificantApproval(List<Approval> approvals)
        {
            if (approvals == null || !approvals.Any())
                return null;

            // Priority order: PendingApproval, Completed, Rejected, Forwarded
            var pendingApproval = approvals.FirstOrDefault(a => a.ApprovalStatus == ApprovalStatus.PendingApproval);
            if (pendingApproval != null)
                return pendingApproval;

            var completed = approvals.FirstOrDefault(a => a.ApprovalStatus == ApprovalStatus.Completed);
            if (completed != null)
                return completed;

            var rejected = approvals.FirstOrDefault(a => a.ApprovalStatus == ApprovalStatus.Rejected);
            if (rejected != null)
                return rejected;

            var forwarded = approvals.FirstOrDefault(a => a.ApprovalStatus == ApprovalStatus.Forwarded);
            if (forwarded != null)
                return forwarded;

            // If none of the specific statuses are found, return the first approval
            return approvals.FirstOrDefault();
        }

    }
}
