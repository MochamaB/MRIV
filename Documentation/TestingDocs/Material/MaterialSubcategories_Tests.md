# Material Subcategories Testing Documentation

## Overview
This document contains test cases for the MaterialSubcategory model CRUD operations and visibility controls. MaterialSubcategories provide detailed classification within MaterialCategories for better material organization in the MRIV system.

## Model Structure
**MaterialSubcategory Properties:**
- Id (Primary Key)
- MaterialCategoryId (Foreign Key to MaterialCategory)
- Name (Required, Unique within category)
- Description
- IsActive (Default: true)
- CreatedAt
- UpdatedAt

**Relationships:**
- Belongs to one MaterialCategory
- Can have many Materials

## Test Cases

### CREATE Operations

#### Test Case MSC-001: Create Valid Material Subcategory
**Objective:** Verify successful creation of a material subcategory with valid data

**Preconditions:**
- MaterialCategory "Electronics" exists
- User has subcategory creation permissions
- Database is accessible

**Test Steps:**
1. Navigate to Material Subcategories → Create New
2. Select Parent Category: "Electronics"
3. Enter valid data:
   - Name: "Laptops"
   - Description: "Portable computers and notebooks"
4. Click Save

**Expected Results:**
- MaterialSubcategory record created successfully
- Id auto-generated
- MaterialCategoryId correctly linked
- CreatedAt timestamp populated
- IsActive defaults to true
- Success message displayed
- User redirected to subcategory list

**Validation Points:**
- Database: Verify record exists with correct data
- Database: Foreign key relationship to category maintained
- UI: Subcategory appears in list view under correct category
- UI: Success notification displayed

#### Test Case MSC-002: Create Subcategory with Duplicate Name in Same Category
**Objective:** Verify system prevents duplicate subcategory names within same category

**Preconditions:**
- MaterialCategory "Electronics" exists
- MaterialSubcategory "Laptops" exists under "Electronics"

**Test Steps:**
1. Navigate to Material Subcategories → Create New
2. Select Parent Category: "Electronics"
3. Enter data:
   - Name: "Laptops" (duplicate within same category)
   - Description: "Another laptops subcategory"
4. Click Save

**Expected Results:**
- Validation error displayed
- Record not created
- Error message: "A subcategory with this name already exists in this category"
- Form remains populated with entered data

**Validation Points:**
- Database: No duplicate record created
- UI: Clear error message displayed
- UI: Form validation prevents submission

#### Test Case MSC-003: Create Subcategory with Same Name in Different Category
**Objective:** Verify system allows same subcategory name in different categories

**Preconditions:**
- MaterialCategory "Electronics" and "Furniture" exist
- MaterialSubcategory "Accessories" exists under "Electronics"

**Test Steps:**
1. Navigate to Material Subcategories → Create New
2. Select Parent Category: "Furniture"
3. Enter data:
   - Name: "Accessories" (same name, different category)
   - Description: "Furniture accessories and fittings"
4. Click Save

**Expected Results:**
- MaterialSubcategory created successfully
- No validation error
- Two "Accessories" subcategories exist under different categories
- Success message displayed

**Validation Points:**
- Database: Both records exist with different MaterialCategoryId
- UI: Both subcategories visible under their respective categories

#### Test Case MSC-004: Create Subcategory with Missing Required Fields
**Objective:** Verify required field validation

**Test Steps:**
1. Navigate to Material Subcategories → Create New
2. Leave Category field unselected
3. Leave Name field empty
4. Enter Description: "Test subcategory"
5. Click Save

**Expected Results:**
- Validation errors for required fields
- Form not submitted
- Error messages: "Category is required", "Name is required"

**Validation Points:**
- Client-side validation prevents submission
- Server-side validation as backup
- Clear field-specific error messages

#### Test Case MSC-005: Create Subcategory for Inactive Category
**Objective:** Verify subcategories cannot be created for inactive categories

**Preconditions:**
- MaterialCategory exists but is marked IsActive = false

**Test Steps:**
1. Navigate to Material Subcategories → Create New
2. Attempt to select inactive category
3. Enter subcategory details
4. Click Save

**Expected Results:**
- Inactive categories not shown in dropdown
- If somehow selected, validation error on save
- Error message: "Cannot create subcategory for inactive category"

### READ Operations

#### Test Case MSC-006: View All Material Subcategories
**Objective:** Verify subcategory list displays correctly

