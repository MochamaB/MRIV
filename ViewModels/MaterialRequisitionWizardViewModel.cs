using Microsoft.AspNetCore.Mvc.Rendering;
using MRIV.Attributes;
using MRIV.Enums;
using MRIV.Models;
using System.ComponentModel.DataAnnotations;
using System.Net.Sockets;

namespace MRIV.ViewModels
{
    [InterFactoryBorrowingValidator]
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

        //  new properties for station categories
        public SelectList? IssueStationCategories { get; set; }
        public SelectList? DeliveryStationCategories { get; set; }

        //  dynamic locations based on selected category
        public SelectList? IssueLocations { get; set; }
        public SelectList? DeliveryLocations { get; set; }
        public EmployeeBkp? LoggedInUserEmployee { get; set; } 
        public Department? LoggedInUserDepartment { get; set; }
        public Station? LoggedInUserStation { get; set; }

        public EmployeeBkp? employeeDetail { get; set; }
        public Department? departmentDetail { get; set; }
        public Station? stationDetail { get; set; }
        public List<Vendor> Vendors { get; set; } = new();
        public List<RequisitionItem> RequisitionItems { get; set; } = new();
        public List<MaterialCategory> MaterialCategories { get; set; } = new();
        public MaterialCategory? MaterialCategory { get; set; }
        public List<Material> Materials { get; set; } = new List<Material>();

        public List<ApprovalStepViewModel>? ApprovalSteps { get; set; }
        public Dictionary<string, SelectList>? DepartmentEmployees { get; set; }

        public EmployeeBkp? dispatchEmployee { get; set; }
        public Vendor? vendor { get; set; }

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
        public int Id { get; set; }  // Add this
        public int RequisitionId { get; set; }  // Add this
        public string EmployeeDesignation { get; set; }  // Add this


        public int? StepNumber { get; set; }
        public string ApprovalStep { get; set; }
        public string PayrollNo { get; set; }
        public string EmployeeName { get; set; }
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public ApprovalStatus ApprovalStatus { get; set; }
        public DateTime CreatedAt { get; set; }

        // New properties for selection
        public SelectList? AvailableDepartments { get; set; }
        public SelectList? AvailableStations { get; set; }
        public int? SelectedDepartmentId { get; set; }
        public string? SelectedStation { get; set; }
    }


}
