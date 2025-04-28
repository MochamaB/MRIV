using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using MRIV.Enums;
using MRIV.Models;
using System.ComponentModel.DataAnnotations;

namespace MRIV.ViewModels
{
    public class EditMaterialViewModel
    {
        // Material properties
        public Material Material { get; set; } = new Material();
        
        // Material Assignment properties
        public MaterialAssignment Assignment { get; set; } = new MaterialAssignment();
        
        // Material Condition for recording changes
        public MaterialCondition Condition { get; set; } = new MaterialCondition();

        // Image handling properties
        public IFormFile? ImageFile { get; set; }
        public List<IFormFile>? GalleryFiles { get; set; }
        public MediaFile? ExistingMainImage { get; set; }
        public List<MediaFile>? ExistingGalleryImages { get; set; } = new List<MediaFile>();

        // Dropdown lists for the form
        public SelectList? MaterialCategories { get; set; }
        public SelectList? MaterialSubcategories { get; set; }
        public SelectList? StationCategories { get; set; }
        public SelectList? LocationOptions { get; set; }
        public SelectList? Departments { get; set; }
        public SelectList? Vendors { get; set; }
        public SelectList? StatusOptions { get; set; }
        public SelectList? AssignmentTypeOptions { get; set; }
        public SelectList? FunctionalStatusOptions { get; set; }
        public SelectList? CosmeticStatusOptions { get; set; }

        // Helper properties for the form
        public string? SelectedLocationCategory { get; set; }
        
        // For compatibility with existing views
        [Display(Name = "Current Location")]
        public string? CurrentLocationId { get; set; }
        
        // Assignment history
        public IEnumerable<MaterialAssignment>? AssignmentHistory { get; set; }
        
        // Condition history
        public IEnumerable<MaterialCondition>? ConditionHistory { get; set; }
        
        // Constructor to initialize the status options
        public EditMaterialViewModel()
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
                
            // Initialize functional status options
            FunctionalStatusOptions = new SelectList(
                Enum.GetValues(typeof(FunctionalStatus))
                    .Cast<FunctionalStatus>()
                    .Select(s => new { Value = (int)s, Text = s.ToString() }),
                "Value", "Text");
                
            // Initialize cosmetic status options
            CosmeticStatusOptions = new SelectList(
                Enum.GetValues(typeof(CosmeticStatus))
                    .Cast<CosmeticStatus>()
                    .Select(s => new { Value = (int)s, Text = s.ToString() }),
                "Value", "Text");
        }
    }
}
