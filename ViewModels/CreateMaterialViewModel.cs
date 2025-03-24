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

        // Dropdown lists for the form
        public SelectList? MaterialCategories { get; set; }
        public SelectList? StationCategories { get; set; }
        public SelectList? LocationOptions { get; set; }
        public SelectList? Vendors { get; set; }
        public SelectList? StatusOptions { get; set; }

        // Helper properties for the form
        public string? SelectedLocationCategory { get; set; }
        
        // Constructor to initialize the status options
        public CreateMaterialViewModel()
        {
            // Initialize the status options from the enum
            StatusOptions = new SelectList(
                Enum.GetValues(typeof(MaterialStatus))
                    .Cast<MaterialStatus>()
                    .Select(s => new { Value = (int)s, Text = s.ToString() }),
                "Value", "Text");
        }
    }
}
