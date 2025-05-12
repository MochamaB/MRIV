# MRIV System Restructuring Implementation Plan
# Location-Based Authorization and Data Model Improvements

## Table of Contents
1. [Executive Summary](#executive-summary)
2. [Current System Analysis](#current-system-analysis)
3. [Proposed Changes](#proposed-changes)
4. [Implementation Plan](#implementation-plan)
5. [Migration Strategy](#migration-strategy)
6. [Testing Plan](#testing-plan)
7. [Rollout Strategy](#rollout-strategy)

## Executive Summary

This document outlines a comprehensive plan to restructure the MRIV system's location-based data model and authorization system. The current implementation uses string-based location references and lacks proper station-based visibility controls, making it difficult to implement complex authorization requirements.

The proposed changes will:
1. Replace string-based location references with ID-based foreign keys
2. Add station references to the Approval model
3. Create a unified location service
4. Enhance the VisibilityAuthorizeService to support both department and station-based filtering
5. Leverage the new RoleGroup system for flexible access control

These changes will provide a solid foundation for implementing complex authorization requirements while improving data integrity, query efficiency, and system maintainability.

## Current System Analysis

### Data Model Issues

#### Requisition Model
- Uses string-based location references (`IssueStation`, `DeliveryStation`)
- Uses string categories (`IssueStationCategory`, `DeliveryStationCategory`) to determine location type
- No foreign key constraints to ensure data integrity
- Difficult to perform joins and efficient queries
- Inconsistent naming possible (e.g., "HQ" vs "Head Office")
- Case sensitivity issues
- Difficult to track location changes if a station/department name changes

#### Approval Model
- Contains `DepartmentId` but lacks `StationId`
- No `ApprovedBy` field to track who made the approval

### Service Layer Issues

#### StationCategoryService
- Complex logic to determine location type based on string patterns
- Makes assumptions about station names (e.g., containing "region")
- Dynamically loads location data from different sources based on category
- Fragile and prone to errors as naming conventions change

#### VisibilityAuthorizeService
- Only filters by department, not by station
- Cannot handle complex authorization scenarios like:
  - ICT staff at a factory needing to see all factory approvals
  - Department-specific visibility at HQ
  - Cross-departmental access for support teams

### UI Issues
- Forms and dropdowns use string values instead of IDs
- No clear separation between department and station selection
- Wizard flow assumes string-based location handling

## Proposed Changes

Please refer to the separate documents for detailed proposed changes:
- [Data Model Changes](./LocationBasedAuthorizationDataModel.md)
- [Service Layer Changes](./LocationBasedAuthorizationServices.md)
- [UI Changes](./LocationBasedAuthorizationUI.md)
- [Controller Changes](./LocationBasedAuthorizationControllers.md)

## Implementation Plan

### Phase 1: Data Model Updates (2 weeks)
1. Create new location-related enums
2. Update Requisition model with ID-based location references
3. Update Approval model with StationId and ApprovedBy fields
4. Create database migration scripts
5. Update DbContext configurations

### Phase 2: Service Layer Updates (2 weeks)
1. Create ILocationService interface and implementation
2. Update VisibilityAuthorizeService to support station-based filtering
3. Implement RoleGroupAccessService to leverage the new RoleGroup system
4. Update existing services to use the new location model

### Phase 3: Controller and Repository Updates (2 weeks)
1. Update MaterialRequisitionController to use ID-based location references
2. Update ApprovalsController to record who approved and use station IDs
3. Update repositories to use the new data model

### Phase 4: UI Updates (3 weeks)
1. Update Requisition wizard views to use ID-based dropdowns
2. Add location type selection to requisition forms
3. Update JavaScript to handle the new location model
4. Update approval views to show station information and who approved

### Phase 5: Testing and Deployment (1 week)
1. Comprehensive testing of all changes
2. Data migration from string-based to ID-based references
3. Deployment to production

## Migration Strategy

### Database Migration
1. Add new ID-based fields to Requisition and Approval tables (nullable initially)
2. Create a script to populate new ID fields based on existing string values
3. Once all records are migrated, make the new fields required and remove old fields

### Application Migration
1. Deploy updated application that can work with both old and new fields
2. Run data migration scripts
3. Deploy final application version that only uses new fields

## Testing Plan

### Unit Tests
1. Test all new and updated services
2. Test data model validations
3. Test authorization logic

### Integration Tests
1. Test end-to-end requisition creation flow
2. Test approval workflows with different user roles and locations
3. Test visibility rules for different scenarios

### User Acceptance Testing
1. Test with real users from different departments and stations
2. Verify all use cases work as expected

## Rollout Strategy

### Phased Rollout
1. Deploy to development environment
2. Deploy to staging environment with production data copy
3. Deploy to production during off-hours
4. Monitor system for issues

### Rollback Plan
1. Keep backup of all production data before migration
2. Prepare rollback scripts
3. Test rollback procedure in staging environment
