# Material Categories Testing Documentation

## Overview
This document contains test cases for the MaterialCategory model CRUD operations and visibility controls. MaterialCategories serve as the primary classification system for materials in the MRIV system.

## Model Structure
**MaterialCategory Properties:**
- Id (Primary Key)
- Name (Required, Unique)
- Description
- UnitOfMeasure (Required)
- IsActive (Default: true)
- CreatedAt
- UpdatedAt

## Test Cases

### CREATE Operations

#### Test Case MC-001: Create Valid Material Category
**Objective:** Verify successful creation of a material category with valid data

**Preconditions:**
- User has material category creation permissions
- Database is accessible

**Test Steps:**
1. Navigate to Material Categories → Create New
2. Enter valid data:
   - Name: "Electronics"
   - Description: "Electronic equipment and devices"
   - UnitOfMeasure: "Each"
3. Click Save

**Expected Results:**
- MaterialCategory record created successfully
- Id auto-generated
- CreatedAt timestamp populated
- IsActive defaults to true
- Success message displayed
- User redirected to category list

**Validation Points:**
- Database: Verify record exists with correct data
- UI: Category appears in list view
- UI: Success notification displayed

#### Test Case MC-002: Create Category with Duplicate Name
**Objective:** Verify system prevents duplicate category names

**Preconditions:**
- MaterialCategory with name "Electronics" already exists

**Test Steps:**
1. Navigate to Material Categories → Create New
2. Enter data:
   - Name: "Electronics" (duplicate)
   - Description: "Another electronics category"
   - UnitOfMeasure: "Each"
3. Click Save

**Expected Results:**
- Validation error displayed
- Record not created
- Error message: "A category with this name already exists"
- Form remains populated with entered data

**Validation Points:**
- Database: No duplicate record created
- UI: Clear error message displayed
- UI: Form validation prevents submission

#### Test Case MC-003: Create Category with Missing Required Fields
**Objective:** Verify required field validation

**Test Steps:**
1. Navigate to Material Categories → Create New
2. Leave Name field empty
3. Enter Description: "Test category"
4. Leave UnitOfMeasure empty
5. Click Save

**Expected Results:**
- Validation errors for required fields
- Form not submitted
- Error messages: "Name is required", "Unit of Measure is required"

**Validation Points:**
- Client-side validation prevents submission
- Server-side validation as backup
- Clear field-specific error messages

### READ Operations

#### Test Case MC-004: View All Material Categories
**Objective:** Verify category list displays correctly

**Preconditions:**
- Multiple MaterialCategories exist in database
- User has read permissions

**Test Steps:**
1. Navigate to Material Categories list
2. Observe displayed data

**Expected Results:**
- All active categories displayed
- Columns shown: Name, Description, Unit of Measure, Created Date
- Data sorted by Name (ascending)
- Pagination if more than 50 records

**Validation Points:**
- All active categories visible
- Inactive categories not shown
- Sorting functions correctly
- Pagination controls work

#### Test Case MC-005: View Category Details
**Objective:** Verify individual category details display

**Test Steps:**
1. Navigate to Material Categories list
2. Click on a category name or View button
3. Observe category details page

**Expected Results:**
- All category details displayed correctly
- Related materials count shown
- Edit/Delete buttons available (if authorized)
- Audit trail information displayed

**Validation Points:**
- All fields display correct data
- Related material count is accurate
- Action buttons respect user permissions

#### Test Case MC-006: Search Material Categories
**Objective:** Verify category search functionality

**Test Steps:**
1. Navigate to Material Categories list
2. Enter "Elect" in search box
3. Verify filtered results
4. Clear search
5. Enter non-existent category name
6. Verify no results message

**Expected Results:**
- Search returns categories matching search term
- Partial matches work (e.g., "Elect" matches "Electronics")
- Case-insensitive search
- Clear "no results" message for invalid searches

### UPDATE Operations

#### Test Case MC-007: Update Material Category Valid Data
**Objective:** Verify successful category update

**Preconditions:**
- MaterialCategory exists
- User has update permissions

**Test Steps:**
1. Navigate to category details or edit page
2. Modify Description field
3. Change UnitOfMeasure if needed
4. Click Save

**Expected Results:**
- Category updated successfully
- UpdatedAt timestamp modified
- Success message displayed
- Changes reflected in list view

**Validation Points:**
- Database: Record updated with new values
- Database: UpdatedAt timestamp changed
- UI: Updated data displayed correctly

#### Test Case MC-008: Update Category Name to Duplicate
**Objective:** Verify duplicate name validation on update

**Preconditions:**
- Two categories exist: "Electronics" and "Furniture"

**Test Steps:**
1. Edit "Furniture" category
2. Change Name to "Electronics"
3. Click Save

**Expected Results:**
- Validation error displayed
- Update not saved
- Error message about duplicate name
- Form retains entered data

#### Test Case MC-009: Update Category with Empty Required Fields
**Objective:** Verify required field validation on update

**Test Steps:**
1. Edit existing category
2. Clear the Name field
3. Click Save

**Expected Results:**
- Validation error for required field
- Update not saved
- Name field highlighted as error
- Clear error message displayed

### DELETE Operations

#### Test Case MC-010: Delete Unused Material Category
**Objective:** Verify deletion of category not linked to materials

**Preconditions:**
- MaterialCategory exists with no associated materials
- User has delete permissions

**Test Steps:**
1. Navigate to category details
2. Click Delete button
3. Confirm deletion in popup dialog

**Expected Results:**
- Category deleted successfully
- Record removed from list view
- Success message displayed
- No database constraint errors

