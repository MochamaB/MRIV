using MRIV.Enums;
using MRIV.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace MRIV.ViewModels
{
    public class MaterialDisplayViewModel
    {
        // Basic Material Information
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        
        // Category Information
        public int MaterialCategoryId { get; set; }
        public string MaterialCategoryName { get; set; }
        public int? MaterialSubcategoryId { get; set; }
        public string MaterialSubcategoryName { get; set; }
        
        // Vendor/Supplier Information
        public string VendorId { get; set; }
        public string VendorName { get; set; }
        
        // Status and Location
        public MaterialStatus? Status { get; set; }
        public string StatusDisplayName => Status?.ToString() ?? "Unknown";
        
        // Current Assignment Information
        public int? CurrentAssignmentId { get; set; }
        public string? AssignedToPayrollNo { get; set; }
        public string? AssignedToName { get; set; }
        public DateTime? AssignmentDate { get; set; }
        public string? AssignmentType { get; set; }
        public string? AssignedByPayrollNo { get; set; }
        public string? AssignedByName { get; set; }
        public string? AssignmentNotes { get; set; }
        
        // Location Information
        public string? StationCategory { get; set; }
        public int? StationId { get; set; }
        public string? StationName { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public string? SpecificLocation { get; set; }
        
        // Current Condition Information
        public int? CurrentConditionId { get; set; }
        public Condition? CurrentCondition { get; set; }
        public string CurrentConditionDisplayName => CurrentCondition?.ToString() ?? "Unknown";
        public FunctionalStatus? FunctionalStatus { get; set; }
        public string FunctionalStatusDisplayName => FunctionalStatus?.ToString() ?? "Unknown";
        public CosmeticStatus? CosmeticStatus { get; set; }
        public string CosmeticStatusDisplayName => CosmeticStatus?.ToString() ?? "Unknown";
        public string? InspectedBy { get; set; }
        public DateTime? InspectionDate { get; set; }
        public string? ConditionNotes { get; set; }
        
        // Purchase Information
        public DateTime? PurchaseDate { get; set; }
        public decimal? PurchasePrice { get; set; }
        
        // Warranty Information
        public DateTime? WarrantyStartDate { get; set; }
        public DateTime? WarrantyEndDate { get; set; }
        public string? WarrantyTerms { get; set; }
        public bool IsUnderWarranty => WarrantyEndDate.HasValue && WarrantyEndDate.Value > DateTime.Today;
        
        // Lifecycle Management
        public int? ExpectedLifespanMonths { get; set; }
        public int? MaintenanceIntervalMonths { get; set; }
        public DateTime? LastMaintenanceDate { get; set; }
        public DateTime? NextMaintenanceDate { get; set; }
        public bool MaintenanceDue => NextMaintenanceDate.HasValue && NextMaintenanceDate.Value <= DateTime.Today;
        
        // Additional Metadata
        public string? Manufacturer { get; set; }
        public string? ModelNumber { get; set; }
        public string? QRCode { get; set; }
        public string? AssetTag { get; set; }
        public string? Specifications { get; set; }
        
        // Timestamps
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        // Media
        public string? MainImagePath { get; set; }
        public List<string> GalleryImagePaths { get; set; } = new List<string>();
        
        // Related Collections
        public IEnumerable<MaterialAssignmentViewModel> AssignmentHistory { get; set; } = new List<MaterialAssignmentViewModel>();
        public IEnumerable<MaterialConditionViewModel> ConditionHistory { get; set; } = new List<MaterialConditionViewModel>();
        public IEnumerable<RequisitionItemViewModel> RequisitionHistory { get; set; } = new List<RequisitionItemViewModel>();
    }
    
    public class MaterialAssignmentViewModel
    {
        public int Id { get; set; }
        public int MaterialId { get; set; }
        public string? PayrollNo { get; set; }
        public string? AssignedToName { get; set; }
        public DateTime AssignmentDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string? StationCategory { get; set; }
        public int? StationId { get; set; }
        public string? StationName { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public string? SpecificLocation { get; set; }
        public RequisitionType AssignmentType { get; set; }
        public string AssignmentTypeDisplayName => AssignmentType.ToString();
        public int? RequisitionId { get; set; }
        public string? AssignedByPayrollNo { get; set; }
        public string? AssignedByName { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }
    }
    
    public class MaterialConditionViewModel
    {
        public int Id { get; set; }
        public int? MaterialId { get; set; }
        public int? MaterialAssignmentId { get; set; }
        public int? RequisitionId { get; set; }
        public int? RequisitionItemId { get; set; }
        public int? ApprovalId { get; set; }
        public ConditionCheckType? ConditionCheckType { get; set; }
        public string ConditionCheckTypeDisplayName => ConditionCheckType?.ToString() ?? "Unknown";
        public string? Stage { get; set; }
        public Condition? Condition { get; set; }
        public string ConditionDisplayName => Condition?.ToString() ?? "Unknown";
        public FunctionalStatus? FunctionalStatus { get; set; }
        public string FunctionalStatusDisplayName => FunctionalStatus?.ToString() ?? "Unknown";
        public CosmeticStatus? CosmeticStatus { get; set; }
        public string CosmeticStatusDisplayName => CosmeticStatus?.ToString() ?? "Unknown";
        public string? ComponentStatuses { get; set; }
        public string? Notes { get; set; }
        public string? InspectedBy { get; set; }
        public string? InspectedByName { get; set; }
        public DateTime? InspectionDate { get; set; }
        public string? ActionRequired { get; set; }
        public DateTime? ActionDueDate { get; set; }
        public List<string> ImagePaths { get; set; } = new List<string>();
    }
    
    public class RequisitionItemViewModel
    {
        public int Id { get; set; }
        public int RequisitionId { get; set; }
        public int? MaterialId { get; set; }
        public string? MaterialName { get; set; }
        public string? MaterialCode { get; set; }
        public string? Description { get; set; }
        public int Quantity { get; set; }
        public RequisitionItemCondition RequisitionItemCondition {  get; set; }
        public RequisitionItemStatus RequisitionItemStatus { get; set; }
        public int SaveToInventory { get; set; }

        public string? Vendor { get; set; }
        public DateTime? RequestDate { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public MaterialConditionViewModel CurrentCondition { get; internal set; }
    }
}
