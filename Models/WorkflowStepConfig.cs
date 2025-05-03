using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

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

        [Required, StringLength(100)]
        public string StepAction { get; set; }

        [Required, StringLength(50)]
        public string ApproverRole { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public Dictionary<string, string> RoleParameters { get; set; } = new();

        [Column(TypeName = "nvarchar(max)")]
        public Dictionary<string, string> Conditions { get; set; } = new();

        [ForeignKey(nameof(WorkflowConfigId))]
        public virtual WorkflowConfig? WorkflowConfig { get; set; }

        // Add these properties for JSON binding (not mapped to database)
        [NotMapped]
        public string? RoleParametersJson
        {
            get => RoleParameters != null ? JsonSerializer.Serialize(RoleParameters) : null;
            set
            {
                if (!string.IsNullOrEmpty(value))
                    RoleParameters = JsonSerializer.Deserialize<Dictionary<string, string>>(value) ?? new();
            }
        }
        [NotMapped]
        public string? ConditionsJson
        {
            get => Conditions != null ? JsonSerializer.Serialize(Conditions) : null;
            set
            {
                if (!string.IsNullOrEmpty(value))
                    Conditions = JsonSerializer.Deserialize<Dictionary<string, string>>(value) ?? new();
            }
        }
    }
}