**Preconditions:**
- Multiple MaterialSubcategories exist across different categories
- User has read permissions

**Test Steps:**
1. Navigate to Material Subcategories list
2. Observe displayed data

**Expected Results:**
- All active subcategories displayed
- Columns shown: Category Name, Subcategory Name, Description, Created Date
- Data grouped by category
- Sorting available by category and name
- Pagination if more than 50 records

**Validation Points:**
- All active subcategories visible
- Inactive subcategories not shown
- Category grouping works correctly
- Sorting functions properly

#### Test Case MSC-007: View Subcategories by Category Filter
**Objective:** Verify category-based filtering

**Test Steps:**
1. Navigate to Material Subcategories list
2. Select "Electronics" from category filter dropdown
3. Observe filtered results
4. Change to "All Categories"
5. Verify all subcategories shown again

**Expected Results:**
- Only subcategories from selected category displayed
- Filter dropdown shows all available categories
- "All Categories" option shows complete list
- Filter state maintained during page navigation

#### Test Case MSC-008: View Subcategory Details
**Objective:** Verify individual subcategory details display

**Test Steps:**
1. Navigate to Material Subcategories list
2. Click on a subcategory name or View button
3. Observe subcategory details page

**Expected Results:**
- All subcategory details displayed correctly
- Parent category information shown
- Related materials count displayed
- Edit/Delete buttons available (if authorized)
- Audit trail information displayed

**Validation Points:**
- All fields display correct data
- Parent category link functional
- Related material count is accurate
- Action buttons respect user permissions

#### Test Case MSC-009: Search Material Subcategories
**Objective:** Verify subcategory search functionality

**Test Steps:**
1. Navigate to Material Subcategories list
2. Enter "Lap" in search box
3. Verify filtered results include "Laptops"
4. Clear search
5. Enter non-existent subcategory name
6. Verify no results message

**Expected Results:**
- Search returns subcategories matching search term
- Partial matches work (case-insensitive)
- Search includes both name and description
- Clear "no results" message for invalid searches

### UPDATE Operations

#### Test Case MSC-010: Update Material Subcategory Valid Data
**Objective:** Verify successful subcategory update

**Preconditions:**
- MaterialSubcategory exists
- User has update permissions

**Test Steps:**
1. Navigate to subcategory details or edit page
2. Modify Description field
3. Change Name if needed (ensuring no conflicts)
4. Click Save

**Expected Results:**
- Subcategory updated successfully
- UpdatedAt timestamp modified
- Success message displayed
- Changes reflected in list view

**Validation Points:**
- Database: Record updated with new values
- Database: UpdatedAt timestamp changed
- UI: Updated data displayed correctly

#### Test Case MSC-011: Update Subcategory Name to Duplicate in Same Category
**Objective:** Verify duplicate name validation on update within same category

**Preconditions:**
- Two subcategories exist in "Electronics": "Laptops" and "Desktops"

**Test Steps:**
1. Edit "Desktops" subcategory
2. Change Name to "Laptops"
3. Click Save

**Expected Results:**
- Validation error displayed
- Update not saved
- Error message about duplicate name within category
- Form retains entered data

#### Test Case MSC-012: Update Subcategory Parent Category
**Objective:** Verify parent category can be changed

**Preconditions:**
- Subcategory "Accessories" exists under "Electronics"
- Category "Furniture" exists
- No materials linked to this subcategory

**Test Steps:**
1. Edit "Accessories" subcategory
2. Change Category from "Electronics" to "Furniture"
3. Click Save

**Expected Results:**
- Parent category updated successfully
- Subcategory now appears under "Furniture" category
- MaterialCategoryId updated in database
- Success message displayed

**Validation Points:**
- Database: MaterialCategoryId changed correctly
- UI: Subcategory moved to correct category group
- No data integrity issues

#### Test Case MSC-013: Update Subcategory with Linked Materials
**Objective:** Verify update behavior when materials are linked

**Preconditions:**
- Subcategory has associated materials

**Test Steps:**
1. Edit subcategory with linked materials
2. Attempt to change parent category
3. Modify description only
4. Save changes

**Expected Results:**
- Category change may be restricted (business rule dependent)
- Description changes allowed
- Warning message if category change affects materials
- Audit trail records changes

### DELETE Operations

#### Test Case MSC-014: Delete Unused Material Subcategory
**Objective:** Verify deletion of subcategory not linked to materials

