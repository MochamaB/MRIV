using MRIV.Enums;

namespace MRIV.ViewModels
{
    public class RequisitionViewModel
    {
        // Original requisition properties
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string IssueStationCategory { get; set; }
        public string IssueStation { get; set; }
        public string IssueDepartment { get; set; }
        public string DeliveryStationCategory { get; set; }
        public string DeliveryStation { get; set; }
        public string DeliveryDepartment { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? CompleteDate { get; set; }
        public RequisitionStatus? Status { get; set; }

        // Display properties (derived from other contexts)
        public string DepartmentName { get; set; }
        public string EmployeeName { get; set; }
        public string IssueStationName { get; set; }
        public string DeliveryLocationName { get; set; }
        public int DaysPending { get; set; }

        // Add approval information
        public int? CurrentApprovalStepNumber { get; set; }
        public string CurrentApprovalStepName { get; set; }
        public string CurrentApproverName { get; set; }
        public string CurrentApproverDesignation { get; set; }
        public ApprovalStatus CurrentApprovalStatus { get; set; }
    }
}
