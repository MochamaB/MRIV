using System.Net.Sockets;

namespace MRIV.ViewModels
{
    public class MaterialRequisitionWizardViewModel
    {
        public List<WizardStepViewModel> Steps { get; set; }
        public int CurrentStep { get; set; }
        public string PartialBasePath { get; set; } // Example: "~/Views/Shared/CreateRequisition/"
        public List<Ticket> Tickets { get; set; } // Add this to hold the ticket data
        public int TotalCount { get; set; }
    }
    public class Ticket
    {
        public int RequestID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }
}
