using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MRIV.Models
{
    public class WorkflowStepConfig
    {
        public int Id { get; set; }

        public int WorkflowConfigId { get; set; }

        [Required]
        public int StepOrder { get; set; }

        [Required, StringLength(100)]
        public string StepName { get; set; }

        [Required, StringLength(50)]
        public string ApproverRole { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public Dictionary<string, string> RoleParameters { get; set; } = new();

        [Column(TypeName = "nvarchar(max)")]
        public Dictionary<string, string> Conditions { get; set; } = new();

        [ForeignKey(nameof(WorkflowConfigId))]
        public virtual WorkflowConfig? WorkflowConfig { get; set; }
    }
}
