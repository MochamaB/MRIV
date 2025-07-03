# Suggested Reports & Dashboards: Requisition Module

## Overview
These reports and dashboard items are designed to provide actionable insights and operational visibility for the Requisition Module. They are structured to support business needs at every level: global, per category, per station, per department, and per role.

---

## Core Dashboard Items
- **Total Requisition Value** (by category, station, department)
- **Requisition Status Breakdown** (Not Started, Pending Dispatch, Pending Receipt, Completed, Cancelled)
- **Requisition Type Distribution** (New Purchase, Transfer, InterFactory, etc.)
- **Active vs. Completed Requisitions**
- **Requisitions Pending Approval/Dispatch/Receipt**
- **Recent Requisitions** (created, updated, completed)
- **Overdue/Pending Requisitions**
- **Top Requesters/Departments by Value/Count**
- **Requisitions by Workflow Step** (where are most stuck?)

---

## Suggested Reports
### 1. **Requisitions by Location**
- **By Category:** List and value of all requisitions per category
- **By Station:** Count, value, and status per station
- **By Department:** Count, value, and status per department

### 2. **Requisition Status & Progress**
- **Status Report:** List of all requisitions by current status (filterable by location/type)
- **Workflow Progress Report:** Current step, pending actions, and bottlenecks
- **Aging Report:** Requisitions open/pending for more than X days

### 3. **Type & Value Analysis**
- **Type Distribution:** Breakdown of requisitions by type (New Purchase, Transfer, etc.)
- **Value Analysis:** Total value of requisitions by type, location, requester
- **High-Value Requisitions:** List of top N requisitions by value

### 4. **User & Department Activity**
- **Requester Activity:** Number and value of requisitions per user/department
- **Approval Turnaround Time:** Average time to approve/dispatch/receive per step/role
- **Pending Actions by User/Role:** List of requisitions awaiting action per user/role

### 5. **Audit & Compliance**
- **Requisition Audit Trail:** All changes to requisition records, status, and workflow
- **Rejected/Cancelled Requisitions:** List with reasons and comments

---

## Role-Based & Location-Based Access
- **Admin/Global:** Access to all reports, dashboards, and analytics
- **Station Manager:** Reports and dashboards filtered to their station
- **Department Head:** Reports and dashboards filtered to their department
- **Category Manager:** Reports filtered to their managed categories
- **Field Supervisor:** Access to requisitions and reports for their assigned stations/departments
- **User:** Limited to requisitions they created or are assigned to

---

## Example Dashboard Widgets
- Requisition Value by Department (bar chart)
- Requisition Status Pie Chart (filterable by location/type)
- Workflow Progress Funnel (steps with most pending)
- Recent Requisitions (table)
- Overdue Requisitions (list)
- Top Requesters (list)

---

> These reports and dashboards should be filterable by category, station, department, type, and role, leveraging the location-based authorization model and workflow configuration. 