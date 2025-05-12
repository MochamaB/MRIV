# MRIV System Controller Changes
# Updates for Location-Based Authorization

## Table of Contents
1. [MaterialRequisitionController Changes](#materialrequisitioncontroller-changes)
2. [ApprovalsController Changes](#approvalscontroller-changes)
3. [JavaScript and View Model Updates](#javascript-and-view-model-updates)

## MaterialRequisitionController Changes

The MaterialRequisitionController needs significant updates to work with the new location model:

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MRIV.Attributes;
using MRIV.Enums;
using MRIV.Models;
using MRIV.Services;
using MRIV.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MRIV.Controllers
{
    [CustomAuthorize]
    public class MaterialRequisitionController : Controller
    {
        private readonly IEmployeeService _employeeService;
        private readonly ILocationService _locationService;
        private readonly RequisitionContext _context;
        private readonly IApprovalService _approvalService;
        private readonly INotificationService _notificationService;
        private readonly IRoleGroupAccessService _roleGroupService;
        
        public MaterialRequisitionController(
            IEmployeeService employeeService,
            ILocationService locationService,
            RequisitionContext context,
            IApprovalService approvalService,
            INotificationService notificationService,
            IRoleGroupAccessService roleGroupService)
        {
            _employeeService = employeeService;
            _locationService = locationService;
            _context = context;
            _approvalService = approvalService;
            _notificationService = notificationService;
            _roleGroupService = roleGroupService;
        }

        // ... existing methods ...

        // Updated method to initialize location data
        private async Task InitializeUserLocationAsync(Requisition requisition, EmployeeBkp employee)
        {
            // Only initialize if values are not already set
            if (requisition.IssueLocationType != 0 && 
                (requisition.IssueDepartmentId.HasValue || 
                 requisition.IssueStationId.HasValue || 
                 requisition.IssueVendorId.HasValue))
            {
                return;
            }

            // Get user's department and station
            var (department, station) = await _locationService.GetUserLocationsAsync(employee.PayrollNo);

            // Determine location type based on user's location
            if (station != null && station.StationName?.ToLower() == "hq")
            {
                // User is at head office, set department as issue location
                requisition.IssueLocationType = LocationType.Department;
                
                if (department != null)
                {
                    requisition.IssueDepartmentId = department.Id;
                }
            }
            else if (station != null)
            {
                // User is at a station (factory or region), set station as issue location
                requisition.IssueLocationType = LocationType.Station;
                requisition.IssueStationId = station.Id;
            }
            
            // Set default visibility scope
            requisition.VisibilityScope = VisibilityScope.Department;
        }

        // Updated method for the requisition details step
        public async Task<IActionResult> RequisitionDetailsAsync()
        {
            // Retrieve the requisition object from the session
            var requisition = HttpContext.Session.GetObject<Requisition>("WizardRequisition");
        
            if (requisition == null)
            {
                return RedirectToAction("TicketAsync");
            }

            var viewModel = await GetWizardViewModelAsync(currentStep: 2, requisition);
            
            // If there are validation errors from a previous post, restore the model state
            if (TempData["ValidationErrors"] is string errorsJson)
            {
                try
                {
                    var errors = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string[]>>(errorsJson);
                    if (errors != null)
                    {
                        foreach (var error in errors)
                        {
                            foreach (var message in error.Value)
                            {
                                ModelState.AddModelError(error.Key, message);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deserializing validation errors");
                }
            }
            
            // Initialize user location if not already set
            await InitializeUserLocationAsync(requisition, viewModel.LoggedInUserEmployee);
            
            // Update session with any changes
            HttpContext.Session.SetObject("WizardRequisition", requisition);
            
            // Prepare location select lists
            viewModel.DepartmentSelectList = await _locationService.GetDepartmentsSelectListAsync(requisition.IssueDepartmentId);
            viewModel.StationSelectList = await _locationService.GetStationsSelectListAsync(requisition.IssueStationId);
            viewModel.VendorSelectList = await _locationService.GetVendorsSelectListAsync(requisition.IssueVendorId);
            
            viewModel.DeliveryDepartmentSelectList = await _locationService.GetDepartmentsSelectListAsync(requisition.DeliveryDepartmentId);
            viewModel.DeliveryStationSelectList = await _locationService.GetStationsSelectListAsync(requisition.DeliveryStationId);
            viewModel.DeliveryVendorSelectList = await _locationService.GetVendorsSelectListAsync(requisition.DeliveryVendorId);
            
            // Prepare location type select list
            viewModel.LocationTypeSelectList = new SelectList(
                new[]
                {
                    new { Value = LocationType.Department.ToString("d"), Text = "Department" },
                    new { Value = LocationType.Station.ToString("d"), Text = "Station" },
                    new { Value = LocationType.Vendor.ToString("d"), Text = "Vendor" }
                },
                "Value", "Text", ((int)requisition.IssueLocationType).ToString());
                
            viewModel.DeliveryLocationTypeSelectList = new SelectList(
                new[]
                {
                    new { Value = LocationType.Department.ToString("d"), Text = "Department" },
                    new { Value = LocationType.Station.ToString("d"), Text = "Station" },
                    new { Value = LocationType.Vendor.ToString("d"), Text = "Vendor" }
                },
                "Value", "Text", ((int)requisition.DeliveryLocationType).ToString());
                
            // Prepare visibility scope select list
            viewModel.VisibilityScopeSelectList = new SelectList(
                new[]
                {
                    new { Value = VisibilityScope.Department.ToString("d"), Text = "Department Only" },
                    new { Value = VisibilityScope.Station.ToString("d"), Text = "Station Wide" },
                    new { Value = VisibilityScope.Global.ToString("d"), Text = "Global" }
                },
                "Value", "Text", ((int)requisition.VisibilityScope).ToString());
            
            return View(WizardViewPath, viewModel);
        }

        // Updated method to save requisition details
        [HttpPost]
        public async Task<IActionResult> CreateRequisitionAsync(MaterialRequisitionWizardViewModel model)
        {
            // Get the requisition from the session
            var requisition = HttpContext.Session.GetObject<Requisition>("WizardRequisition");
            
            if (requisition == null)
            {
                return RedirectToAction("TicketAsync");
            }
            
            // Update requisition with form data
            requisition.RequisitionType = model.Requisition.RequisitionType;
            requisition.IssueLocationType = model.Requisition.IssueLocationType;
            requisition.DeliveryLocationType = model.Requisition.DeliveryLocationType;
            requisition.VisibilityScope = model.Requisition.VisibilityScope;
            requisition.Remarks = model.Requisition.Remarks;
            
            // Set location IDs based on location type
            switch (requisition.IssueLocationType)
            {
                case LocationType.Department:
                    requisition.IssueDepartmentId = model.Requisition.IssueDepartmentId;
                    requisition.IssueStationId = null;
                    requisition.IssueVendorId = null;
                    break;
                    
                case LocationType.Station:
                    requisition.IssueDepartmentId = null;
                    requisition.IssueStationId = model.Requisition.IssueStationId;
                    requisition.IssueVendorId = null;
                    break;
                    
                case LocationType.Vendor:
                    requisition.IssueDepartmentId = null;
                    requisition.IssueStationId = null;
                    requisition.IssueVendorId = model.Requisition.IssueVendorId;
                    break;
            }
            
            // Set delivery location IDs based on location type
            switch (requisition.DeliveryLocationType)
            {
                case LocationType.Department:
                    requisition.DeliveryDepartmentId = model.Requisition.DeliveryDepartmentId;
                    requisition.DeliveryStationId = null;
                    requisition.DeliveryVendorId = null;
                    break;
                    
                case LocationType.Station:
                    requisition.DeliveryDepartmentId = null;
                    requisition.DeliveryStationId = model.Requisition.DeliveryStationId;
                    requisition.DeliveryVendorId = null;
                    break;
                    
                case LocationType.Vendor:
                    requisition.DeliveryDepartmentId = null;
                    requisition.DeliveryStationId = null;
                    requisition.DeliveryVendorId = model.Requisition.DeliveryVendorId;
                    break;
            }
            
            // Validate the model
            if (!ModelState.IsValid)
            {
                // Store validation errors in TempData
                var errors = ModelState
                    .Where(e => e.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                    
                TempData["ValidationErrors"] = System.Text.Json.JsonSerializer.Serialize(errors);
                
                // Redirect back to the form
                return RedirectToAction("RequisitionDetails");
            }
            
            // Update session with the updated requisition
            HttpContext.Session.SetObject("WizardRequisition", requisition);
            
            // Proceed to the next step
            return RedirectToAction("RequisitionItems");
        }

        // ... other methods ...

        // Updated method to create approvals
        private async Task<List<Approval>> CreateApprovals(Requisition requisition, List<WorkflowStepConfig> steps)
        {
            var approvals = new List<Approval>();
            
            // Get user's department and station
            var payrollNo = HttpContext.Session.GetString("EmployeePayrollNo");
            var (department, station) = await _locationService.GetUserLocationsAsync(payrollNo);
            
            foreach (var step in steps)
            {
                var approval = new Approval
                {
                    RequisitionId = requisition.Id,
                    DepartmentId = requisition.DepartmentId,
                    Step = step.Id,
                    ApprovalStatus = ApprovalStatus.Pending,
                    CreatedAt = DateTime.Now,
                    VisibilityScope = requisition.VisibilityScope // Inherit from requisition
                };
                
                // Set station ID based on requisition's location
                if (requisition.IssueLocationType == LocationType.Station && requisition.IssueStationId.HasValue)
                {
                    approval.StationId = requisition.IssueStationId.Value;
                }
                else if (requisition.DeliveryLocationType == LocationType.Station && requisition.DeliveryStationId.HasValue)
                {
                    approval.StationId = requisition.DeliveryStationId.Value;
                }
                else if (station != null)
                {
                    approval.StationId = station.Id;
                }
                
                approvals.Add(approval);
            }
            
            return approvals;
        }
    }
}
```

## ApprovalsController Changes

The ApprovalsController needs updates to work with the new location model and to record who approved:

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MRIV.Attributes;
using MRIV.Enums;
using MRIV.Models;
using MRIV.Services;
using MRIV.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MRIV.Controllers
{
    [CustomAuthorize]
    public class ApprovalsController : Controller
    {
        private readonly RequisitionContext _context;
        private readonly IEmployeeService _employeeService;
        private readonly ILocationService _locationService;
        private readonly IVisibilityAuthorizeService _visibilityService;
        private readonly INotificationService _notificationService;
        private readonly IRoleGroupAccessService _roleGroupService;
        private readonly ILogger<ApprovalsController> _logger;
        
        public ApprovalsController(
            RequisitionContext context,
            IEmployeeService employeeService,
            ILocationService locationService,
            IVisibilityAuthorizeService visibilityService,
            INotificationService notificationService,
            IRoleGroupAccessService roleGroupService,
            ILogger<ApprovalsController> logger)
        {
            _context = context;
            _employeeService = employeeService;
            _locationService = locationService;
            _visibilityService = visibilityService;
            _notificationService = notificationService;
            _roleGroupService = roleGroupService;
            _logger = logger;
        }

        // GET: Approvals
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string sortField = null, string sortOrder = "desc", string searchTerm = null)
        {
            var userPayrollNo = HttpContext.Session.GetString("EmployeePayrollNo");
            if (userPayrollNo == null)
                return RedirectToAction("Index", "Login");

            // Ensure valid pagination parameters
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : (pageSize > 100 ? 100 : pageSize);

            // Get filter values from request query string
            var filters = new Dictionary<string, string>();
            foreach (var key in Request.Query.Keys.Where(k => k != "page" && k != "pageSize" && k != "sortField" && k != "sortOrder" && k != "searchTerm"))
            {
                if (!string.IsNullOrEmpty(Request.Query[key]))
                {
                    filters[key] = Request.Query[key];
                }
            }

            // Start with base query
            var approvals = _context.Approvals
                .Include(a => a.Requisition)
                .Include(a => a.StepConfig)
                .AsQueryable();

            // Apply location-based filtering
            var filteredApprovals = await _visibilityService.ApplyLocationScopeAsync(approvals, userPayrollNo, true, true);

            // Apply search term if provided
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                filteredApprovals = filteredApprovals.Where(a =>
                    a.Requisition.PayrollNo.ToLower().Contains(searchTerm) ||
                    a.Comments != null && a.Comments.ToLower().Contains(searchTerm) ||
                    a.ApprovalStatus.ToString().ToLower().Contains(searchTerm) ||
                    a.ApprovedBy != null && a.ApprovedBy.ToLower().Contains(searchTerm));
            }

            // Apply ApprovalStatus filter if present
            if (filters.TryGetValue("ApprovalStatus", out var approvalStatusStr) &&
                Enum.TryParse<ApprovalStatus>(approvalStatusStr, out var approvalStatus))
            {
                filteredApprovals = filteredApprovals.Where(a => a.ApprovalStatus == approvalStatus);
            }

            // Apply sorting
            if (!string.IsNullOrEmpty(sortField))
            {
                switch (sortField.ToLower())
                {
                    case "id":
                        filteredApprovals = sortOrder.ToLower() == "desc"
                            ? filteredApprovals.OrderByDescending(a => a.Id)
                            : filteredApprovals.OrderBy(a => a.Id);
                        break;
                    case "status":
                        filteredApprovals = sortOrder.ToLower() == "desc"
                            ? filteredApprovals.OrderByDescending(a => a.ApprovalStatus)
                            : filteredApprovals.OrderBy(a => a.ApprovalStatus);
                        break;
                    case "approvedby":
                        filteredApprovals = sortOrder.ToLower() == "desc"
                            ? filteredApprovals.OrderByDescending(a => a.ApprovedBy)
                            : filteredApprovals.OrderBy(a => a.ApprovedBy);
                        break;
                    case "createdat":
                    default:
                        filteredApprovals = sortOrder.ToLower() == "desc"
                            ? filteredApprovals.OrderByDescending(a => a.CreatedAt)
                            : filteredApprovals.OrderBy(a => a.CreatedAt);
                        break;
                }
            }
            else
            {
                // Default sorting
                filteredApprovals = filteredApprovals.OrderByDescending(a => a.CreatedAt);
            }

            // Get total count for pagination
            var totalItems = await filteredApprovals.CountAsync();

            // Apply pagination
            var pagedApprovals = await filteredApprovals
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Create view model
            var viewModel = new ApprovalListViewModel
            {
                Approvals = pagedApprovals,
                PaginationInfo = new PaginationViewModel
                {
                    CurrentPage = page,
                    ItemsPerPage = pageSize,
                    TotalItems = totalItems,
                    Action = "Index",
                    Controller = "Approvals"
                },
                SortField = sortField,
                SortOrder = sortOrder,
                SearchTerm = searchTerm,
                Filters = filters
            };

            // Load location names
            foreach (var approval in pagedApprovals)
            {
                // Load department name
                approval.DepartmentName = await _locationService.GetDepartmentNameAsync(approval.DepartmentId);
                
                // Load station name if available
                if (approval.StationId.HasValue)
                {
                    approval.StationName = await _locationService.GetStationNameAsync(approval.StationId.Value);
                }
                
                // Load approver name if available
                if (!string.IsNullOrEmpty(approval.ApprovedBy))
                {
                    var approver = await _employeeService.GetEmployeeByPayrollAsync(approval.ApprovedBy);
                    approval.ApproverName = approver?.Fullname ?? approval.ApprovedBy;
                }
            }

            return View(viewModel);
        }

        // ... other methods ...

        // POST: Approvals/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, ApprovalViewModel model)
        {
            var approval = await _context.Approvals
                .Include(a => a.Requisition)
                .Include(a => a.StepConfig)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (approval == null)
            {
                return NotFound();
            }

            // Get current user's payroll number
            var userPayrollNo = HttpContext.Session.GetString("EmployeePayrollNo");
            if (userPayrollNo == null)
            {
                return RedirectToAction("Index", "Login");
            }

            // Check if user can approve this step
            bool canApprove = await _visibilityService.UserHasRoleAsync(userPayrollNo, approval.StepConfig.RoleParameters);
            if (!canApprove)
            {
                TempData["ErrorMessage"] = "You do not have permission to approve this step.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Check if step should be restricted to the requisition creator
            bool restrictToPayroll = await _visibilityService.ShouldRestrictToPayrollAsync(approval.StepConfig, userPayrollNo);
            if (restrictToPayroll && approval.Requisition.PayrollNo != userPayrollNo)
            {
                TempData["ErrorMessage"] = "This approval step can only be completed by the requisition creator.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Update approval status
            approval.ApprovalStatus = model.ApprovalAction == "approve" ? ApprovalStatus.Approved : ApprovalStatus.Rejected;
            approval.Comments = model.Comments;
            approval.UpdatedAt = DateTime.Now;
            
            // Record who approved
            approval.ApprovedBy = userPayrollNo;
            approval.ApprovedAt = DateTime.Now;

            _context.Update(approval);
            await _context.SaveChangesAsync();

            // Send notifications
            await _notificationService.SendApprovalNotificationAsync(approval);

            // Update requisition status if needed
            await UpdateRequisitionStatusAsync(approval.RequisitionId);

            TempData["SuccessMessage"] = $"Approval has been {(model.ApprovalAction == "approve" ? "approved" : "rejected")}.";
            return RedirectToAction(nameof(Index));
        }

        // ... other methods ...
    }
}
```

## JavaScript and View Model Updates

### MaterialRequisitionWizardViewModel Updates

```csharp
public class MaterialRequisitionWizardViewModel
{
    // Existing properties...
    
    // New properties for location selection
    public SelectList LocationTypeSelectList { get; set; }
    public SelectList DepartmentSelectList { get; set; }
    public SelectList StationSelectList { get; set; }
    public SelectList VendorSelectList { get; set; }
    
    public SelectList DeliveryLocationTypeSelectList { get; set; }
    public SelectList DeliveryDepartmentSelectList { get; set; }
    public SelectList DeliveryStationSelectList { get; set; }
    public SelectList DeliveryVendorSelectList { get; set; }
    
    public SelectList VisibilityScopeSelectList { get; set; }
}
```

### JavaScript for Location Type Selection

Add this JavaScript to handle dynamic location type selection in the wizard:

```javascript
// Add this to the requisition details page
$(document).ready(function() {
    // Function to toggle location fields based on location type
    function toggleLocationFields(locationType, prefix) {
        // Hide all location fields first
        $(`#${prefix}DepartmentContainer`).hide();
        $(`#${prefix}StationContainer`).hide();
        $(`#${prefix}VendorContainer`).hide();
        
        // Show the appropriate field based on location type
        switch (locationType) {
            case "1": // Department
                $(`#${prefix}DepartmentContainer`).show();
                break;
            case "2": // Station
                $(`#${prefix}StationContainer`).show();
                break;
            case "3": // Vendor
                $(`#${prefix}VendorContainer`).show();
                break;
        }
    }
    
    // Initialize fields based on current values
    toggleLocationFields($("#IssueLocationType").val(), "Issue");
    toggleLocationFields($("#DeliveryLocationType").val(), "Delivery");
    
    // Add change handlers
    $("#IssueLocationType").change(function() {
        toggleLocationFields($(this).val(), "Issue");
    });
    
    $("#DeliveryLocationType").change(function() {
        toggleLocationFields($(this).val(), "Delivery");
    });
    
    // Handle visibility scope changes
    $("#VisibilityScope").change(function() {
        var scope = $(this).val();
        
        // Show appropriate help text based on scope
        $(".visibility-help").hide();
        $(`.visibility-help[data-scope="${scope}"]`).show();
    });
    
    // Initialize visibility help text
    $(".visibility-help").hide();
    $(`.visibility-help[data-scope="${$("#VisibilityScope").val()}"]`).show();
});
```

### Requisition Details View Updates

Update the requisition details partial view to support the new location model:

```html
<div class="row mb-3">
    <div class="col-md-6">
        <div class="form-group">
            <label asp-for="Requisition.IssueLocationType" class="form-label">Issue Location Type</label>
            <select asp-for="Requisition.IssueLocationType" asp-items="Model.LocationTypeSelectList" class="form-select">
                <option value="">-- Select Location Type --</option>
            </select>
            <span asp-validation-for="Requisition.IssueLocationType" class="text-danger"></span>
        </div>
        
        <div id="IssueDepartmentContainer" class="form-group mt-3">
            <label asp-for="Requisition.IssueDepartmentId" class="form-label">Issue Department</label>
            <select asp-for="Requisition.IssueDepartmentId" asp-items="Model.DepartmentSelectList" class="form-select">
                <option value="">-- Select Department --</option>
            </select>
            <span asp-validation-for="Requisition.IssueDepartmentId" class="text-danger"></span>
        </div>
        
        <div id="IssueStationContainer" class="form-group mt-3">
            <label asp-for="Requisition.IssueStationId" class="form-label">Issue Station</label>
            <select asp-for="Requisition.IssueStationId" asp-items="Model.StationSelectList" class="form-select">
                <option value="">-- Select Station --</option>
            </select>
            <span asp-validation-for="Requisition.IssueStationId" class="text-danger"></span>
        </div>
        
        <div id="IssueVendorContainer" class="form-group mt-3">
            <label asp-for="Requisition.IssueVendorId" class="form-label">Issue Vendor</label>
            <select asp-for="Requisition.IssueVendorId" asp-items="Model.VendorSelectList" class="form-select">
                <option value="">-- Select Vendor --</option>
            </select>
            <span asp-validation-for="Requisition.IssueVendorId" class="text-danger"></span>
        </div>
    </div>
    
    <div class="col-md-6">
        <div class="form-group">
            <label asp-for="Requisition.DeliveryLocationType" class="form-label">Delivery Location Type</label>
            <select asp-for="Requisition.DeliveryLocationType" asp-items="Model.DeliveryLocationTypeSelectList" class="form-select">
                <option value="">-- Select Location Type --</option>
            </select>
            <span asp-validation-for="Requisition.DeliveryLocationType" class="text-danger"></span>
        </div>
        
        <div id="DeliveryDepartmentContainer" class="form-group mt-3">
            <label asp-for="Requisition.DeliveryDepartmentId" class="form-label">Delivery Department</label>
            <select asp-for="Requisition.DeliveryDepartmentId" asp-items="Model.DeliveryDepartmentSelectList" class="form-select">
                <option value="">-- Select Department --</option>
            </select>
            <span asp-validation-for="Requisition.DeliveryDepartmentId" class="text-danger"></span>
        </div>
        
        <div id="DeliveryStationContainer" class="form-group mt-3">
            <label asp-for="Requisition.DeliveryStationId" class="form-label">Delivery Station</label>
            <select asp-for="Requisition.DeliveryStationId" asp-items="Model.DeliveryStationSelectList" class="form-select">
                <option value="">-- Select Station --</option>
            </select>
            <span asp-validation-for="Requisition.DeliveryStationId" class="text-danger"></span>
        </div>
        
        <div id="DeliveryVendorContainer" class="form-group mt-3">
            <label asp-for="Requisition.DeliveryVendorId" class="form-label">Delivery Vendor</label>
            <select asp-for="Requisition.DeliveryVendorId" asp-items="Model.DeliveryVendorSelectList" class="form-select">
                <option value="">-- Select Vendor --</option>
            </select>
            <span asp-validation-for="Requisition.DeliveryVendorId" class="text-danger"></span>
        </div>
    </div>
</div>

<div class="row mb-3">
    <div class="col-md-6">
        <div class="form-group">
            <label asp-for="Requisition.VisibilityScope" class="form-label">Visibility Scope</label>
            <select asp-for="Requisition.VisibilityScope" asp-items="Model.VisibilityScopeSelectList" class="form-select">
                <option value="">-- Select Visibility Scope --</option>
            </select>
            <span asp-validation-for="Requisition.VisibilityScope" class="text-danger"></span>
            
            <div class="visibility-help text-muted mt-2" data-scope="1">
                <small>Department Only: Only visible to users in the same department</small>
            </div>
            <div class="visibility-help text-muted mt-2" data-scope="2">
                <small>Station Wide: Visible to all users in the same station, regardless of department</small>
            </div>
            <div class="visibility-help text-muted mt-2" data-scope="3">
                <small>Global: Visible to all users with appropriate roles</small>
            </div>
        </div>
    </div>
</div>
```
