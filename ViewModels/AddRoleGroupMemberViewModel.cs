using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace MRIV.ViewModels
{
    public class AddRoleGroupMemberViewModel
    {
        public int RoleGroupId { get; set; }
        
        [Display(Name = "Role Group")]
        public string RoleGroupName { get; set; }
        
        [Required]
        [Display(Name = "Payroll Number")]
        [StringLength(8, ErrorMessage = "Payroll number must be 8 characters.")]
        public string PayrollNo { get; set; }
        
        // Search parameters
        [Display(Name = "Station Category")]
        public string StationCategory { get; set; }
        
        [Display(Name = "Station")]
        public int? StationId { get; set; }
        
        [Display(Name = "Department")]
        public int? DepartmentId { get; set; }
        
        [Display(Name = "Role")]
        public string Role { get; set; }
        
        [Display(Name = "Employee Name")]
        public string SearchTerm { get; set; }
        
        // Lists for dropdowns
        public List<SelectListItem> StationCategories { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "", Text = "-- Select Category --" },
            new SelectListItem { Value = "HQ", Text = "Head Office (HQ)" },
            new SelectListItem { Value = "Factory", Text = "Factory" },
            new SelectListItem { Value = "Region", Text = "Region" }
        };
        
        public List<SelectListItem> Stations { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Departments { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Roles { get; set; } = new List<SelectListItem>();
    }
}
