using System;
using System.ComponentModel.DataAnnotations;

namespace MRIV.ViewModels
{
    public class EmployeeViewModel
    {
        public string PayrollNo { get; set; }
        public string RollNo { get; set; }
        public string EmployeeId { get; set; }
        public string FullName { get; set; }
        public string Designation { get; set; }
        
        [Display(Name = "Department")]
        public string Department { get; set; }
        public string DepartmentName { get; set; }
        
        [Display(Name = "Station")]
        public string Station { get; set; }
        public string StationName { get; set; }
        
        [Display(Name = "Role")]
        public string Role { get; set; }
        
        public string EmailAddress { get; set; }
        public string MobileNumber { get; set; }
        
        public int EmpisCurrActive { get; set; }
    }
}
