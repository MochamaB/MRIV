using Microsoft.AspNetCore.Mvc.Rendering;
using MRIV.Enums;
using MRIV.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

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
        public SelectList? Stations { get; set; }
        public SelectList? Departments { get; set; }
        public SelectList? Vendors { get; set; }
        public SelectList? StatusOptions { get; set; }
        public SelectList? AssignmentTypeOptions { get; set; }

        // Helper properties for the form
        public string? SelectedLocationCategory { get; set; }
        public string? SelectedStation { get; set; }
        public string? SelectedDepartment { get; set; }

        // For compatibility with existing views
        [Display(Name = "Station")]
        public int? StationId { get; set; }
        
        // For image uploads
        public IFormFile? ImageFile { get; set; }
        
        // For gallery images
        public List<IFormFile>? GalleryFiles { get; set; }
        
        // For displaying existing media
        public MediaFile? ExistingImage { get; set; }
        public List<MediaFile>? GalleryImages { get; set; }
        
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
                Enum.GetValues(typeof(RequisitionType))
                    .Cast<RequisitionType>()
                    .Select(s => new { Value = (int)s, Text = s.ToString() }),
                "Value", "Text");
                
            // Set default assignment type
            Assignment.AssignmentType = RequisitionType.NewPurchase;
            
            // Initialize gallery images list
            GalleryImages = new List<MediaFile>();
            GalleryFiles = new List<IFormFile>();
        }
    }
}
