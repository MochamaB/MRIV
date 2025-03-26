using System.Collections.Generic;

namespace MRIV.Models.ViewModels
{
    public class StepParametersViewModel
    {
        public ParameterCollection RoleParameters { get; set; } = new ParameterCollection();
        public ParameterCollection Conditions { get; set; } = new ParameterCollection();
    }

    public class ParameterCollection
    {
        public List<string> Keys { get; set; } = new List<string>();
        public List<string> Values { get; set; } = new List<string>();
    }
}
