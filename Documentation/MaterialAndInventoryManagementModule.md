# Material & Inventory Management Module

## Overview

The Material & Inventory Management Module is responsible for the lifecycle of materials within the MRIV system. It covers material creation, categorization, assignment, condition tracking, inventory status, and integration with requisition and approval workflows. This module ensures that all materials are properly tracked, assigned, and maintained throughout their lifecycle.

---

## Core Models and Their Relationships

### 1. **Material**
Represents a physical or virtual item in inventory.

- **Key Properties:**
  - `Id`: Unique identifier
  - `MaterialCategoryId`, `MaterialSubcategoryId`: Category and subcategory references
  - `Code`, `Name`, `Description`: Identification and description
  - `VendorId`: Supplier reference
  - `PurchaseDate`, `PurchasePrice`, `Warranty*`: Acquisition and warranty info
  - `Status`: Current inventory status (see `MaterialStatus` enum)
  - `CreatedAt`, `UpdatedAt`: Timestamps
  - `Media`: Associated media files (images, documents)
- **Navigation Properties:**
  - `MaterialCategory`, `MaterialSubcategory`: Category objects
  - `RequisitionItems`: Items in requisitions referencing this material
  - `MaterialAssignments`: Assignment history
  - `MaterialConditions`: Condition history

### 2. **MaterialCategory & MaterialSubcategory**
Organize materials for easier management and reporting.

- **MaterialCategory**
  - `Id`, `Name`, `Description`, `UnitOfMeasure`
  - `Subcategories`: List of subcategories
  - `Materials`: List of materials in this category
  - `Media`: Category images
- **MaterialSubcategory**
  - `Id`, `MaterialCategoryId`, `Name`, `Description`
  - `MaterialCategory`: Parent category
  - `Materials`: List of materials in this subcategory
  - `Media`: Subcategory images

### 3. **MaterialAssignment**
Tracks the assignment of a material to a user, station, or department.

- **Key Properties:**
  - `Id`, `MaterialId`, `PayrollNo` (assigned to), `AssignmentDate`, `ReturnDate`
  - `StationCategory`, `StationId`, `DepartmentId`, `SpecificLocation`
  - `AssignmentType`: (New, Transfer, Maintenance, etc.)
  - `RequisitionId`: Link to the requisition that triggered the assignment
  - `AssignedByPayrollNo`, `Notes`, `IsActive`
- **Navigation Properties:**
  - `Material`: The assigned material
  - `Requisition`: The related requisition
  - `MaterialConditions`: Condition records for this assignment

### 4. **MaterialCondition**
Records the condition of a material at various lifecycle stages.

- **Key Properties:**
  - `Id`, `MaterialId`, `MaterialAssignmentId`, `RequisitionId`, `RequisitionItemId`, `ApprovalId`
  - `ConditionCheckType`: (Initial, Assignment, Periodic, Damage, Disposal)
  - `Stage`, `Condition`, `FunctionalStatus`, `CosmeticStatus`, `ComponentStatuses`
  - `Notes`, `InspectedBy`, `InspectionDate`, `ActionRequired`, `ActionDueDate`
- **Navigation Properties:**
  - `Material`, `MaterialAssignment`, `Requisition`, `RequisitionItem`, `Approval`
  - `Media`: Associated media files (e.g., inspection photos)

### 5. **RequisitionItem**
Represents a material (or to-be-created material) requested in a requisition.

- **Key Properties:**
  - `Id`, `RequisitionId`, `MaterialId`, `Quantity`, `Name`, `Description`
  - `Condition`, `Status`, `SaveToInventory`, `CreatedAt`, `UpdatedAt`
- **Navigation Properties:**
  - `Requisition`: Parent requisition
  - `Material`: Linked material (if any)

### 6. **MediaFile**
Stores metadata about files (images, documents) attached to materials, categories, assignments, or conditions.

- **Key Properties:**
  - `Id`, `FileName`, `MimeType`, `FilePath`, `Collection`, `ModelType`, `ModelId`, `FileSize`, `Alt`, `CustomProperties`, `CreatedAt`, `UpdatedAt`

---

## Key Enums

- **MaterialStatus**: UnderMaintenance, LostOrStolen, Disposed, Available, Assigned, InProcess
- **AssignmentType**: NewPurchase, Transfer, InterFactory, Maintenance, Return, Disposal, Loan, TemporaryAllocation, Other
- **Condition, ConditionCheckType, FunctionalStatus, CosmeticStatus**: Used in MaterialCondition for detailed state tracking

---

## Logic Flows

### 1. **Material Creation & Categorization**
- Materials are created via the MaterialsController or as part of a requisition.
- Each material is assigned a category and (optionally) a subcategory.
- Media (images, documents) can be attached to materials and categories.

### 2. **Material Assignment**
- When a material is issued, transferred, or returned, a MaterialAssignment record is created or updated.
- Assignment includes who (payroll), where (station/department), when (dates), and context (assignment type).
- Only one assignment is active at a time per material (`IsActive`).

### 3. **Condition Tracking**
- At each assignment, return, or periodic check, a MaterialCondition record is created.
- Tracks physical and functional state, inspection details, and any required actions.
- Linked to both the material and the specific assignment/requisition.

### 4. **Inventory Status Management**
- Material status is updated based on assignment, return, maintenance, or disposal.
- Statuses include: Available, Assigned, In Process, Under Maintenance, etc.
- Status changes are reflected in the UI and used for filtering/searching.

### 5. **Integration with Requisition Workflow**
- Materials can be requested via the Material Requisition Wizard.
- New materials can be created as part of a requisition (with code generation and validation).
- RequisitionItems link requested materials to the requisition and drive assignment/condition creation upon approval.

### 6. **Media Management**
- MediaFile records are attached to materials, categories, assignments, and conditions for documentation and audit.
- Media is accessible via URLs and can be displayed in the UI.

---

## View Models & UI Integration

- **CreateMaterialViewModel**: Used for material creation, includes dropdowns for categories, subcategories, locations, and media upload.
- **MaterialsIndexViewModel**: Used for listing materials, includes filters, pagination, and lookup dictionaries for related data.
- **MaterialDisplayViewModel**: Used for detailed material views, including assignment and condition history.
- **MaterialRequisitionWizardViewModel**: Used in the wizard, connects requisition, items, materials, and assignments.

---

## How Everything Connects

- **Material** is the core entity, linked to categories, assignments, conditions, and requisition items.
- **MaterialAssignment** records every issuance, transfer, or return, and is linked to both the material and the triggering requisition.
- **MaterialCondition** provides a detailed audit trail of the material's state at every key event.
- **RequisitionItem** connects the requisition process to inventory, allowing new or existing materials to be requested and tracked.
- **MediaFile** enables rich documentation and traceability for all entities in the module.

---

## Example Flow: Material Issuance

1. **User requests a material** via the requisition wizard.
2. **RequisitionItem** is created, referencing an existing or new Material.
3. Upon approval, a **MaterialAssignment** is created, marking the material as assigned to a user/location.
4. A **MaterialCondition** record is created, documenting the state at issuance.
5. **MaterialStatus** is updated to reflect the new state (e.g., Assigned).
6. All actions are auditable, and media can be attached at each step.

---

## Summary

The Material & Inventory Management Module provides a robust, auditable, and extensible framework for tracking materials throughout their lifecycle. It integrates tightly with requisition and approval workflows, supports detailed categorization and condition tracking, and enables rich documentation through media attachments. This ensures accountability, transparency, and efficiency in material management across the organization. 