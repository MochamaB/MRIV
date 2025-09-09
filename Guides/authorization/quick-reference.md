# Authorization Quick Reference

## Overview

This quick reference guide provides essential information about the MRIV authorization system, including role groups, permissions, and access levels.

## Permission Matrix

| Role Type | CanAccessAcrossStations | CanAccessAcrossDepartments | Access Scope |
|-----------|:----------------------:|:-------------------------:|--------------|
| **Default User** (no group) | NO | NO | Own department at own station |
| **Department Manager** | NO | NO | Own department at own station |
| **Station Support/Manager** | NO | YES | All departments at own station |
| **Group/General Manager** | YES | NO | Own department at all stations |
| **Administrator** | YES | YES | All data everywhere |

## Key Concepts

### Role Groups
Role groups control data visibility through two key flags:

- **CanAccessAcrossStations**: View department data across all stations
- **CanAccessAcrossDepartments**: View all departments within station scope

### Access Decision Logic

```
IF user is not in any role group
    → Access only their department at their station
ELSE IF both flags are FALSE
    → Access only their department at their station
ELSE IF CanAccessAcrossDepartments = TRUE and CanAccessAcrossStations = FALSE
    → Access all departments at their station only
ELSE IF CanAccessAcrossStations = TRUE and CanAccessAcrossDepartments = FALSE
    → Access their department at all stations
ELSE IF both flags are TRUE
    → Access all data (all departments, all stations)
```

## Common Scenarios

### Department Head
- **Role Group**: Department Manager
- **Permissions**: CanAccessAcrossStations = FALSE, CanAccessAcrossDepartments = FALSE
- **Can See**: Only requisitions and materials from their own department at their station

### Station Manager
- **Role Group**: Station Support
- **Permissions**: CanAccessAcrossStations = FALSE, CanAccessAcrossDepartments = TRUE
- **Can See**: All department data within their station

### Regional Manager
- **Role Group**: General Manager (Departmental)
- **Permissions**: CanAccessAcrossStations = TRUE, CanAccessAcrossDepartments = FALSE
- **Can See**: Their department's data across all stations

### System Administrator
- **Role Group**: Administrator
- **Permissions**: CanAccessAcrossStations = TRUE, CanAccessAcrossDepartments = TRUE
- **Can See**: Everything in the system

## Data Normalization

### Station Codes
- **HQ**: Normalized to "0"
- **Field Stations**: Normalized to 3-digit format (001, 002, etc.)

### Department Codes
- Stored as integer values
- String normalization with trimming applied

## Troubleshooting Quick Fixes

### User Can't See Expected Data
1. Check if user is assigned to appropriate role group
2. Verify role group has correct permission flags
3. Confirm user's department and station assignments
4. Check if data belongs to user's accessible scope

### Permission Denied Errors
1. Verify user's role group membership is active
2. Check if required role groups have correct flags
3. Ensure user's employee record has correct department/station

### Cross-Department Access Issues
1. Confirm CanAccessAcrossDepartments = TRUE in user's role group
2. Verify user is at the correct station for the data
3. Check if data has proper department/station assignments

## Related Links

- [Role Groups Explained](role-groups.md) - Detailed explanation of role group system
- [Troubleshooting Access Issues](troubleshooting.md) - Comprehensive troubleshooting guide
- [Managing Role Groups](../admin/role-management.md) - Administrative procedures

## Need Help?

If you're still having trouble with permissions or access control:

1. Check the [troubleshooting guide](troubleshooting.md) for common issues
2. Contact your system administrator
3. Submit feedback using the feedback button above