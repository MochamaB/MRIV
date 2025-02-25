using System.ComponentModel.DataAnnotations;

namespace MRIV.Models
{
    public class WorkflowConfig
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string IssueStationCategory { get; set; }

        [Required, StringLength(50)]
        public string DeliveryStationCategory { get; set; }

        public virtual ICollection<WorkflowStepConfig> Steps { get; set; } = new List<WorkflowStepConfig>();
    }
}