**Preconditions:**
- MaterialSubcategory exists with no associated materials
- User has delete permissions

**Test Steps:**
1. Navigate to subcategory details
2. Click Delete button
3. Confirm deletion in popup dialog

**Expected Results:**
- Subcategory deleted successfully
- Record removed from list view
- Success message displayed
- No database constraint errors

**Validation Points:**
- Database: Record marked as IsActive = false (soft delete)
- UI: Subcategory no longer appears in active list
- UI: Confirmation dialog prevents accidental deletion

#### Test Case MSC-015: Attempt Delete Subcategory with Associated Materials
**Objective:** Verify system prevents deletion of subcategories in use

**Preconditions:**
- MaterialSubcategory exists with associated materials

**Test Steps:**
1. Navigate to subcategory with linked materials
2. Click Delete button
3. Confirm deletion attempt

**Expected Results:**
- Deletion prevented
- Error message: "Cannot delete subcategory with associated materials"
- Subcategory remains in system
- List of associated materials displayed (optional)

**Validation Points:**
- Database: Foreign key constraints enforced
- UI: Clear error message with details
- UI: Option to view associated materials

#### Test Case MSC-016: Soft Delete vs Hard Delete Behavior
**Objective:** Verify deleted subcategories can be restored

**Test Steps:**
1. Delete a subcategory (soft delete)
2. Navigate to "Show Inactive" view
3. Verify deleted subcategory appears
4. Test restore functionality if available

**Expected Results:**
- Deleted subcategories appear in inactive view
- IsActive = false in database
- Restore option available (if implemented)
- Audit trail tracks deletion and restoration

### VISIBILITY and AUTHORIZATION

#### Test Case MSC-017: Role-Based Subcategory Access
**Objective:** Verify different roles see appropriate subcategories

**Test Steps:**
1. Login as regular user
2. Check available subcategory options
3. Login as admin user
4. Compare available subcategories

**Expected Results:**
- Regular users see standard subcategories
- Admin users see all subcategories including inactive
- Create/Edit/Delete buttons respect user permissions
- Unauthorized actions blocked

#### Test Case MSC-018: Department-Specific Subcategory Visibility
**Objective:** Test if subcategories can be restricted by department (if implemented)

**Test Steps:**
1. Create department-specific subcategories
2. Login as users from different departments
3. Verify subcategory visibility rules

**Expected Results:**
- Users see only relevant subcategories for their department
- Cross-department restrictions enforced
- Error messages for unauthorized access attempts

### INTEGRATION Testing

#### Test Case MSC-019: Subcategory-Material Relationship
**Objective:** Verify subcategory-material integration works correctly

**Test Steps:**
1. Create new material subcategory
2. Create materials using this subcategory
3. Update subcategory details
4. Verify material records reflect changes

**Expected Results:**
- Materials correctly link to subcategories
- Subcategory changes don't break material links
- Material dropdown shows only active subcategories
- Foreign key relationships maintained

#### Test Case MSC-020: Subcategory in Material Creation
**Objective:** Test subcategory selection during material creation

**Test Steps:**
1. Navigate to Create Material
2. Select a Category (e.g., "Electronics")
3. Verify Subcategory dropdown populates with relevant subcategories
4. Select subcategory and create material

**Expected Results:**
- Subcategory dropdown filtered by selected category
- Only active subcategories appear
- Subcategories sorted alphabetically
- Selected subcategory correctly saved with material

#### Test Case MSC-021: Category-Subcategory Cascade Behavior
**Objective:** Test behavior when parent category is deactivated

**Test Steps:**
1. Create category with subcategories
2. Deactivate the parent category
3. Check subcategory visibility and behavior

**Expected Results:**
- Business rule dependent:
  - Option A: Subcategories also become inactive
  - Option B: Warning before category deactivation
  - Option C: Prevent category deactivation if subcategories exist
- Clear user guidance on the chosen behavior

### DATA VALIDATION

#### Test Case MSC-022: Subcategory Name Validation Rules
**Objective:** Test various name validation scenarios

**Test Data:**
- Empty string
- Single character
- Very long string (>100 chars)
- Special characters
- Numbers only
- Mixed alphanumeric with spaces

**Expected Results:**
- Appropriate validation for each scenario
- Clear error messages
- Consistent validation client and server-side

