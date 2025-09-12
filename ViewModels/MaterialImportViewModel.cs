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
        public int FailureCount { get; set; }
        public int TotalRows { get; set; }
        public int TotalProcessed { get; set; }
        public bool HasResults { get; set; }
        public string ImportType { get; set; } // "Category", "Subcategory", "Material"
    }

    public class MaterialImportResult
    {
        public int RowNumber { get; set; }
        public string Name { get; set; }
        public string ItemName { get; set; }
        public string Description { get; set; }
        public string Message { get; set; }
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

    public class MaterialImportRow
    {
        // Core Material Data
        public string Name { get; set; }
        public string CategoryName { get; set; }
        public string SubcategoryName { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }

        // Extended Material Data
        public string Manufacturer { get; set; }
        public string ModelNumber { get; set; }
        public string Specifications { get; set; }
        public string AssetTag { get; set; }
        public string QRCODE { get; set; }
        public string VendorId { get; set; }
        public string PurchaseDate { get; set; }
        public string PurchasePrice { get; set; }
        public string WarrantyStartDate { get; set; }
        public string WarrantyEndDate { get; set; }
        public string WarrantyTerms { get; set; }
        public string ExpectedLifespanMonths { get; set; }
        public string MaintenanceIntervalMonths { get; set; }

        // Assignment Data
        public string AssignedToPayrollNo { get; set; }
        public string StationCategory { get; set; }
        public string StationName { get; set; }
        public string DepartmentName { get; set; }
        public string SpecificLocation { get; set; }
        public string AssignmentType { get; set; }
        public string AssignmentNotes { get; set; }

        // Condition Data
        public string ConditionStatus { get; set; }
        public string ConditionNotes { get; set; }
        public string? InspectionDate { get; set; }
        public string ConditionCheckType { get; set; }
        public string Stage { get; set; }
    }
}
