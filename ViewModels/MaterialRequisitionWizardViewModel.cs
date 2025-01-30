using MRIV.Models;
using System.Net.Sockets;

namespace MRIV.ViewModels
{
    public class MaterialRequisitionWizardViewModel
    {
        public List<WizardStepViewModel> Steps { get; set; }
        public int CurrentStep { get; set; }
        public string PartialBasePath { get; set; } // Example: "~/Views/Shared/CreateRequisition/"
        public List<Ticket> Tickets { get; set; } // Add this to hold the ticket data
        public List<Station> Stations { get; set; }
        public List<Department> Departments { get; set; }
        public List<EmployeeBkp> EmployeeBkps { get; set; }
        public int TotalCount { get; set; }
        public Requisition Requisition { get; set; }
        public EmployeeBkp Employee { get; set; }
        public Department Department { get; set; }
        public Station Station { get; set; }
        // Existing properties
        public List<Vendor> Vendors { get; set; }
    }
    public class Ticket
    {
        public int RequestID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }

    public class Vendor
    {
        public int VendorID { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Location { get; set; }
        public bool IsNullRecord { get; set; }
        public DateTime LastModified { get; set; }
    }
}
