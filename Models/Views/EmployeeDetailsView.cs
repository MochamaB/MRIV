using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MRIV.Models.Views
{
    /// <summary>
    /// Entity model for vw_EmployeeDetails database view
    /// Provides complete employee information with resolved department/station names
    /// </summary>
    [Table("vw_EmployeeDetails")]
    public class EmployeeDetailsView
    {
        [Key]
        [StringLength(8)]
        public string PayrollNo { get; set; }

        [StringLength(100)]
        public string? Fullname { get; set; }

        [StringLength(50)]
        public string? SurName { get; set; }

        [StringLength(50)]
        public string? OtherNames { get; set; }

        // Department information (both integer ID and string code)
        public int? DepartmentId { get; set; }         // Integer primary key from Department table

        [StringLength(50)]
        public string? DepartmentCode { get; set; }     // String business code from Employee_bkp

        [StringLength(100)]
        public string? DepartmentName { get; set; }

        // Station information with category integration
        public int? StationId { get; set; }            // Station ID with HQ = 0 mapping

        [StringLength(50)]
        public string? OriginalStationName { get; set; } // Original station value from ktdaleave

        [StringLength(50)]
        public string? StationName { get; set; }        // Resolved station name

        [StringLength(50)]
        public string? StationCategoryCode { get; set; }

        [StringLength(50)]
        public string? StationCategoryName { get; set; }

        // Employee status and role information
        public int IsActive { get; set; }              // 0 = Active in ktdaleave

        [StringLength(20)]
        public string? Role { get; set; }

        [StringLength(50)]
        public string? Designation { get; set; }

        [Column("Email_Address")]
        [StringLength(100)]
        public string? EmailAddress { get; set; }

        [Column("scale")]
        [StringLength(50)]
        public string? Scale { get; set; }

        [StringLength(5)]
        public string? RollNo { get; set; }

        // Hierarchy information
        [StringLength(50)]
        public string? HeadOfDepartment { get; set; }   // PayrollNo of HOD

        [StringLength(50)]
        public string? Supervisor { get; set; }         // PayrollNo of Supervisor

        [StringLength(100)]
        public string? HeadOfDepartmentName { get; set; }

        [StringLength(100)]
        public string? SupervisorName { get; set; }
    }
}