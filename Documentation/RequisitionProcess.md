# Requisition Process Blueprint

## Overview

The requisition process in the MRIV system is a multi-step, wizard-driven workflow that allows users to request, track, and manage materials. The process is tightly integrated with the Material & Inventory Management and Approval Workflow modules, ensuring that all material movements are authorized, auditable, and compliant with business rules.

---

## Wizard Flow: Step-by-Step

1. **Ticket Selection**
   - User selects or searches for a support/request ticket to link with the requisition (optional for some types).
   - The selected ticket is stored in session and pre-fills some requisition details.

2. **Requisition Details**
   - User enters logistics: department, station, requisition type, dispatch type, etc.
   - Validations are performed based on requisition type (see below).
   - User location and context are initialized.

3. **Requisition Items**
   - User adds one or more items to the requisition.
   - Each item can reference an existing material or specify a new material to be created.
   - Item-level details: quantity, condition, save-to-inventory flag, etc.
   - Code generation and uniqueness checks for new materials.

4. **Approvers & Receivers**
   - Approval steps are generated dynamically based on requisition details and workflow configuration.
   - User can select specific employees for each approval step (where allowed).
   - Department/role-based selection for special steps (e.g., vendor dispatch).

5. **Summary & Completion**
   - User reviews all entered data, items, and approval chain.
   - On submission, all data is saved in a transaction, and notifications are sent.

---

## Requisition Types & Their Logic

### 1. **New Purchase**
- **Purpose:** Request procurement of new materials not currently in inventory.
- **Flow:**
  - User specifies details for new materials (category, description, etc.).
  - New Material records are created upon approval.
  - Approval steps typically include department head, procurement, and finance.
- **Validations:**
  - Required fields: category, description, quantity, justification.
  - Code uniqueness for new materials.
- **Events:**
  - Material is added to inventory upon approval.
  - Notifications to procurement and requester.

### 2. **Transfer**
- **Purpose:** Move existing materials between departments, stations, or users.
- **Flow:**
  - User selects materials from inventory.
  - MaterialAssignment is updated to reflect new owner/location.
  - Approval steps may include both source and destination supervisors.
- **Validations:**
  - Material must be available and not already assigned.
  - Source and destination must be different.
- **Events:**
  - Assignment and condition records updated.
  - Notifications to both parties.

### 3. **InterFactory**
- **Purpose:** Borrow or transfer materials between factories.
- **Flow:**
  - Both issue and delivery station categories must be set to 'Factory'.
  - Approval chain may include both factory managers.
- **Validations:**
  - Enforced by custom validator: both station categories must be 'Factory'.
- **Events:**
  - MaterialAssignment reflects inter-factory context.
  - Notifications to both factories.

### 4. **Maintenance/Repair**
- **Purpose:** Request maintenance or repair for a material.
- **Flow:**
  - User selects material needing maintenance.
  - Material status set to 'Under Maintenance'.
  - Assignment may be temporarily transferred to maintenance team/vendor.
- **Validations:**
  - Material must not already be under maintenance.
- **Events:**
  - Condition record created for maintenance event.
  - Notifications to maintenance team and requester.

### 5. **Return**
- **Purpose:** Return a material to inventory or previous owner.
- **Flow:**
  - User selects material to return.
  - Assignment is closed, material status set to 'Available'.
- **Validations:**
  - Material must be currently assigned.
- **Events:**
  - Assignment and condition records updated.
  - Notification to inventory manager.

### 6. **Disposal**
- **Purpose:** Retire or dispose of obsolete/damaged materials.
- **Flow:**
  - User selects material for disposal.
  - Approval chain includes compliance/finance.
  - Material status set to 'Disposed' upon approval.
- **Validations:**
  - Justification required.
  - Compliance checks (e.g., not under active assignment).
- **Events:**
  - Material removed from active inventory.
  - Audit trail maintained.

### 7. **Loan/Borrow**
- **Purpose:** Temporarily allocate materials to another user or department.
- **Flow:**
  - User specifies loan period and recipient.
  - Assignment is time-bound.
- **Validations:**
  - Material must be available.
  - Loan period must be specified.
- **Events:**
  - Assignment and condition records updated.
  - Notification to both parties.

### 8. **Temporary Allocation**
- **Purpose:** Short-term assignment for events, projects, etc.
- **Flow:**
  - Similar to loan/borrow, but may have different approval chain.
- **Validations:**
  - Material must be available.
- **Events:**
  - Assignment and condition records updated.

### 9. **Other**
- **Purpose:** Catch-all for requisitions not covered above.
- **Flow:**
  - User provides custom details and justification.
  - Approval chain determined by admin or workflow config.
- **Validations:**
  - Justification required.
- **Events:**
  - As determined by admin.

---

## Relationships with Other Modules

### **Material Module**
- **Creation:** New materials are created for New Purchase requisitions.
- **Assignment:** MaterialAssignment records are created/updated for Transfer, Loan, Maintenance, etc.
- **Condition:** MaterialCondition records are created at each key event (issuance, return, maintenance).
- **Status:** Material status is updated based on requisition type and approval outcome.

### **Approval Workflow Module**
- **Approval Steps:** Generated dynamically based on requisition type, station/department, and workflow config.
- **Assignment:** Each step is assigned to a specific employee or role.
- **State Transitions:** Approval status (Pending, Approved, Rejected) drives requisition and material state.
- **Special Cases:** Vendor dispatch, inter-factory, and compliance steps are handled via workflow config.

---

## Validations & Business Rules

- **Required Fields:** Enforced at each step (e.g., category, quantity, justification).
- **Custom Validators:** E.g., InterFactory requires both station categories to be 'Factory'.
- **Material Availability:** Checked for Transfer, Loan, Return, etc.
- **Code Uniqueness:** For new materials.
- **Assignment Integrity:** Only one active assignment per material.
- **Approval Chain Integrity:** All required steps must be completed in order.

---

## Events & Notifications

- **On Submission:**
  - Requisition, items, assignments, and approvals are saved in a transaction.
  - Notifications sent to creator and first approver.
- **On Approval/Dispatch/Receipt:**
  - Material status and assignments updated.
  - Condition records created.
  - Notifications sent to next approver or recipient.
- **On Rejection/Return/Disposal:**
  - Material and requisition status updated.
  - Audit trail maintained.
  - Notifications sent to relevant parties.

---

## State Transitions

- **RequisitionStatus:** NotStarted → PendingDispatch → PendingReceipt → Completed/Cancelled
- **MaterialStatus:** Available ↔ Assigned ↔ InProcess ↔ UnderMaintenance ↔ Disposed
- **ApprovalStatus:** NotStarted → PendingApproval → Approved/Rejected

---

## Integration Points

- **Ticketing System:** For initial request context.
- **Vendor Management:** For dispatch and procurement steps.
- **Employee/Department/Station Services:** For assignment and approval routing.
- **Notification Service:** For all key events.

---

## Special Cases & Branching Logic

- **InterFactory Borrowing:** Enforced by custom validator; approval chain includes both factories.
- **Vendor Dispatch:** Approval step assigned to vendor or ICT dispatch.
- **Disposal:** Requires compliance/finance approval and audit trail.
- **Custom Workflows:** WorkflowConfig and WorkflowStepConfig allow for dynamic, type-specific approval chains.

---

## Summary

This blueprint provides a comprehensive, auditable, and extensible requisition process. It ensures that all material movements are authorized, validated, and tracked, with clear integration to inventory and approval modules. The process is adaptable to various requisition types and organizational needs, supporting both standard and custom workflows. 