using Microsoft.AspNetCore.Mvc.Rendering;
using MRIV.Models;
using System.Net.Sockets;

namespace MRIV.ViewModels
{
    public class MaterialRequisitionWizardViewModel
    {
        public List<WizardStepViewModel> Steps { get; set; } = new();
        public int CurrentStep { get; set; }
        public string PartialBasePath { get; set; } = string.Empty;
        public List<Ticket> Tickets { get; set; } = new();
        public List<Station> Stations { get; set; } = new();
        public List<Department> Departments { get; set; } = new();
        public List<EmployeeBkp> EmployeeBkps { get; set; } = new();
        public int? TotalCount { get; set; }
        public Requisition? Requisition { get; set; }
        public EmployeeBkp? LoggedInUserEmployee { get; set; } 
        public Department? LoggedInUserDepartment { get; set; }
        public Station? LoggedInUserStation { get; set; }
        public List<Vendor> Vendors { get; set; } = new();
        public List<RequisitionItem> RequisitionItems { get; set; } = new();
        public List<MaterialCategory> MaterialCategories { get; set; } = new();
        public MaterialCategory? MaterialCategory { get; set; }
        public List<Material> Materials { get; set; } = new List<Material>();

        public List<ApprovalStepViewModel>? ApprovalSteps { get; set; }
        public Dictionary<string, SelectList>? DepartmentEmployees { get; set; }
    }
    public class Ticket
    {
        public int RequestID { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
    }

    public class Vendor
    {
        public int VendorID { get; set; }
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Location { get; set; }
        public bool? IsNullRecord { get; set; }
        public DateTime? LastModified { get; set; }
    }

    public class ApprovalStepViewModel
    {
        public int? StepNumber { get; set; }
        public string ApprovalStep { get; set; }
        public string PayrollNo { get; set; }
        public string EmployeeName { get; set; }
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public string ApprovalStatus { get; set; }
        public DateTime CreatedAt { get; set; }
    }


}


