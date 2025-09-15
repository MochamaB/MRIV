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

        [Display(Name = "Station Category")]
        public string StationCategoryCode { get; set; }
        public string StationCategoryName { get; set; }
        
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
        
        // Additional fields from EmployeeDetailsView
        public string SurName { get; set; }
        public string OtherNames { get; set; }
        public string SupervisorName { get; set; }
        public string HeadOfDepartmentName { get; set; }
        public string Scale { get; set; }
        
        public int EmpisCurrActive { get; set; }
        
        // Helper properties for UI
        public string Initials => GetInitials();
        public string StatusBadge => EmpisCurrActive == 0 ? "Active" : "Inactive";
        public string StatusClass => EmpisCurrActive == 0 ? "badge-success" : "badge-danger";
        
        private string GetInitials()
        {
            if (string.IsNullOrEmpty(FullName))
                return "??";
                
            var parts = FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            else if (parts.Length == 1)
                return $"{parts[0][0]}{parts[0][0]}".ToUpper();
            else
                return "??";
        }
    }
}
