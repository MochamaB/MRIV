using MRIV.Attributes;
using MRIV.Enums;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace MRIV.ViewModels
{
    public class ApprovalActionViewModel
    {
        public int Id { get; set; }
        
        public int RequisitionId { get; set; }
        
        public string ApprovalStep { get; set; }
        
        public string? EmployeeName { get; set; }
        
        public string? DepartmentName { get; set; }
        
        public ApprovalStatus CurrentStatus { get; set; }

        [Required(ErrorMessage = "Please select an action")]
        [Display(Name = "Action")]
        public string Action { get; set; }

        [StringLength(500, ErrorMessage = "Comments cannot exceed 500 characters")]
        [RequiredIfRejected("Action", ErrorMessage = "Comments are required when rejecting an approval or putting it on hold.")]
        public string? Comments { get; set; }

        // Additional properties for display
        public string? IssueCategory { get; set; }
        public string? IssueStation {  get; set; }
        public string? DeliveryCategory { get; set; }
        public string? DeliveryStation { get; set; }
        public string? RequisitionDetails { get; set; }
        
        public int StepNumber { get; set; }
        
        public List<ApprovalStepViewModel> ApprovalHistory { get; set; } = new List<ApprovalStepViewModel>();
    }
}
