using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace MRIV.ViewModels
{
    public class MaterialImportViewModel
    {
        [Required]
        [Display(Name = "Import File")]
        public IFormFile ImportFile { get; set; }

        public List<MaterialImportResult> Results { get; set; } = new List<MaterialImportResult>();
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public int TotalRows { get; set; }
        public bool HasResults { get; set; }
        public string ImportType { get; set; } // "Category", "Subcategory", "Material"
    }

    public class MaterialImportResult
    {
        public int RowNumber { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string AdditionalInfo { get; set; } // UnitOfMeasure for categories, CategoryName for subcategories, etc.
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class MaterialCategoryImportRow
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string UnitOfMeasure { get; set; }
    }

    public class MaterialSubcategoryImportRow
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string CategoryName { get; set; }
    }
}
