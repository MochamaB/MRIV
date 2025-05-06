using System.ComponentModel.DataAnnotations;

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
    }
}
