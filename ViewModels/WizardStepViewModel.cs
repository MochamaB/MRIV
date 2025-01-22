namespace MRIV.ViewModels
{
    public class WizardStepViewModel
    {
        public string StepName { get; set; }
        public int StepNumber { get; set; }
        public bool IsActive { get; set; }
        public bool IsCompleted { get; set; }
    }
}
