# MRIV Material Management Testing Documentation

## Overview

This document provides comprehensive testing procedures for the Material Requisition and Inventory Verification (MRIV) system, focusing on material movement, status tracking, visibility controls, and assignment management. The testing scenarios cover the core business processes from material creation through disposal, with emphasis on workflow validation, authorization, and audit trails.

## Table of Contents
1. [System Architecture Overview](#system-architecture-overview)
2. [Material Model Relationships](#material-model-relationships)
3. [Status Transition Testing](#status-transition-testing)
4. [Visibility and Authorization Testing](#visibility-and-authorization-testing)
5. [Assignment and Responsibility Testing](#assignment-and-responsibility-testing)
6. [Workflow Testing Scenarios](#workflow-testing-scenarios)
7. [Integration Testing](#integration-testing)
8. [Data Validation Testing](#data-validation-testing)

---

## System Architecture Overview

### Database Contexts Integration
The MRIV system uses two main database contexts:

1. **RequisitionContext** - Primary system data:
   - Materials, MaterialAssignments, MaterialConditions
   - Requisitions, RequisitionItems, Approvals
   - WorkflowConfigs, Notifications, Settings

2. **KtdaleaveContext** - Legacy HR/Employee data:
   - Departments, EmployeeBkps (Employees), Stations, Roles

### Key Model Relationships
```
Material (1) ←→ (N) MaterialAssignment ←→ (N) MaterialCondition
    ↓                      ↓                        ↓
RequisitionItem ←→ Requisition ←→ Approval ←→ WorkflowStepConfig
    ↓                      ↓                        ↓
Department/Station    Employee/Role         NotificationTemplate
```

---

## Material Model Relationships

### Core Entity Relationships

#### Material Entity
- **Primary Properties**: Id, Code, Name, Description, Status, Category/Subcategory
- **Assignment Tracking**: MaterialAssignments (history of who has/had the material)
- **Condition Tracking**: MaterialConditions (condition at each lifecycle stage)
- **Requisition Integration**: RequisitionItems (requests involving this material)
- **Media Support**: Implements IHasMedia for attachments

#### MaterialAssignment Entity
- **Assignment Details**: PayrollNo, StationCategory, StationId, DepartmentId
- **Assignment Context**: AssignmentType, RequisitionId, AssignedByPayrollNo
- **Timeline**: AssignmentDate, ReturnDate, IsActive
- **Location Details**: SpecificLocation, Notes

#### MaterialCondition Entity
- **Inspection Details**: ConditionCheckType, Stage, Condition, FunctionalStatus, CosmeticStatus
- **Context Links**: MaterialId, MaterialAssignmentId, RequisitionId, ApprovalId
- **Audit Trail**: InspectedBy, InspectionDate, ActionRequired, ActionDueDate

---

## Status Transition Testing

### Material Status Flow
```
Available → Assigned → InProcess → [UnderMaintenance|Available|Disposed]
    ↑_____________←←← Return ←←←_____________↓
```

#### MaterialStatus Enum Values:
1. **Available** (4) - Material ready for assignment
2. **Assigned** (5) - Currently assigned to user/location  
3. **InProcess** (6) - Being transferred/processed
4. **UnderMaintenance** (1) - Under repair/maintenance
5. **LostOrStolen** (2) - Lost or stolen material
6. **Disposed** (3) - Permanently disposed

### Test Scenarios: Status Transitions

#### Test Case 1: New Material Creation
**Preconditions:**
- User has material creation permissions
- Valid category and subcategory selected

**Test Steps:**
1. Navigate to Materials → Create New
2. Fill in required fields (Name, Description, Category)
3. Set initial status to "Available"
4. Save material

**Expected Results:**
- Material created with Status = Available (4)
- CreatedAt timestamp populated
- Unique ID assigned
- Initial MaterialCondition record created with ConditionCheckType = Initial

**Validation Points:**
- Database: Verify Material.Status = 4
- Database: Verify MaterialCondition record exists with ConditionCheckType = 0
- UI: Material appears in "Available Materials" list

#### Test Case 2: Material Assignment (Transfer)
**Preconditions:**
- Material exists with Status = Available
- Target user/location exists
- User has transfer permissions

**Test Steps:**
1. Create Transfer requisition
2. Select available material
3. Specify target user (PayrollNo) and location
4. Complete approval workflow
5. Process assignment

**Expected Results:**
- Material.Status changes from Available (4) to Assigned (5)
- New MaterialAssignment record created with IsActive = true
- Previous MaterialAssignment (if any) set to IsActive = false
- MaterialCondition record created with ConditionCheckType = Transfer (1)
- RequisitionStatus changes to Completed (5)

**Validation Points:**
- Database: Material.Status = 5
- Database: MaterialAssignment.IsActive = true for new assignment
- Database: MaterialCondition.ConditionCheckType = 1
- Database: MaterialAssignment.PayrollNo = target user
- UI: Material appears in target user's "My Materials" list
- UI: Material removed from "Available Materials" list

#### Test Case 3: Material Return
**Preconditions:**
- Material has active assignment (Status = Assigned)
- User has return permissions

**Test Steps:**
1. Create Return requisition
2. Select assigned material
3. Complete approval workflow
4. Process return

**Expected Results:**
- Material.Status changes from Assigned (5) to Available (4)
- Current MaterialAssignment.IsActive set to false
- MaterialAssignment.ReturnDate populated
- MaterialCondition record created with ConditionCheckType = AtReceipt (3)

**Validation Points:**
- Database: Material.Status = 4
- Database: MaterialAssignment.IsActive = false
- Database: MaterialAssignment.ReturnDate IS NOT NULL
- UI: Material appears in "Available Materials" list again

### RequisitionStatus Flow
```
NotStarted → PendingDispatch → PendingReceipt → Completed
     ↓________________→ Cancelled ←________________↑
```

#### Test Case 4: Requisition Status Progression
**Test Steps:**
1. Create new requisition (Status = NotStarted)
2. Submit for approval (Status = PendingDispatch)
3. Approve and dispatch (Status = PendingReceipt)  
4. Confirm receipt (Status = Completed)

**Validation Points:**
- Each status transition updates RequisitionStatus correctly
- Associated MaterialAssignment/Condition records created at appropriate stages
- Notifications sent to relevant parties at each stage

---

## Visibility and Authorization Testing

### VisibilityAuthorizeService Testing

The system implements location-based authorization through the VisibilityAuthorizeService, which controls what data users can see based on their department, station, and role assignments.

#### Authorization Hierarchy
```
Global Roles (Admin/System) - Can see all data
    ↓
Station-Level Roles - Can see all departments in their station
    ↓
Department-Level Roles - Can see only their department data
    ↓
User-Level - Can see only their own assignments
```

### Test Scenarios: Visibility Controls

#### Test Case 5: Department-Level Visibility
**Setup:**
- User1: Department A, Station 001
- User2: Department B, Station 001  
- User3: Department A, Station 002
- Material M1: Assigned to User1

**Test Steps:**
1. Login as User1 → Should see Material M1
2. Login as User2 → Should NOT see Material M1 (different department)
3. Login as User3 → Should NOT see Material M1 (different station)
4. Login as Station Manager for Station 001 → Should see Material M1

**Expected Results:**
- VisibilityAuthorizeService.ApplyVisibilityScopeAsync filters data correctly
- Material lists show only authorized materials per user
- Department/Station filters work correctly

#### Test Case 6: Role-Based Assignment Visibility
**Setup:**
- Requisition R1: Created by Department A
- Approval Step 1: Department Head approval (Department A only)
- Approval Step 2: Station Manager approval (Station-wide)

**Test Steps:**
1. Department A Head login → Should see R1 for approval
2. Department B Head login → Should NOT see R1
3. Station Manager login → Should see R1 after Dept Head approval
4. System Admin login → Should see all requisitions

**Validation Points:**
- Approval queues show only relevant requisitions per user role
- WorkflowStepConfig.RoleParameters correctly filter approvals
- UserHasRoleForStep method returns correct permissions

### Station Category Integration

#### Test Case 7: Station Category Filtering
**Setup:**
- Station Categories: headoffice, factory, region, vendor
- User roles mapped to specific station categories

**Test Steps:**
1. Create requisition with IssueStationCategory = "factory"
2. Verify only factory users can see issue-related actions
3. Create requisition with DeliveryStationCategory = "vendor"  
4. Verify vendors can see delivery-related actions

**Expected Results:**
- StationCategory filtering works in requisition creation
- LocationService provides correct station options based on category
- Visibility controls respect station category boundaries

---

## Assignment and Responsibility Testing

### MaterialAssignment Lifecycle

#### Test Case 8: Assignment Creation and Tracking
**Preconditions:**
- Material M1 exists (Status = Available)
- Employee E1 exists with valid PayrollNo
- Department D1 and Station S1 exist

**Test Steps:**
1. Create Transfer requisition for Material M1 to Employee E1
2. Complete approval workflow
3. Verify assignment creation
4. Check assignment details

**Expected Results:**
- MaterialAssignment record created with:
  - MaterialId = M1.Id
  - PayrollNo = E1.PayrollNo  
  - AssignmentType = Transfer (2)
  - AssignmentDate = Current timestamp
  - IsActive = true
  - StationId/DepartmentId = E1's location

**Validation Points:**
- Database: MaterialAssignment record exists and is correct
- Database: Only one active assignment per material
- UI: Assignment shows in employee's material list
- UI: Assignment shows in material's assignment history

#### Test Case 9: Assignment Transfer Chain
**Test Steps:**
1. Material M1 assigned to Employee E1
2. Create transfer from E1 to E2
3. Complete approval and assignment
4. Verify assignment chain

**Expected Results:**
- Original assignment (E1) set to IsActive = false, ReturnDate populated
- New assignment (E2) created with IsActive = true
- MaterialCondition records created for both transfer-out and transfer-in
- Assignment history maintains complete audit trail

#### Test Case 10: Multi-Assignment Scenarios
**Test Complex Assignment Types:**

**New Purchase Assignment:**
- AssignmentType = NewPurchase (1)
- Should link to procurement requisition
- Should create initial MaterialCondition record

**Maintenance Assignment:**
- AssignmentType = Maintenance (4)
- Material.Status should change to UnderMaintenance (1)
- Should allow temporary assignment to maintenance team

**Loan Assignment:**
- AssignmentType = Loan (7)
- Should have specified ReturnDate
- Should track loan period and send return reminders

**Disposal Assignment:**
- AssignmentType = Disposal (6)
- Material.Status should change to Disposed (3)
- Should require approval from compliance/finance

---

## Workflow Testing Scenarios

### Integration Testing: Complete Material Lifecycle

#### Test Case 11: End-to-End Material Journey
**Scenario**: New laptop procurement, assignment, transfer, maintenance, and disposal

**Phase 1: Procurement**
1. Employee creates New Purchase requisition for laptop
2. Department Head approves
3. Procurement team approves
4. Finance approves
5. Material record created upon approval

**Phase 2: Initial Assignment**
1. Laptop received and inspected (MaterialCondition: Initial)
2. Assigned to requesting employee (MaterialAssignment: NewPurchase)
3. Material.Status = Assigned

**Phase 3: Transfer**
1. Employee transfers laptop to colleague
2. Transfer requisition created and approved
3. Condition check at transfer (MaterialCondition: Transfer)
4. Assignment updated to new employee

**Phase 4: Maintenance**
1. Laptop requires repair
2. Maintenance requisition created
3. Temporary assignment to IT department
4. Material.Status = UnderMaintenance
5. Condition check documents repair needs

**Phase 5: Return from Maintenance**
1. Repair completed
2. Return requisition processed
3. Condition check confirms repair completion
4. Material.Status = Available
5. Ready for reassignment

**Phase 6: Disposal**
1. Laptop reaches end of life
2. Disposal requisition created
3. Compliance and finance approval required
4. Final condition check and disposal documentation
5. Material.Status = Disposed

**Validation Points Throughout:**
- Every status change properly recorded
- All assignments have corresponding condition checks
- Approval chains follow configured workflows
- Notifications sent to appropriate parties
- Complete audit trail maintained
- Media attachments (photos, documents) properly linked

### Performance and Concurrent Access Testing

#### Test Case 12: Concurrent Assignment Attempts
**Setup:** Multiple users attempting to request same material simultaneously

**Test Steps:**
1. Material M1 available
2. User1 and User2 simultaneously create requisitions for M1
3. Both complete at nearly same time

**Expected Results:**
- Only one requisition should succeed
- Other should fail with appropriate error message
- No orphaned assignments or inconsistent data
- Material availability correctly managed

#### Test Case 13: Bulk Material Operations
**Test Steps:**
1. Create bulk material import (100+ materials)
2. Process multiple simultaneous requisitions
3. Verify database consistency and performance

**Validation Points:**
- Database transactions maintain consistency
- Performance remains acceptable with large datasets
- Memory usage stays within acceptable limits

---

## Integration Testing

### Database Context Integration

#### Test Case 14: Cross-Context Data Access
**Scenario**: Requisition involving employee data from KtdaleaveContext and materials from RequisitionContext

**Test Steps:**
1. Create requisition with employee lookup
2. Verify employee data correctly retrieved from KtdaleaveContext
3. Verify material data correctly stored in RequisitionContext
4. Test join queries across contexts

**Expected Results:**
- Employee information (name, department) correctly displayed
- Department and station data properly integrated
- No data consistency issues between contexts

### External Service Integration

#### Test Case 15: Email Notification Testing
**Test Steps:**
1. Create requisition requiring approval
2. Verify approval notification sent to approver
3. Process approval and verify status change notification
4. Test notification templates and content

**Expected Results:**
- Emails sent to correct recipients
- Email content includes relevant requisition details
- Notification status tracked in database

---

## Data Validation Testing

### Business Rule Validation

#### Test Case 16: Material Code Uniqueness
**Test Steps:**
1. Create material with code "LAP001"
2. Attempt to create another material with same code
3. Verify validation error

#### Test Case 17: Assignment Constraint Validation
**Test Steps:**
1. Assign material to user
2. Attempt to assign same material to different user without returning first
3. Verify constraint violation handled correctly

#### Test Case 18: Requisition Type Validation
**InterFactory Requisition Validation:**
1. Create InterFactory requisition
2. Set IssueStationCategory = "headoffice"
3. Verify validation error (both must be "factory")
4. Correct to DeliveryStationCategory = "factory"
5. Verify validation passes

### Edge Cases and Error Handling

#### Test Case 19: Invalid Data Scenarios
- Null/empty required fields
- Invalid foreign key references
- Malformed JSON in condition data
- File upload errors for media attachments

#### Test Case 20: System Boundary Testing
- Maximum string length validation
- Date range validation (warranty dates, assignment dates)
- Decimal precision for purchase prices
- Large file upload handling

---

## Test Data Setup

### Required Test Data
```sql
-- Departments (KtdaleaveContext)
INSERT INTO Department (DepartmentCode, DepartmentID, DepartmentName)
VALUES (1, 'IT', 'Information Technology'),
       (2, 'HR', 'Human Resources'),
       (3, 'FIN', 'Finance');

-- Employees (KtdaleaveContext)  
INSERT INTO EmployeeBkp (PayrollNo, Surname, Othernames, DepartmentCode, StationCode)
VALUES ('EMP001', 'Smith', 'John', 1, '001'),
       ('EMP002', 'Jones', 'Mary', 2, '001'),
       ('EMP003', 'Brown', 'David', 1, '002');

-- Material Categories (RequisitionContext)
INSERT INTO MaterialCategory (Name, Description, UnitOfMeasure)
VALUES ('Electronics', 'Electronic equipment and devices', 'Each'),
       ('Furniture', 'Office furniture and fixtures', 'Each'),
       ('Stationery', 'Office stationery and supplies', 'Box');

-- Materials (RequisitionContext)
INSERT INTO Material (MaterialCategoryId, Code, Name, Description, Status)
VALUES (1, 'LAP001', 'Dell Laptop', 'Dell Latitude 7420 Business Laptop', 4),
       (1, 'MON001', 'Dell Monitor', 'Dell 24" LED Monitor', 4),
       (2, 'CHR001', 'Office Chair', 'Ergonomic Office Chair', 4);
```

---

## Reporting and Metrics

### Test Execution Tracking
- Track test execution results
- Identify patterns in failures
- Monitor performance metrics
- Validate audit trail completeness

### Key Performance Indicators
- Material assignment success rate
- Average requisition processing time
- Approval workflow efficiency
- Data consistency validation results

---

## Conclusion

This testing documentation provides comprehensive coverage of the MRIV system's material management processes. Focus areas include:

1. **Material Lifecycle Management** - From creation to disposal
2. **Status Transition Integrity** - Ensuring proper state changes
3. **Visibility and Authorization** - Role-based data access control
4. **Assignment Tracking** - Complete audit trail of material custody
5. **Workflow Integration** - Seamless requisition and approval processes
6. **Data Consistency** - Cross-context integration validation

Each test case should be executed with careful attention to both positive and negative scenarios, ensuring the system handles edge cases gracefully while maintaining data integrity and business rule compliance.

The testing approach emphasizes traceability, accountability, and transparency - core requirements for an enterprise material management system in a regulated environment like KTDA.