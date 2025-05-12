using MRIV.Models;

namespace MRIV.ViewModels
{
    public class RequisitionDetailsViewModel
    {
        public Requisition? Requisition { get; set; }
        public EmployeeBkp? EmployeeDetail { get; set; }
        public Department? DepartmentDetail { get; set; }
        public string IssueStationCategory { get; set; }
        public string? IssueStation { get; set; }
        public string? IssueDepartment { get; set; }
       
        public string DeliveryStationCategory { get; set; }
        public string? DeliveryStation { get; set; }
        public string? DeliveryDepartment { get; set; }
        public List<RequisitionItem>? RequisitionItems { get; set; }
        public List<ApprovalStepViewModel>? ApprovalSteps { get; set; }
        public EmployeeBkp? DispatchEmployee { get; set; }
        public Vendor? Vendor { get; set; }
        public List<Vendor> Vendors { get; set; } = new();
    }
}
