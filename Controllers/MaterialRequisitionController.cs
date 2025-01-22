using Microsoft.AspNetCore.Mvc;
using MRIV.ViewModels;

namespace MRIV.Controllers
{
    public class MaterialRequisitionController : Controller
    {
        private const string WizardViewPath = "~/Views/Wizard/NumberWizard.cshtml";

        private List<string> GetSteps()
        {
            return new List<string> { "Ticket", "Requisition Details", "Requisition Items", "Approvers & Recievers", "Summary" };
        }
        private List<WizardStepViewModel> GetWizardSteps(int currentStep)
        {
            return GetSteps().Select((step, index) => new WizardStepViewModel
            {
                StepName = step,
                StepNumber = index + 1,
                IsActive = index + 1 == currentStep,
                IsCompleted = index + 1 < currentStep
            }).ToList();
        }

        [HttpGet]
        public IActionResult Ticket()
        {
            var steps = GetWizardSteps(currentStep: 1); // Pass the current step
            var viewModel = new MaterialRequisitionWizardViewModel
            {
                Steps = steps,
                CurrentStep = 1
            };
            ViewBag.Steps = GetSteps();
            ViewBag.CurrentStep = "Ticket";
            ViewBag.CurrentStepIndex = 0;

            return View(WizardViewPath, viewModel);
        }
    }
}
