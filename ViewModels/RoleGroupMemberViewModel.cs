using System.ComponentModel.DataAnnotations;

namespace MRIV.ViewModels
{
    public class RoleGroupMemberViewModel
    {
        public int Id { get; set; }
        
        public int RoleGroupId { get; set; }
        
        [Display(Name = "Payroll No")]
        public string PayrollNo { get; set; }
        
        [Display(Name = "Employee Name")]
        public string EmployeeName { get; set; }
        
        [Display(Name = "Role")]
        public string EmployeeRole { get; set; }
        
        [Display(Name = "Department")]
        public string Department { get; set; }
        
        [Display(Name = "Station")]
        public string Station { get; set; }
        
        [Display(Name = "Active")]
        public bool IsActive { get; set; }
    }
}
