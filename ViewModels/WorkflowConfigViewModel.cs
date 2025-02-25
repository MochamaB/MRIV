using Microsoft.AspNetCore.Mvc.Rendering;
using MRIV.Models;

namespace MRIV.ViewModels
{
    public class WorkflowConfigViewModel
    {
        public WorkflowConfig WorkflowConfig { get; set; }
        public List<WorkflowStepConfig> Steps { get; set; }
        public SelectList StationCategories { get; set; }
        public SelectList ApproverRoles { get; set; }
    }
}
