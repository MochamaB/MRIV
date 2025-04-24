using Microsoft.AspNetCore.Mvc.Rendering;
using MRIV.Enums;
using MRIV.Models;
using System.ComponentModel.DataAnnotations;

namespace MRIV.ViewModels
{
    public class CreateMaterialViewModel
    {
        // Material properties
        public Material Material { get; set; } = new Material();
        
        // Material Assignment properties
        public MaterialAssignment Assignment { get; set; } = new MaterialAssignment();

        // Dropdown lists for the form
        public SelectList? MaterialCategories { get; set; }
        public SelectList? MaterialSubcategories { get; set; }
        public SelectList? StationCategories { get; set; }
        public SelectList? LocationOptions { get; set; }
        public SelectList? Departments { get; set; }
        public SelectList? Vendors { get; set; }
        public SelectList? StatusOptions { get; set; }
        public SelectList? AssignmentTypeOptions { get; set; }

        // Helper properties for the form
        public string? SelectedLocationCategory { get; set; }
        
        // For compatibility with existing views
        [Display(Name = "Current Location")]
        public string? CurrentLocationId { get; set; }
        
        // Constructor to initialize the status options
        public CreateMaterialViewModel()
        {
            // Initialize the status options from the enum
            StatusOptions = new SelectList(
                Enum.GetValues(typeof(MaterialStatus))
                    .Cast<MaterialStatus>()
                    .Select(s => new { Value = (int)s, Text = s.ToString() }),
                "Value", "Text");
                
            // Initialize assignment type options
            AssignmentTypeOptions = new SelectList(
                Enum.GetValues(typeof(AssignmentType))
                    .Cast<AssignmentType>()
                    .Select(s => new { Value = (int)s, Text = s.ToString() }),
                "Value", "Text");
                
            // Set default assignment type
            Assignment.AssignmentType = AssignmentType.New;
        }
    }
}
