 using MRIV.Enums;
using MRIV.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MRIV.ViewModels
{
    public class ApprovalSummaryViewModel
    {
        // Core properties
        public int ApprovalId { get; set; }
        public int RequisitionId { get; set; }
        public string? ApprovalStep { get; set; }
        public int StepNumber { get; set; }

        // Requisition details
        public int RequisitionNumber { get; set; }
        public string? IssueStationCategory { get; set; }
        public int RequestingDepartment { get; set; }
        public int? RequestingStation { get; set; }
        public string? DeliveryStationCategory { get; set; }
        public int? DeliveryStation { get; set; }
        public DateTime RequestDate { get; set; }
        public string? Status { get; set; }
        public string? DepartmentName { get; set; }
        public string? RequestingEmployeeName { get; set; }
        public string? IssueStationName { get; set; }
        public string? DeliveryStationName { get; set; }
        public string? DeliveryDepartmentName { get; set; }
        public string? Remarks { get; set; }
        public string? DispatchType { get; set; }
        public string? DispatcherName { get; set; }
        public string? VendorName { get; set; }
        public string? CollectorName { get; set; }
        public string? CollectorId { get; set; }

        // Approval action
        public string? Action { get; set; }
        [StringLength(500, ErrorMessage = "Comments cannot exceed 500 characters")]
        [Attributes.RequiredIfRejected("Action", ErrorMessage = "Comments are required when rejecting an approval or putting it on hold.")]
        public string? Comments { get; set; }
        public Dictionary<string, string> StatusOptions { get; set; } = new Dictionary<string, string>();

        // Approval steps (current and history)
        public List<ApprovalStepViewModel> CurrentApprovalSteps { get; set; } = new List<ApprovalStepViewModel>();
        public List<ApprovalStepViewModel> ApprovalHistory { get; set; } = new List<ApprovalStepViewModel>();

        // Requisition items
        public List<RequisitionItemConditionViewModel> RequisitionItems { get; set; } = new List<RequisitionItemConditionViewModel>();
    }
}
