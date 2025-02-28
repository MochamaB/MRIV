using MRIV.ViewModels;

namespace MRIV.Models
{
    // Models/WizardStepBase.cs
    public abstract class WizardStepBase
    {
        public List<WizardStepViewModel> Steps { get; set; } = new();
        public int CurrentStep { get; set; }
        public string PartialBasePath { get; set; } = "~/Views/Shared/CreateRequisition/";

        // Navigation methods
        public virtual string GetPreviousStepAction()
        {
            if (CurrentStep <= 1) return null;

            var steps = new[] { "Ticket", "RequisitionDetails", "RequisitionItems", "ApproversReceivers", "WizardSummary" };
            return steps[CurrentStep - 2]; // -2 because of zero indexing and we want previous
        }

        public virtual string GetNextStepAction()
        {
            var steps = new[] { "Ticket", "RequisitionDetails", "RequisitionItems", "ApproversReceivers", "WizardSummary" };
            if (CurrentStep >= steps.Length) return null;

            return steps[CurrentStep];
        }
    }
}