**Validation Points:**
- Database: Record marked as IsActive = false (soft delete)
- UI: Category no longer appears in active list
- UI: Confirmation dialog prevents accidental deletion

#### Test Case MC-011: Attempt Delete Category with Associated Materials
**Objective:** Verify system prevents deletion of categories in use

**Preconditions:**
- MaterialCategory exists with associated materials

**Test Steps:**
1. Navigate to category with linked materials
2. Click Delete button
3. Confirm deletion attempt

**Expected Results:**
- Deletion prevented
- Error message: "Cannot delete category with associated materials"
- Category remains in system
- List of associated materials displayed

**Validation Points:**
- Database: Foreign key constraints enforced
- UI: Clear error message with details
- UI: Option to view associated materials

#### Test Case MC-012: Soft Delete vs Hard Delete Behavior
**Objective:** Verify deleted categories can be restored

**Test Steps:**
1. Delete a category (soft delete)
2. Navigate to "Show Inactive" view
3. Verify deleted category appears
4. Test restore functionality if available

**Expected Results:**
- Deleted categories appear in inactive view
- IsActive = false in database
- Restore option available (if implemented)
- Audit trail tracks deletion and restoration

### VISIBILITY and AUTHORIZATION

#### Test Case MC-013: Role-Based Category Access
**Objective:** Verify different roles see appropriate categories

**Test Steps:**
1. Login as regular user
2. Check available category options
3. Login as admin user
4. Compare available categories

**Expected Results:**
- Regular users see standard categories
- Admin users see all categories including inactive
- Create/Edit/Delete buttons respect user permissions
- Unauthorized actions blocked

#### Test Case MC-014: Department-Specific Category Visibility
**Objective:** Test if categories can be restricted by department (if implemented)

**Test Steps:**
1. Create department-specific categories
2. Login as users from different departments
3. Verify category visibility rules

**Expected Results:**
- Users see only relevant categories for their department
- Cross-department restrictions enforced
- Error messages for unauthorized access attempts

### INTEGRATION Testing

#### Test Case MC-015: Category-Material Relationship
**Objective:** Verify category-material integration works correctly

**Test Steps:**
1. Create new material category
2. Create materials using this category
3. Update category details
4. Verify material records reflect changes

**Expected Results:**
- Materials correctly link to categories
- Category changes don't break material links
- Material dropdown shows only active categories
- Foreign key relationships maintained

#### Test Case MC-016: Category in Material Creation
**Objective:** Test category selection during material creation

**Test Steps:**
1. Navigate to Create Material
2. Open Category dropdown
3. Verify available categories
4. Select category and create material

**Expected Results:**
- Only active categories appear in dropdown
- Categories sorted alphabetically
- Selected category correctly saved with material
- Category information displays in material details

### DATA VALIDATION

#### Test Case MC-017: Category Name Validation Rules
**Objective:** Test various name validation scenarios

**Test Data:**
- Empty string
- Single character
- Very long string (>100 chars)
- Special characters
- Numbers only
- Mixed alphanumeric

**Expected Results:**
- Appropriate validation for each scenario
- Clear error messages
- Consistent validation client and server-side

#### Test Case MC-018: Unit of Measure Validation
**Objective:** Test UnitOfMeasure field validation

**Test Steps:**
1. Test standard units: "Each", "Box", "Meter", "Kilogram"
2. Test empty value
3. Test very long value
4. Test special characters

**Expected Results:**
- Standard units accepted
- Empty value rejected
- Reasonable length limits enforced
- Special characters handled appropriately

### PERFORMANCE Testing

#### Test Case MC-019: Large Category Dataset Performance
**Objective:** Test system performance with many categories

**Setup:**
- Create 1000+ material categories
- Test list loading performance
- Test search performance

**Expected Results:**
- List loads within 3 seconds
- Search results return within 1 second
- Pagination works smoothly
- No memory leaks or browser freezing

#### Test Case MC-020: Concurrent Category Operations
**Objective:** Test concurrent access scenarios

**Test Steps:**
1. Multiple users create categories simultaneously
2. Multiple users update same category
3. One user deletes while another updates

**Expected Results:**
- Database maintains consistency
- Appropriate locking mechanisms
- Clear error messages for conflicts
- No data corruption occurs

## Test Data Setup

### Sample Categories for Testing
```sql
INSERT INTO MaterialCategory (Name, Description, UnitOfMeasure, IsActive) VALUES
('Electronics', 'Electronic equipment and devices', 'Each', 1),
('Furniture', 'Office furniture and fixtures', 'Each', 1),
('Stationery', 'Office stationery and supplies', 'Box', 1),
('Vehicles', 'Company vehicles and transport equipment', 'Each', 1),
('Safety Equipment', 'Safety and protective equipment', 'Each', 1),
('Cleaning Supplies', 'Cleaning and maintenance supplies', 'Liter', 1),
('IT Hardware', 'Computer hardware and accessories', 'Each', 1),
('Office Supplies', 'General office supplies and materials', 'Pack', 1);
```

## Automation Considerations

### Automated Test Scenarios
- CRUD operations can be automated using Selenium/Playwright
- API endpoints can be tested with REST clients
- Database validation through direct queries
- Performance tests using load testing tools

### Test Environment Requirements
- Clean database before each test run
- Test data seeding capability
- User account with various permission levels
- Isolated test environment to prevent data conflicts

## Success Criteria

### All Tests Pass When:
- CRUD operations work correctly with valid data
- Validation prevents invalid data entry
- Security controls limit unauthorized access
- Performance meets acceptable standards
- Data integrity maintained under all conditions
- User interface provides clear feedback
- Error handling is comprehensive and user-friendly

This test suite ensures MaterialCategory functionality is reliable, secure, and performant in the MRIV system.