# MRIV System Data Model Changes
# Location-Based Authorization and Data Model Improvements

## Table of Contents
1. [New Enums](#new-enums)
2. [Requisition Model Changes](#requisition-model-changes)
3. [Approval Model Changes](#approval-model-changes)
4. [Database Migration Strategy](#database-migration-strategy)

## New Enums

Create new enums to standardize location types:

```csharp
namespace MRIV.Enums
{
    /// <summary>
    /// Defines the type of location (Department, Station, Vendor)
    /// </summary>
    public enum LocationType
    {
        Department = 1,
        Station = 2,
        Vendor = 3
    }

    /// <summary>
    /// Defines the scope of visibility for approvals and requisitions
    /// </summary>
    public enum VisibilityScope
    {
        Department = 1,  // Visible only to the specific department
        Station = 2,     // Visible to all departments in the station
        Global = 3       // Visible to all users with appropriate roles
    }
}
```

## Requisition Model Changes

Update the Requisition model to use ID-based location references:

```csharp
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MRIV.Enums;

namespace MRIV.Models
{
    public class Requisition
    {
        public int Id { get; set; }

        public int TicketId { get; set; }

        [Required]
        public int DepartmentId { get; set; }

        [Required]
        [StringLength(20)]
        public string PayrollNo { get; set; }

        [Required]
        [Display(Name = "Requisition Type")]
        public RequisitionType RequisitionType { get; set; }

        // Issue Location (Source)
        [Required]
        [Display(Name = "Issue Location Type")]
        public LocationType IssueLocationType { get; set; }
        
        // If IssueLocationType is Department
        public int? IssueDepartmentId { get; set; }
        
        [ForeignKey("IssueDepartmentId")]
        public Department IssueDepartment { get; set; }
        
        // If IssueLocationType is Station
        public int? IssueStationId { get; set; }
        
        [ForeignKey("IssueStationId")]
        public Station IssueStation { get; set; }
        
        // If IssueLocationType is Vendor
        public int? IssueVendorId { get; set; }
        
        [ForeignKey("IssueVendorId")]
        public Vendor IssueVendor { get; set; }

        // Delivery Location (Destination)
        [Required]
        [Display(Name = "Delivery Location Type")]
        public LocationType DeliveryLocationType { get; set; }
        
        // If DeliveryLocationType is Department
        public int? DeliveryDepartmentId { get; set; }
        
        [ForeignKey("DeliveryDepartmentId")]
        public Department DeliveryDepartment { get; set; }
        
        // If DeliveryLocationType is Station
        public int? DeliveryStationId { get; set; }
        
        [ForeignKey("DeliveryStationId")]
        public Station DeliveryStation { get; set; }
        
        // If DeliveryLocationType is Vendor
        public int? DeliveryVendorId { get; set; }
        
        [ForeignKey("DeliveryVendorId")]
        public Vendor DeliveryVendor { get; set; }

        // Visibility scope for this requisition
        [Required]
        [Display(Name = "Visibility Scope")]
        public VisibilityScope VisibilityScope { get; set; } = VisibilityScope.Department;

        [StringLength(500)]
        public string Remarks { get; set; }

        [StringLength(20)]
        [RequiredIfSectionVisible]
        [Display(Name = "Dispatched By")]
        public string? DispatchType { get; set; }

        [RequiredIf(nameof(DispatchType), "admin")]
        [StringLength(20)]
        [Display(Name = "Dispatching Employee")]
        public string? DispatchPayrollNo { get; set; }

        [RequiredIf(nameof(DispatchType), "vendor")]
        [StringLength(50)]
        [Display(Name = "Dispatching Vendor")]
        public string? DispatchVendor { get; set; }

        [StringLength(100)]
        public string? CollectorName { get; set; }

        [StringLength(50)]
        public string? CollectorId { get; set; }

        public RequisitionStatus? Status { get; set; }

        // Legacy fields (for migration, to be removed after migration is complete)
        [NotMapped]
        public string IssueStationCategory { get; set; }

        [NotMapped]
        public string IssueStation { get; set; }

        [NotMapped]
        public string DeliveryStationCategory { get; set; }

        [NotMapped]
        public string DeliveryStation { get; set; }
    }
}
```

## Approval Model Changes

Update the Approval model to include station reference and who approved:

```csharp
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MRIV.Enums;

namespace MRIV.Models
{
    public class Approval
    {
        public int Id { get; set; }

        [Required]
        public int RequisitionId { get; set; }
        
        [ForeignKey("RequisitionId")]
        public Requisition Requisition { get; set; }

        [Required]
        public int DepartmentId { get; set; }
        
        // Add station reference
        public int? StationId { get; set; }
        
        [ForeignKey("StationId")]
        public Station Station { get; set; }

        // Visibility scope for this approval
        [Required]
        public VisibilityScope VisibilityScope { get; set; } = VisibilityScope.Department;

        [Required]
        public int Step { get; set; }
        
        [ForeignKey("Step")]
        public WorkflowStepConfig StepConfig { get; set; }

        [Required]
        public ApprovalStatus ApprovalStatus { get; set; }

        // Who approved this step
        [StringLength(20)]
        public string ApprovedBy { get; set; }
        
        public DateTime? ApprovedAt { get; set; }

        [StringLength(500)]
        public string Comments { get; set; }

        public DateTime CreatedAt { get; set; }
        
        public DateTime? UpdatedAt { get; set; }
    }
}
```

## Database Migration Strategy

### Step 1: Create New Tables with Updated Schema

```sql
-- Add new columns to Requisition table
ALTER TABLE Requisitions ADD
    IssueLocationType INT NULL,
    IssueDepartmentId INT NULL,
    IssueStationId INT NULL,
    IssueVendorId INT NULL,
    DeliveryLocationType INT NULL,
    DeliveryDepartmentId INT NULL,
    DeliveryStationId INT NULL,
    DeliveryVendorId INT NULL,
    VisibilityScope INT NULL;

-- Add foreign key constraints
ALTER TABLE Requisitions ADD CONSTRAINT FK_Requisitions_IssueDepartment
    FOREIGN KEY (IssueDepartmentId) REFERENCES Departments(Id);
    
ALTER TABLE Requisitions ADD CONSTRAINT FK_Requisitions_IssueStation
    FOREIGN KEY (IssueStationId) REFERENCES Stations(Id);
    
ALTER TABLE Requisitions ADD CONSTRAINT FK_Requisitions_IssueVendor
    FOREIGN KEY (IssueVendorId) REFERENCES Vendors(Id);
    
ALTER TABLE Requisitions ADD CONSTRAINT FK_Requisitions_DeliveryDepartment
    FOREIGN KEY (DeliveryDepartmentId) REFERENCES Departments(Id);
    
ALTER TABLE Requisitions ADD CONSTRAINT FK_Requisitions_DeliveryStation
    FOREIGN KEY (DeliveryStationId) REFERENCES Stations(Id);
    
ALTER TABLE Requisitions ADD CONSTRAINT FK_Requisitions_DeliveryVendor
    FOREIGN KEY (DeliveryVendorId) REFERENCES Vendors(Id);

-- Add new columns to Approval table
ALTER TABLE Approvals ADD
    StationId INT NULL,
    VisibilityScope INT NULL,
    ApprovedBy NVARCHAR(20) NULL,
    ApprovedAt DATETIME NULL;

-- Add foreign key constraint
ALTER TABLE Approvals ADD CONSTRAINT FK_Approvals_Station
    FOREIGN KEY (StationId) REFERENCES Stations(Id);
```

### Step 2: Data Migration Script

Create a script to migrate data from string-based to ID-based references:

```csharp
// C# migration code to be run as a one-time job
public async Task MigrateLocationDataAsync()
{
    using var scope = _serviceProvider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<RequisitionContext>();
    var ktdaContext = scope.ServiceProvider.GetRequiredService<KtdaleaveContext>();
    
    // Load all departments and stations
    var departments = await ktdaContext.Departments.ToListAsync();
    var stations = await ktdaContext.Stations.ToListAsync();
    var vendors = await dbContext.Vendors.ToListAsync();
    
    // Migrate Requisition data
    var requisitions = await dbContext.Requisitions.ToListAsync();
    foreach (var requisition in requisitions)
    {
        // Determine issue location type and ID
        if (!string.IsNullOrEmpty(requisition.IssueStationCategory))
        {
            switch (requisition.IssueStationCategory.ToLower())
            {
                case "headoffice":
                case "department":
                    requisition.IssueLocationType = LocationType.Department;
                    var issueDept = departments.FirstOrDefault(d => 
                        d.DepartmentName.Equals(requisition.IssueStation, StringComparison.OrdinalIgnoreCase));
                    if (issueDept != null)
                        requisition.IssueDepartmentId = issueDept.Id;
                    break;
                    
                case "factory":
                case "region":
                    requisition.IssueLocationType = LocationType.Station;
                    var issueStation = stations.FirstOrDefault(s => 
                        s.StationName.Equals(requisition.IssueStation, StringComparison.OrdinalIgnoreCase));
                    if (issueStation != null)
                        requisition.IssueStationId = issueStation.Id;
                    break;
                    
                case "vendor":
                    requisition.IssueLocationType = LocationType.Vendor;
                    var issueVendor = vendors.FirstOrDefault(v => 
                        v.Name.Equals(requisition.IssueStation, StringComparison.OrdinalIgnoreCase));
                    if (issueVendor != null)
                        requisition.IssueVendorId = issueVendor.Id;
                    break;
            }
        }
        
        // Determine delivery location type and ID
        if (!string.IsNullOrEmpty(requisition.DeliveryStationCategory))
        {
            switch (requisition.DeliveryStationCategory.ToLower())
            {
                case "headoffice":
                case "department":
                    requisition.DeliveryLocationType = LocationType.Department;
                    var deliveryDept = departments.FirstOrDefault(d => 
                        d.DepartmentName.Equals(requisition.DeliveryStation, StringComparison.OrdinalIgnoreCase));
                    if (deliveryDept != null)
                        requisition.DeliveryDepartmentId = deliveryDept.Id;
                    break;
                    
                case "factory":
                case "region":
                    requisition.DeliveryLocationType = LocationType.Station;
                    var deliveryStation = stations.FirstOrDefault(s => 
                        s.StationName.Equals(requisition.DeliveryStation, StringComparison.OrdinalIgnoreCase));
                    if (deliveryStation != null)
                        requisition.DeliveryStationId = deliveryStation.Id;
                    break;
                    
                case "vendor":
                    requisition.DeliveryLocationType = LocationType.Vendor;
                    var deliveryVendor = vendors.FirstOrDefault(v => 
                        v.Name.Equals(requisition.DeliveryStation, StringComparison.OrdinalIgnoreCase));
                    if (deliveryVendor != null)
                        requisition.DeliveryVendorId = deliveryVendor.Id;
                    break;
            }
        }
        
        // Set default visibility scope
        requisition.VisibilityScope = VisibilityScope.Department;
    }
    
    // Update all requisitions
    await dbContext.SaveChangesAsync();
    
    // Migrate Approval data
    var approvals = await dbContext.Approvals
        .Include(a => a.Requisition)
        .ToListAsync();
        
    foreach (var approval in approvals)
    {
        // Set StationId based on Requisition's station if available
        if (approval.Requisition != null)
        {
            if (approval.Requisition.IssueLocationType == LocationType.Station && 
                approval.Requisition.IssueStationId.HasValue)
            {
                approval.StationId = approval.Requisition.IssueStationId.Value;
            }
            else if (approval.Requisition.DeliveryLocationType == LocationType.Station && 
                     approval.Requisition.DeliveryStationId.HasValue)
            {
                approval.StationId = approval.Requisition.DeliveryStationId.Value;
            }
        }
        
        // Set default visibility scope
        approval.VisibilityScope = VisibilityScope.Department;
    }
    
    // Update all approvals
    await dbContext.SaveChangesAsync();
}
```

### Step 3: Make New Fields Required and Remove Old Fields

After successful migration and testing:

```sql
-- Make new fields required
ALTER TABLE Requisitions ALTER COLUMN IssueLocationType INT NOT NULL;
ALTER TABLE Requisitions ALTER COLUMN VisibilityScope INT NOT NULL;
ALTER TABLE Requisitions ALTER COLUMN DeliveryLocationType INT NOT NULL;

-- Make approval fields required
ALTER TABLE Approvals ALTER COLUMN VisibilityScope INT NOT NULL;

-- Drop old columns
ALTER TABLE Requisitions DROP COLUMN IssueStationCategory;
ALTER TABLE Requisitions DROP COLUMN IssueStation;
ALTER TABLE Requisitions DROP COLUMN DeliveryStationCategory;
ALTER TABLE Requisitions DROP COLUMN DeliveryStation;
```