#### Test Case MSC-023: Parent Category Validation
**Objective:** Test parent category relationship validation

**Test Steps:**
1. Test with valid active category
2. Test with inactive category
3. Test with non-existent category ID
4. Test with null category

**Expected Results:**
- Only active categories accepted
- Inactive/invalid categories rejected
- Clear error messages for invalid selections
- Dropdown prevents invalid selections

### HIERARCHY and NAVIGATION

#### Test Case MSC-024: Category-Subcategory Hierarchy Display
**Objective:** Verify hierarchical display in various interfaces

**Test Steps:**
1. Check subcategory list grouped by category
2. Check material creation form hierarchy
3. Check reporting interfaces
4. Check search/filter interfaces

**Expected Results:**
- Clear parent-child relationship display
- Intuitive navigation between levels
- Breadcrumb navigation where applicable
- Consistent hierarchy across interfaces

#### Test Case MSC-025: Subcategory Sorting and Ordering
**Objective:** Test sorting behavior across hierarchy

**Test Steps:**
1. Create subcategories with various names (A-Z, numbers, special chars)
2. Test sorting within categories
3. Test global sorting across categories

**Expected Results:**
- Subcategories sorted alphabetically within each category
- Categories themselves sorted alphabetically
- Custom sorting options available if implemented
- Consistent sorting across all interfaces

### PERFORMANCE Testing

#### Test Case MSC-026: Large Subcategory Dataset Performance
**Objective:** Test system performance with many subcategories

**Setup:**
- Create 500+ subcategories across 20+ categories
- Test list loading performance
- Test filtered views performance

**Expected Results:**
- List loads within 3 seconds
- Category filtering works within 1 second
- Search results return quickly
- No browser performance issues

#### Test Case MSC-027: Concurrent Subcategory Operations
**Objective:** Test concurrent access scenarios

**Test Steps:**
1. Multiple users create subcategories in same category simultaneously
2. Users update same subcategory concurrently
3. One user deletes while another updates

**Expected Results:**
- Database maintains consistency
- Appropriate locking mechanisms
- Clear error messages for conflicts
- No data corruption occurs

## Test Data Setup

### Sample Subcategories for Testing
```sql
-- Electronics subcategories
INSERT INTO MaterialSubcategory (MaterialCategoryId, Name, Description, IsActive) VALUES
(1, 'Laptops', 'Portable computers and notebooks', 1),
(1, 'Desktops', 'Desktop computers and workstations', 1),
(1, 'Monitors', 'Computer monitors and displays', 1),
(1, 'Printers', 'Printing devices and equipment', 1),
(1, 'Accessories', 'Computer accessories and peripherals', 1);

-- Furniture subcategories  
INSERT INTO MaterialSubcategory (MaterialCategoryId, Name, Description, IsActive) VALUES
(2, 'Chairs', 'Office chairs and seating', 1),
(2, 'Desks', 'Office desks and workstations', 1),
(2, 'Storage', 'Filing cabinets and storage units', 1),
(2, 'Tables', 'Meeting and conference tables', 1);

-- Stationery subcategories
INSERT INTO MaterialSubcategory (MaterialCategoryId, Name, Description, IsActive) VALUES
(3, 'Writing Materials', 'Pens, pencils, and writing supplies', 1),
(3, 'Paper Products', 'Various paper types and notebooks', 1),
(3, 'Binding Supplies', 'Staplers, clips, and binding materials', 1);
```

## Automation Considerations

### Automated Test Scenarios
- CRUD operations automation with Selenium/Playwright
- API endpoint testing for subcategory management
- Database validation through direct queries
- Performance testing with large datasets
- Hierarchy navigation automation

### Test Environment Requirements
- Clean database with sample categories
- Test data seeding for subcategories
- User accounts with various permission levels
- Isolated test environment
- Performance monitoring tools

## Success Criteria

### All Tests Pass When:
- CRUD operations work correctly for subcategories
- Parent-child relationships maintained properly
- Validation prevents duplicate names within categories
- Allows same names across different categories
- Security controls limit unauthorized access
- Performance acceptable with large datasets
- Data integrity maintained under all conditions
- User interface provides intuitive hierarchy navigation
- Error handling comprehensive and user-friendly
- Integration with materials functions correctly

This comprehensive test suite ensures MaterialSubcategory functionality is reliable, maintains proper hierarchy, and integrates seamlessly with the broader MRIV system.