# Role Group Access Guide
# Understanding Location-Based Authorization in MRIV

## Table of Contents
1. [Introduction](#introduction)
2. [Access Control Flags](#access-control-flags)
3. [How Access Control Works](#how-access-control-works)
4. [Predefined Role Groups](#predefined-role-groups)
5. [Example Scenarios](#example-scenarios)
6. [Implementation Guidelines](#implementation-guidelines)

## Introduction

The MRIV system uses a Role Group-based authorization system to control access to requisitions, approvals, and other resources based on the user's department and station. This guide explains how the access control flags work and provides examples for different scenarios.

The system is designed around a hierarchical organization structure:
- **Stations** (e.g., HQ, Factory A, Region B) are the top-level locations
- **Departments** (e.g., ICT, HR, Finance) exist within stations

## Access Control Flags

The Role Group model includes two key flags that control access across organizational boundaries:

### 1. CanAccessAcrossStations (formerly HasFullDepartmentAccess)

This flag determines if members can access data from their department across all stations.

- **When TRUE**: Members can see all requisitions/approvals for their department, regardless of station
- **When FALSE**: Members can only see requisitions/approvals for their department within their station

### 2. CanAccessAcrossDepartments (formerly HasFullStationAccess)

This flag determines if members can access data from all departments within their station.

- **When TRUE**: Members can see all requisitions/approvals within their station, regardless of department
- **When FALSE**: Members can only see requisitions/approvals for their own department within their station

## How Access Control Works

The access control system evaluates permissions based on a combination of:
1. The user's department and station
2. The department and station associated with the requisition/approval
3. The access flags of the role groups the user belongs to

### Access Decision Logic

```
IF user is Admin
    GRANT access to all resources
ELSE IF user has both CanAccessAcrossStations AND CanAccessAcrossDepartments
    GRANT access to all resources
ELSE
    START with no access
    
    IF user has CanAccessAcrossStations
        GRANT access to all resources in user's department across all stations
    ELSE IF user has CanAccessAcrossDepartments
        GRANT access to all resources in user's station across all departments
    ELSE
        GRANT access only to resources in user's department AND user's station
```

## Predefined Role Groups

Based on common organizational needs, we recommend creating the following predefined role groups:

### 1. Department Manager

```
Name: "Department Manager"
Description: "Can access department data across all stations"
CanAccessAcrossStations: TRUE
CanAccessAcrossDepartments: FALSE
```

Department managers need to see all requisitions/approvals for their department across all stations, but don't need to see other departments' data.

### 2. Station Support

```
Name: "Station Support"
Description: "Can access all departments' data within their station"
CanAccessAcrossStations: FALSE
CanAccessAcrossDepartments: TRUE
```

Support staff need to see all requisitions/approvals within their station, regardless of department, but don't need to see data from other stations.

### 3. Global Administrator

```
Name: "Global Administrator"
Description: "Can access all data across all departments and stations"
CanAccessAcrossStations: TRUE
CanAccessAcrossDepartments: TRUE
```

Administrators need full access to all requisitions/approvals across all departments and stations.

### 4. Standard User

```
Name: "Standard User"
Description: "Can only access their department's data within their station"
CanAccessAcrossStations: FALSE
CanAccessAcrossDepartments: FALSE
```

Regular users only need to see requisitions/approvals for their department within their station.

## Example Scenarios

### Head Office (HQ) Scenarios

#### ICT Support at HQ

```
Role Group: "HQ ICT Support"
Description: "ICT staff supporting all departments at HQ"
CanAccessAcrossStations: FALSE
CanAccessAcrossDepartments: TRUE
```

**What They Can See:**
- ✅ All requisitions/approvals at HQ, regardless of department
- ❌ ICT requisitions/approvals at factories or regions

**Example:** An ICT support technician at HQ needs to see all IT-related requisitions from all departments at HQ to provide support, but doesn't need to see factory ICT requisitions.

#### ICT Manager at HQ

```
Role Group: "ICT Department Manager"
Description: "Manages ICT across all stations"
CanAccessAcrossStations: TRUE
CanAccessAcrossDepartments: FALSE
```

**What They Can See:**
- ✅ All ICT requisitions/approvals across all stations (HQ, factories, regions)
- ❌ Non-ICT requisitions/approvals at HQ or elsewhere

**Example:** The ICT Manager oversees all ICT operations across the organization and needs to see all ICT requisitions/approvals, but doesn't need to see HR or Finance requisitions.

#### HQ Administrator

```
Role Group: "HQ Administrator"
Description: "Administers all departments at HQ"
CanAccessAcrossStations: FALSE
CanAccessAcrossDepartments: TRUE
```

**What They Can See:**
- ✅ All requisitions/approvals at HQ, regardless of department
- ❌ Any requisitions/approvals at factories or regions

**Example:** An administrative assistant to the CEO needs to see all requisitions/approvals at HQ to coordinate activities, but doesn't need to see factory or region data.

### Factory Scenarios

#### Factory Manager

```
Role Group: "Factory Manager"
Description: "Manages all departments within a factory"
CanAccessAcrossStations: FALSE
CanAccessAcrossDepartments: TRUE
```

**What They Can See:**
- ✅ All requisitions/approvals at their factory, regardless of department
- ❌ Any requisitions/approvals at HQ or other factories/regions

**Example:** A factory manager needs to oversee all operations within their factory and see all requisitions/approvals, but doesn't need to see data from other locations.

#### Factory ICT Support

```
Role Group: "Factory ICT Support"
Description: "ICT staff supporting all departments at a factory"
CanAccessAcrossStations: FALSE
CanAccessAcrossDepartments: TRUE
```

**What They Can See:**
- ✅ All requisitions/approvals at their factory, regardless of department
- ❌ Any requisitions/approvals at HQ or other factories/regions

**Example:** An ICT support technician at a factory needs to see all IT-related requisitions from all departments at their factory to provide support.

#### Factory Department Head

```
Role Group: "Factory Department Head"
Description: "Manages a department within a factory"
CanAccessAcrossStations: FALSE
CanAccessAcrossDepartments: FALSE
```

**What They Can See:**
- ✅ Requisitions/approvals for their department at their factory
- ❌ Requisitions/approvals for other departments at their factory
- ❌ Any requisitions/approvals at HQ or other factories/regions

**Example:** A production department head at a factory only needs to see requisitions/approvals for their department.

### Cross-Location Scenarios

#### Regional ICT Manager

```
Role Group: "Regional ICT Manager"
Description: "Manages ICT across multiple factories in a region"
CanAccessAcrossStations: TRUE
CanAccessAcrossDepartments: FALSE
```

**What They Can See:**
- ✅ All ICT requisitions/approvals across all factories in their region
- ❌ Non-ICT requisitions/approvals at any location

**Example:** A regional ICT manager oversees ICT operations across multiple factories and needs to see all ICT requisitions/approvals in those locations.

#### Receiving Team

```
Role Group: "Receiving Team"
Description: "Handles receiving at their station"
CanAccessAcrossStations: FALSE
CanAccessAcrossDepartments: TRUE
```

**What They Can See:**
- ✅ All requisitions/approvals at their station, regardless of department
- ❌ Any requisitions/approvals at other stations

**Example:** A receiving team at a factory needs to see all incoming requisitions regardless of department to coordinate receiving activities.

## Implementation Guidelines

### 1. Role Group Creation

When creating role groups, consider:
- The organizational level (station, department, or cross-cutting)
- The specific access needs of the group
- The principle of least privilege (grant only the access needed)

### 2. User Interface

The role group creation/edit form should:
- Use clear, descriptive labels for the access flags
- Include help text explaining what each flag does
- Show examples of what access will be granted

Example UI text:
```
[ ] Can access across stations
    Users can see their department's data at all stations
    Example: ICT Manager can see ICT requisitions from all factories

[ ] Can access across departments
    Users can see all departments' data at their station
    Example: Factory Support can see all requisitions at their factory
```

### 3. Testing Access Control

After setting up role groups, test access control by:
1. Creating test users in different departments and stations
2. Adding them to different role groups
3. Creating test requisitions with different department/station combinations
4. Verifying that users can only see the requisitions they should have access to

### 4. Auditing

Regularly audit role group memberships to ensure:
- Users have appropriate access levels
- Access is revoked when no longer needed
- New role groups are created as organizational needs change
