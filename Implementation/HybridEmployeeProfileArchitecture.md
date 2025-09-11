# Hybrid Employee Profile Architecture Plan
## Unified Data Services with Separated Concerns

### Document Information
- **Version**: 1.0
- **Date**: December 2024
- **Purpose**: Define hybrid architecture for employee profile data across login enhancement and administrative views
- **Dependencies**: [LoginEnhancementRefactoringPlan.md](./LoginEnhancementRefactoringPlan.md)

---

## Executive Summary

### Problem Statement
The system needs to handle employee profile data (materials, requisitions, approvals) in two distinct contexts:
1. **Logged-in User Profile** - User viewing their own data with heavy caching and performance optimization
2. **Administrative Employee View** - Admin/supervisor viewing any employee's data with proper authorization

### Solution Overview
A **Hybrid Architecture** that separates concerns while maximizing code reuse through three specialized services:
- **IUserProfileService** - Login enhancement and current user caching
- **IEmployeeProfileDataService** - Shared data operations for all employee-related data
- **IEmployeeAuthorizationService** - Access control and visibility management

---

## Architecture Overview

### Service Separation Strategy

```
┌─────────────────────────────────────────────────────────┐
│                     CLIENT REQUESTS                     │
└─────────────────┬───────────────────────┬───────────────┘
                  │                       │
                  ▼                       ▼
    ┌─────────────────────────┐  ┌─────────────────────────┐
    │   LOGGED-IN USER        │  │   ADMIN EMPLOYEE        │
    │   PROFILE CONTEXT       │  │   VIEW CONTEXT          │
    └─────────┬───────────────┘  └─────────┬───────────────┘
              │                            │
              ▼                            ▼
┌─────────────────────────┐      ┌─────────────────────────┐
│  IUserProfileService    │      │ IEmployeeAuthorization  │
│  - Current user cache   │      │ Service                 │
│  - Session management   │      │ - Access validation     │
│  - Login enhancement    │      │ - Visibility rules      │
└─────────┬───────────────┘      └─────────┬───────────────┘
          │                                │
          │                                │
          └──────────┬───────────────────────┘
                     │
                     ▼
          ┌─────────────────────────┐
          │ IEmployeeProfileData    │
          │ Service                 │
          │ - Materials data        │
          │ - Requisitions data     │
          │ - Approvals data        │
          │ - Shared operations     │
          └─────────────────────────┘
```

---

## Service Architecture Details

### 1. IUserProfileService (Login Enhancement Focus)

#### Purpose
- **Primary Focus**: Login-time enhancement and current user session management
- **Scope**: Logged-in user's own data only
- **Caching Strategy**: Heavy caching with session-based storage

#### Service Interface
```csharp
public interface IUserProfileService
{
    // Core login enhancement methods
    Task<UserProfile> BuildUserProfileAsync(string payrollNo);
    Task<UserProfile> GetCurrentUserProfileAsync();
    Task RefreshCurrentUserProfileAsync();
    Task InvalidateCurrentUserProfileAsync();
    
    // Quick access methods for current user
    Task<DisplayPermissions> GetCurrentUserDisplayPermissionsAsync();
    Task<ReferencePermissions> GetCurrentUserReferencePermissionsAsync();
    Task<ActionPermissions> GetCurrentUserActionPermissionsAsync();
    
    // Current user's operational data (leverages EmployeeProfileDataService)
    Task<MaterialsIndexViewModel> GetCurrentUserMaterialsAsync(int page = 1, int pageSize = 10);
    Task<List<RequisitionViewModel>> GetCurrentUserRequisitionsAsync(int page = 1, int pageSize = 10);
    Task<List<ApprovalStepViewModel>> GetCurrentUserApprovalsAsync(int page = 1, int pageSize = 10);
}
```

