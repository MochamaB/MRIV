using MRIV.Enums;
using MRIV.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MRIV.ViewModels
{
    public class ApprovalWizardViewModel
    {
        // Core properties
        public int ApprovalId { get; set; }
        public int RequisitionId { get; set; }
        public string? ApprovalStep { get; set; } // Make nullable to avoid null ref exceptions
        public int StepNumber { get; set; }

        // Requisition details
        public int RequisitionNumber { get; set; }
        public string? IssueStationCategory { get; set; }
        public int RequestingDepartment { get; set; }
        public string? RequestingStation { get; set; }
        public string? DeliveryStationCategory { get; set; }
        public string? DeliveryStation { get; set; }
        public DateTime RequestDate { get; set; }
        public string? Status { get; set; }

        // Wizard navigation properties
        public string CurrentStep { get; set; } = "ItemConditions";
        public bool IsLastStep { get; set; }

        // Action properties
        public string? Action { get; set; }
        public string? Comments { get; set; }

        // Collections
        public List<RequisitionItemConditionViewModel> Items { get; set; } = new List<RequisitionItemConditionViewModel>();
        public List<ApprovalHistoryViewModel> ApprovalHistory { get; set; } = new List<ApprovalHistoryViewModel>();
    }

    public class RequisitionItemConditionViewModel
    {
        public int RequisitionItemId { get; set; }
        public int? MaterialConditionId { get; set; }
        public int? MaterialId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int Quantity { get; set; }
        public string? MaterialCode { get; set; }
        public RequisitionItemCondition RequisitionItemCondition { get; set; }
        public MaterialStatus? Condition { get; set; }
        public string? Notes { get; set; }
        public string? Stage { get; set; }
        public Vendor? vendor { get; set; }
    }

    public class ApprovalHistoryViewModel
    {
        public int ApprovalId { get; set; }
        public string? ApprovalStep { get; set; }
        public int? StepNumber { get; set; }
        public string? Status { get; set; }
        public string? ApproverName { get; set; }
        public string? Comments { get; set; }
        public DateTime? ApprovedDate { get; set; }
    }
}
