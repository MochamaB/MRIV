using System.ComponentModel.DataAnnotations;

namespace MRIV.ViewModels
{
    public class EmployeeSearchResultViewModel
    {
        public string PayrollNo { get; set; }
        public string Name { get; set; }
        public string Designation { get; set; }
        public string Department { get; set; }
        public string Role { get; set; }
    }
}
