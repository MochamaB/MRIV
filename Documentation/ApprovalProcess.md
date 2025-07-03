# Approval Process Blueprint

## Overview

The approval process in the MRIV system is a multi-step, workflow-driven mechanism that governs the authorization and tracking of material requisitions. Each approval step is tightly integrated with the Material & Inventory Management module and the Notification module, ensuring that all material movements are validated, auditable, and communicated to relevant stakeholders.

---

## Core Concepts

- **Approval Step:** Represents a single stage in the approval workflow for a requisition. Each step is assigned to a specific user, department, or vendor, and has a defined action (e.g., Approve, Dispatch, Receive).
- **ApprovalStatus:** Enum representing the state of each approval step (NotStarted, PendingApproval, Approved, Rejected, Dispatched, Received, OnHold, PendingDispatch, PendingReceive, Cancelled).
- **WorkflowConfig/StepConfig:** Define the sequence and logic of approval steps for different requisition types and contexts.

---

## Approval Step Lifecycle

1. **Creation**
   - Approval steps are generated dynamically when a requisition is created, based on workflow configuration and requisition details.
   - Each step is assigned a step number, responsible party (employee, department, or vendor), and initial status (first step: PendingApproval, others: NotStarted).

2. **Activation**
   - When a step becomes active (PendingApproval), the assigned user is notified.
   - The user can take an action: Approve, Reject, Dispatch, Receive, or Put On Hold, depending on the step type.

3. **Action Handling**
   - **Approve:**
     - The step status is set to Approved.
     - If there is a next step, it is activated (status set to PendingApproval or PendingDispatch/Receive as appropriate).
     - If this is the last step, the requisition is marked as Completed, and material locations/assignments are updated.
   - **Reject:**
     - The step status is set to Rejected.
     - The requisition is marked as Cancelled.
     - All stakeholders are notified.
   - **Dispatch/Receive:**
     - The step status is set to Dispatched/Received.
     - MaterialAssignment and MaterialCondition records are updated to reflect the new state/location/condition.
     - The next step is activated, or the requisition is completed if this was the last step.
   - **On Hold:**
     - The step status is set to OnHold.
     - Comments are required, and the process is paused until further action.

4. **Completion**
   - When all steps are approved/dispatched/received as required, the requisition is marked as Completed.
   - Material assignments and conditions are finalized.
   - Final notifications are sent.

---

## Material Module Integration

### **MaterialAssignment**
- At dispatch/assignment steps, a MaterialAssignment record is created or updated to reflect the new owner, location, and assignment type.
- Only one active assignment per material is allowed.
- Assignment includes context: who, where, when, and why (requisition type).

### **MaterialCondition**
- At each key event (dispatch, receipt, return, maintenance), a MaterialCondition record is created.
- Tracks the physical and functional state of the material, inspection details, and any required actions.
- Linked to the approval step, assignment, and requisition item for full traceability.

### **Status Updates**
- MaterialStatus is updated based on approval actions (e.g., Assigned, InProcess, UnderMaintenance, Disposed).
- RequisitionStatus is updated as the approval workflow progresses (NotStarted → PendingDispatch → PendingReceipt → Completed/Cancelled).

---

## Notification Module Integration

- **On Step Activation:**
  - Notifies the assigned user/department/vendor that action is required.
- **On Approval/Dispatch/Receipt:**
  - Notifies the next approver and the requester as appropriate.
- **On Rejection/On Hold:**
  - Notifies the requester and relevant stakeholders with comments and required actions.
- **On Completion:**
  - Notifies the requester and all involved parties that the requisition is complete.

Notifications are sent using template-based messages, with context parameters (requisition ID, material, action required, URLs, etc.).

---

## Expected Handling at Each Approval Step

| Step Type         | Material Assignment         | Material Condition         | Notification Triggered         |
|-------------------|----------------------------|---------------------------|-------------------------------|
| Approve           | No change (unless last)    | No change (unless last)   | Next approver & requester       |
| Dispatch          | Create/Update Assignment   | Create Condition (Dispatch)| Next approver & recipient       |
| Receive           | Update Assignment (if needed)| Create Condition (Receipt)| Requester/Inventory manager   |
| Reject            | Cancel Assignment (if any) | Create Condition (Rejected)| Requester/All stakeholders    |
| On Hold           | No change                  | No change                 | Requester/Current approver    |
| Complete          | Finalize Assignment        | Final Condition           | Requester/All stakeholders    |

---

## Best Practices & Recommendations

- **Atomic Transactions:** All approval actions that affect materials or requisitions should be wrapped in database transactions to ensure consistency.
- **Audit Trail:** All changes to approval steps, assignments, and conditions should be logged for traceability.
- **Custom Workflows:** Use WorkflowConfig and StepConfig to support custom approval chains for different requisition types and contexts.
- **Validation:** Enforce business rules at each step (e.g., only allow dispatch if material is available, require comments on rejection/on hold).
- **Extensibility:** Design for new step types, notification templates, and integration points as business needs evolve.

---

## Example Flow: Dispatch & Receipt

1. **Dispatch Step:**
   - Approver marks step as Dispatched.
   - MaterialAssignment is created/updated (assigned to recipient/location).
   - MaterialCondition is created (state at dispatch).
   - Notification sent to recipient/next approver.
2. **Receipt Step:**
   - Approver marks step as Received.
   - MaterialAssignment is updated (if needed).
   - MaterialCondition is created (state at receipt).
   - Notification sent to requester/inventory manager.

---

## Summary

The approval process is the backbone of controlled material movement in the MRIV system. It ensures that every action is authorized, tracked, and communicated, with full integration to material assignment, condition tracking, and notifications. This blueprint provides a foundation for robust, auditable, and extensible approval workflows. 