#### Key Characteristics
- **Session-based caching** with 30-minute TTL
- **Automatic refresh** on role/permission changes
- **No authorization checks** (user's own data)
- **Optimized for frequent access**
- **Integrates with login enhancement plan**

### 2. IEmployeeProfileDataService (Shared Data Operations)

#### Purpose
- **Primary Focus**: Raw data operations for any employee
- **Scope**: All employee-related data regardless of context
- **Caching Strategy**: Moderate caching with shorter TTL (5-15 minutes)

#### Service Interface
```csharp
public interface IEmployeeProfileDataService
{
    // Basic employee data
    Task<EmployeeBkp> GetEmployeeBasicDataAsync(string payrollNo);
    Task<(EmployeeBkp employee, Department department, Station station)> GetEmployeeWithLocationAsync(string payrollNo);
    
    // Materials operations
    Task<MaterialsIndexViewModel> GetEmployeeMaterialsAsync(string payrollNo, int page = 1, int pageSize = 10, bool includeHistory = false);
    Task<List<Material>> GetActiveMaterialsForEmployeeAsync(string payrollNo);
    Task<List<MaterialAssignment>> GetMaterialHistoryForEmployeeAsync(string payrollNo);
    
    // Requisitions operations
    Task<List<RequisitionViewModel>> GetEmployeeRequisitionsAsync(string payrollNo, int page = 1, int pageSize = 10, bool includeAll = true);
    Task<RequisitionSummary> GetEmployeeRequisitionSummaryAsync(string payrollNo);
    
    // Approvals operations
    Task<List<ApprovalStepViewModel>> GetEmployeeApprovalsAsync(string payrollNo, int page = 1, int pageSize = 10, bool includeProcessed = true);
    Task<ApprovalSummary> GetEmployeeApprovalSummaryAsync(string payrollNo);
    
    // Consolidated operations
    Task<EmployeeProfileDataViewModel> BuildEmployeeProfileDataAsync(string payrollNo, EmployeeProfileContext context);
    Task<Dictionary<string, int>> GetEmployeeDataSummaryAsync(string payrollNo);
}
```

#### Key Characteristics
- **Context-agnostic** data operations
- **Reusable across all scenarios**
- **Consistent data structures**
- **Moderate caching** for performance
- **No authorization logic** (pure data service)

### 3. IEmployeeAuthorizationService (Access Control)

#### Purpose
- **Primary Focus**: Access control and visibility validation
- **Scope**: Authorization for administrative employee viewing
- **Caching Strategy**: Request-level caching with memory cache

#### Service Interface
```csharp
public interface IEmployeeAuthorizationService
{
    // Basic access validation
    Task<bool> CanViewEmployeeProfileAsync(string targetPayrollNo, string currentUserPayrollNo);
    Task<bool> CanViewEmployeeDataAsync(EmployeeDataType dataType, string targetPayrollNo, string currentUserPayrollNo);
    
    // Visibility level determination
    Task<EmployeeVisibilityLevel> GetEmployeeVisibilityLevelAsync(string targetPayrollNo, string currentUserPayrollNo);
    Task<List<EmployeeDataType>> GetAccessibleDataTypesAsync(string targetPayrollNo, string currentUserPayrollNo);
    
    // Context-specific validation
    Task<bool> CanViewMaterialsAsync(string targetPayrollNo, string currentUserPayrollNo);
    Task<bool> CanViewRequisitionsAsync(string targetPayrollNo, string currentUserPayrollNo);
    Task<bool> CanViewApprovalsAsync(string targetPayrollNo, string currentUserPayrollNo);
    
    // Administrative functions
    Task<List<string>> GetVisibleEmployeePayrollsAsync(string currentUserPayrollNo);
    Task<bool> IsAdministrativeUserAsync(string payrollNo);
}
```

#### Key Characteristics
- **Role-based access control**
- **Department/station visibility rules**
- **Request-level caching**
- **Integration with existing VisibilityAuthorizeService**

---

## Data Flow Patterns

### Pattern 1: Logged-in User Profile Access

#### Scenario: User clicks "My Profile" → Materials Tab
```
1. User Request → /Employees/Profile
2. EmployeesController.Profile() → IUserProfileService.GetCurrentUserProfileAsync()
3. Materials Tab Load → IUserProfileService.GetCurrentUserMaterialsAsync()
4. Behind the scenes → IEmployeeProfileDataService.GetEmployeeMaterialsAsync(currentUser.PayrollNo)
5. Return cached user context + fresh materials data
```

#### Benefits:
- **Fast initial load** from cached user profile
- **Fresh materials data** on tab activation
- **No authorization overhead**
- **Optimized for user experience**

### Pattern 2: Administrative Employee View Access

#### Scenario: Admin views employee details → Materials Tab
```
1. Admin Request → /Employees/Details/{id}
2. EmployeesController.Details() → IEmployeeAuthorizationService.CanViewEmployeeProfileAsync()
3. Materials Tab Load → IEmployeeAuthorizationService.CanViewMaterialsAsync()
4. If authorized → IEmployeeProfileDataService.GetEmployeeMaterialsAsync(targetEmployee.PayrollNo)
5. Return authorized data with proper visibility filtering
```

#### Benefits:
- **Proper authorization validation**
- **Flexible visibility rules**
- **No unnecessary caching overhead**
- **Secure administrative access**

### Pattern 3: Shared Data Operations

#### Materials Table Display (Both Contexts)
```
1. Either context → IEmployeeProfileDataService.GetEmployeeMaterialsAsync()
2. Service uses same MaterialsIndexViewModel structure
3. Same _MaterialsTable.cshtml partial for display
4. Same lookup dictionaries and data processing
5. Consistent UI experience across contexts
```

#### Benefits:
- **Zero code duplication**
- **Consistent behavior**
- **Single point of maintenance**
- **Reusable components**

---

## Implementation Strategy

### Phase 1: Core Service Implementation

#### Week 1: IEmployeeProfileDataService
```
Implementation Priority:
1. Basic employee data operations
2. Materials data operations with reusable logic
3. Integration with existing MaterialsController patterns
4. Unit tests for data operations
```

#### Week 2: IEmployeeAuthorizationService
```
Implementation Priority:
1. Integration with existing VisibilityAuthorizeService
2. Employee-specific access validation
3. Administrative role detection
4. Visibility level determination
```

#### Week 3: IUserProfileService Integration
```
Implementation Priority:
1. Current user context management
2. Integration with EmployeeProfileDataService
3. Session-based caching implementation
4. Login enhancement integration points
```

### Phase 2: UI Integration

#### Week 4: Shared Components
```
Implementation Priority:
1. Extract _MaterialsTable.cshtml partial
2. Create reusable table components
3. Update MaterialsController for reusability
4. Test both usage contexts
```

#### Week 5: Profile Implementation
```
Implementation Priority:
1. Update Employee Details page with tabs
2. Implement Materials tab with shared components
3. Add authorization checks for admin views
4. Test user profile vs admin view scenarios
```

### Phase 3: Requisitions and Approvals

#### Week 6-7: Extend Pattern
```
Implementation Priority:
1. Apply same pattern to Requisitions
2. Apply same pattern to Approvals
3. Create shared table partials
4. Comprehensive testing
```

---

## Service Dependencies and Relationships

### Dependency Injection Setup
```
services.AddScoped<IEmployeeProfileDataService, EmployeeProfileDataService>();
services.AddScoped<IEmployeeAuthorizationService, EmployeeAuthorizationService>();
services.AddScoped<IUserProfileService, UserProfileService>();

// Service relationships
IUserProfileService depends on IEmployeeProfileDataService
IEmployeeAuthorizationService depends on IVisibilityAuthorizeService
IEmployeeProfileDataService depends on existing data contexts
```

### Cross-Service Communication
```
IUserProfileService
├── Uses IEmployeeProfileDataService for current user data
├── Manages its own caching strategy
└── No authorization dependencies (user's own data)

IEmployeeAuthorizationService
├── Validates access before data operations
├── Works independently of profile services
└── Integrates with existing authorization system

IEmployeeProfileDataService
├── Pure data operations, no authorization
├── Used by both other services
└── Consistent data structures across contexts
```

---

## Caching Strategy Details

### IUserProfileService Caching
```
Cache Strategy: Multi-level
├── L1: Session storage (immediate access)
│   ├── User profile context
│   ├── Permission flags
│   └── Basic user info
├── L2: Memory cache (15-30 min TTL)
│   ├── User's materials summary
│   ├── Recent requisitions
│   └── Pending approvals
└── L3: Database cache table (1-hour TTL)
    ├── Complete user profile
    ├── Permission calculations
    └── Operational summaries
```

### IEmployeeProfileDataService Caching
```
Cache Strategy: Moderate caching
├── Memory cache (5-15 min TTL)
│   ├── Employee basic data
│   ├── Materials assignments
│   └── Recent activity summaries
└── Request-level cache
    ├── Lookup dictionaries
    ├── Station/department names
    └── Vendor information
```

### IEmployeeAuthorizationService Caching
```
Cache Strategy: Request-level
├── Per-request cache
│   ├── Authorization decisions
│   ├── Visibility levels
│   └── Accessible data types
└── Short-term memory cache (5 min TTL)
    ├── Role group memberships
    ├── Administrative user flags
    └── Department/station access
```

---

## Security Considerations

### Access Control Matrix
```
Context              | Authorization Required | Data Scope        | Caching Level
---------------------|------------------------|-------------------|---------------
Logged-in User       | No (implicit)         | Own data only     | Heavy
Admin Employee View  | Yes (explicit)        | Authorized data   | Moderate
System Operations    | Yes (service-level)   | All data          | Minimal
```

### Security Boundaries
- **IUserProfileService**: No authorization checks (user's own data)
- **IEmployeeAuthorizationService**: All authorization logic centralized
- **IEmployeeProfileDataService**: Pure data operations, relies on callers for authorization

### Data Protection
- **Session encryption** for cached user profiles
- **Memory cache isolation** between users
- **Database cache with user association** to prevent cross-contamination

---

## Performance Expectations

### Response Time Targets
```
Operation                          | Target Time | Caching Level
-----------------------------------|-------------|---------------
Current User Profile Load          | <100ms      | L1 Cache
Current User Materials Tab         | <200ms      | L2 Cache
Admin Employee Profile Load        | <300ms      | No Cache
Admin Employee Materials Tab       | <400ms      | Request Cache
Initial Login Profile Build        | <1000ms     | Database
```

### Scalability Considerations
- **Memory cache limits** to prevent server memory issues
- **Cache eviction policies** based on usage patterns
- **Database cache cleanup** jobs for expired profiles
- **Session storage optimization** for large user bases

---

## Migration Path from Current System

### Step 1: Implement Core Services
- Create IEmployeeProfileDataService with existing MaterialsController logic
- No changes to existing UI or controllers
- Backward compatibility maintained

### Step 2: Create Shared Components
- Extract _MaterialsTable.cshtml from existing Materials/Index.cshtml
- Update Materials/Index.cshtml to use partial
- Test for regression issues

### Step 3: Implement Authorization Service
- Create IEmployeeAuthorizationService with existing VisibilityAuthorizeService logic
- Add employee-specific authorization methods
- Test administrative access patterns

### Step 4: Integrate with Login Enhancement
- Implement IUserProfileService according to login enhancement plan
- Integrate with IEmployeeProfileDataService for current user operations
- Begin user profile caching

### Step 5: Update Employee Details Page
- Implement tabbed interface as already completed
- Add Materials tab using shared components
- Test both user profile and admin view contexts

---

## Testing Strategy

### Unit Testing Focus
```
IEmployeeProfileDataService:
├── Data retrieval accuracy
├── ViewMododel construction
├── Caching behavior
└── Performance benchmarks

IEmployeeAuthorizationService:
├── Access control accuracy
├── Visibility level determination
├── Edge case handling
└── Performance under load

IUserProfileService:
├── Cache effectiveness
├── Session management
├── Profile building accuracy
└── Integration with other services
```

### Integration Testing Focus
```
Cross-service Integration:
├── User profile → data service flow
├── Authorization → data service flow
├── Shared component rendering
└── Cache invalidation scenarios

UI Integration:
├── Profile materials tab functionality
├── Admin employee materials tab functionality
├── Consistent behavior across contexts
└── Performance under realistic load
```

### Performance Testing
```
Load Testing Scenarios:
├── Concurrent user profile access
├── Heavy administrative employee viewing
├── Cache hit rate measurement
└── Memory usage monitoring
```

---

## Success Criteria

### Functional Requirements
- ✅ **Code Reuse**: Single data service handles both contexts
- ✅ **Performance**: User profile loads <100ms, admin views <300ms
- ✅ **Security**: Proper authorization for all administrative access
- ✅ **Consistency**: Identical UI behavior across contexts
- ✅ **Maintainability**: Single point of maintenance for data operations

### Technical Requirements
- ✅ **Backward Compatibility**: No regression in existing functionality
- ✅ **Scalability**: Support for 500+ concurrent users
- ✅ **Cache Efficiency**: >90% cache hit rate for user profiles
- ✅ **Memory Usage**: <50MB additional memory per 100 users
- ✅ **Response Times**: All operations meet target response times

### Business Requirements
- ✅ **User Experience**: Seamless profile viewing experience
- ✅ **Administrative Efficiency**: Fast employee data access for managers
- ✅ **Security Compliance**: Full audit trail for all data access
- ✅ **Feature Parity**: All materials functionality available in both contexts
- ✅ **Future Extensibility**: Easy addition of requisitions and approvals

---

## Future Extensions

### Phase 4: Advanced Features
- **Historical data views** for employee activity over time
- **Comparative analytics** across employees and departments
- **Advanced filtering** based on user permissions
- **Export capabilities** for administrative reporting

### Phase 5: Mobile Optimization
- **Responsive design** for mobile employee profile access
- **Offline caching** for critical user profile data
- **Push notifications** for profile-related updates

### Phase 6: Integration Enhancements
- **Active Directory integration** for employee data synchronization
- **Third-party system integration** for comprehensive employee views
- **API endpoints** for external system access to employee profile data

---

## Conclusion

This Hybrid Architecture provides the optimal balance between performance, security, and maintainability for handling employee profile data across different contexts. It enables the successful implementation of both the login enhancement plan and the administrative employee viewing functionality while maintaining clear separation of concerns and maximizing code reuse.

The architecture is designed to evolve with the system's needs while providing a solid foundation for current requirements and future enhancements.

---

*This document should be referenced during implementation and updated as the architecture evolves. All implementation phases should follow this architectural guidance to ensure consistency and maintainability.*