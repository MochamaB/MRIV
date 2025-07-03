using System.ComponentModel.DataAnnotations;

namespace MRIV.ViewModels
{
    public class EmployeeSearchViewModel
    {
        [Display(Name = "Station Category")]
        public string StationCategory { get; set; }

        [Display(Name = "Station")]
        public int? StationId { get; set; }

        [Display(Name = "Department")]
        public int? DepartmentId { get; set; }

        [Display(Name = "Role")]
        public string Role { get; set; }

        [Display(Name = "Employee Name")]
        public string EmployeeName { get; set; }
    }
}
