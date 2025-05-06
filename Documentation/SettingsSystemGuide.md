# MRIV Settings System: Architecture and Implementation Guide

## Table of Contents
1. [System Overview](#system-overview)
2. [Component Breakdown](#component-breakdown)
3. [Data Flow and Interactions](#data-flow-and-interactions)
4. [Implementation Examples](#implementation-examples)
5. [Integration Guide](#integration-guide)

## System Overview

The MRIV Settings System is a flexible, hierarchical configuration system that allows settings to be defined at three levels:

- **Global**: Settings that apply to the entire application
- **Module**: Settings specific to a particular module/controller
- **User**: Personal preferences for individual users

This hierarchical approach enables sensible defaults while allowing customization where needed. The system is built with performance in mind, utilizing caching to minimize database queries.

## Component Breakdown

### 1. Database Models

#### `SettingDefinition`
- **Purpose**: Defines what settings exist in the system
- **Key Properties**:
  - `Key`: Unique identifier for the setting
  - `Name`: Display name
  - `Description`: Explanation of the setting's purpose
  - `DataType`: Type of data (String, Int, Boolean, etc.)
  - `DefaultValue`: Default value if no custom value is set
  - `IsUserConfigurable`: Whether users can customize this setting
  - `ModuleName`: Module this setting belongs to (null for global)
  - `GroupId`: Group this setting belongs to for organization

#### `SettingValue`
- **Purpose**: Stores actual values for settings at different scopes
- **Key Properties**:
  - `SettingDefinitionId`: Reference to the setting definition
  - `Value`: The actual value (stored as string)
  - `Scope`: Global, Module, or User
  - `ScopeId`: Identifier for the scope (null for global, module name for module, user ID for user)

#### `SettingGroup`
- **Purpose**: Organizes settings into logical groups
- **Key Properties**:
  - `Name`: Group name
  - `Description`: Group description
  - `DisplayOrder`: Controls the order of display
  - `ParentGroupId`: Optional parent group for hierarchical organization

### 2. Enums

#### `SettingScope`
- **Purpose**: Defines the scope levels for settings
- **Values**:
  - `Global = 0`: Application-wide settings
  - `Module = 1`: Module-specific settings
  - `User = 2`: User-specific settings

#### `SettingDataType`
- **Purpose**: Defines the data types that can be stored
- **Values**:
  - `String = 0`: Text values
  - `Int = 1`: Integer values
  - `Decimal = 2`: Decimal values
  - `Boolean = 3`: True/false values
  - `DateTime = 4`: Date and time values
  - `Json = 5`: Complex data stored as JSON
  - `StringArray = 6`: Array of strings (comma-separated)

### 3. Constants

#### `SettingConstants`
- **Purpose**: Centralizes setting keys and default values
- **Key Sections**:
  - `GlobalSettings`: Keys for global settings
  - `ModuleSettings`: Keys for module-specific settings
  - `UserSettings`: Keys for user-specific settings
  - `DefaultValues`: Default values for settings

### 4. Services

#### `ISettingsService` (Interface)
- **Purpose**: Defines the contract for accessing settings
- **Key Methods**:
  - `GetSetting<T>`: Gets a setting value with specific scope
  - `UpdateSetting<T>`: Updates a setting value
  - `GetGlobalSetting<T>`: Gets a global setting
  - `GetModuleSetting<T>`: Gets a module setting
  - `GetUserSetting<T>`: Gets a user setting
  - `GetEffectiveSetting<T>`: Gets the effective setting using hierarchical resolution
  - `InvalidateCache`: Clears the cache for settings

#### `SettingsService` (Implementation)
- **Purpose**: Implements the settings service with caching
- **Key Features**:
  - Memory caching for performance
  - Hierarchical resolution of settings
  - Type conversion between string storage and typed values
  - Database operations using Entity Framework Core

#### `SettingTypeConverter` (Helper)
- **Purpose**: Handles conversion between string storage and typed values
- **Key Methods**:
  - `ConvertFromString<T>`: Converts stored string to typed value
  - `ConvertToString<T>`: Converts typed value to string for storage
  - `IsValidForDataType<T>`: Validates if a value is compatible with a data type

### 5. View Models

#### `SettingsViewModel`
- **Purpose**: Main view model for the settings page
- **Key Properties**:
  - `Groups`: List of setting groups with their settings
  - `Scope`: Current scope being viewed
  - `ScopeId`: ID of the current scope
  - `AvailableModules`: List of modules for navigation
  - `AvailableUsers`: List of users for navigation

#### `SettingGroupViewModel`
- **Purpose**: Represents a group of settings in the UI
- **Key Properties**:
  - `Name`: Group name
  - `Description`: Group description
  - `Settings`: List of settings in this group

#### `SettingItemViewModel`
- **Purpose**: Represents an individual setting in the UI
- **Key Properties**:
  - `Key`: Setting key
  - `Name`: Display name
  - `Description`: Description
  - `DataType`: Data type
  - `Value`: Current value
  - `DefaultValue`: Default value
  - `HasCustomValue`: Whether this has a custom value for the current scope

#### `UpdateSettingViewModel`
- **Purpose**: Used for AJAX updates to settings
- **Key Properties**:
  - `Key`: Setting key
  - `Value`: New value
  - `Scope`: Scope to update
  - `ScopeId`: ID of the scope
  - `ResetToDefault`: Whether to reset to default value

### 6. Controller

#### `SettingsController`
- **Purpose**: Handles the settings admin interface
- **Key Actions**:
  - `Global`: Shows global settings
  - `Module`: Shows module-specific settings
  - `UserSettings`: Shows user-specific settings
  - `MySettings`: Shows current user's settings
  - `UpdateSetting`: AJAX endpoint for updating settings

### 7. Views

#### `Index.cshtml`
- **Purpose**: Main view for displaying and editing settings
- **Key Features**:
  - Navigation between different scopes
  - Accordion display of setting groups
  - Dynamic rendering of settings based on data type

#### `_SettingValueEditor.cshtml`
- **Purpose**: Partial view for rendering setting inputs
- **Key Feature**: Renders different input controls based on data type

## Data Flow and Interactions

### 1. Initialization Flow

1. **Database Setup**:
   - Models define the database schema
   - Migration creates the tables
   - Seeder populates initial settings

2. **Service Registration**:
   - `SettingsService` is registered in dependency injection
   - `ISettingsService` interface is used for accessing settings

3. **Cache Initialization**:
   - First request for settings populates the cache
   - Subsequent requests use cached values until invalidated

### 2. Setting Retrieval Flow

When a controller or view needs a setting value:

1. **Request**: Controller calls `_settingsService.GetEffectiveSetting<T>("SettingKey", userId, moduleKey)`

2. **Cache Check**:
   - Service checks if the value is in the cache
   - If found, returns the cached value
   - If not, proceeds to database lookup

3. **Hierarchical Resolution**:
   - First checks for user-specific setting
   - If not found, checks for module-specific setting
   - If still not found, uses global setting
   - If no value exists at any level, uses the default value from definition

4. **Type Conversion**:
   - String value from database is converted to requested type
   - Result is cached for future use

5. **Return**: Typed value is returned to the caller

### 3. Setting Update Flow

When a setting value is updated:

1. **Request**: Controller calls `_settingsService.UpdateSetting<T>("SettingKey", newValue, scope, scopeId)`

2. **Validation**:
   - Value is validated against the setting's data type
   - If invalid, the update is rejected

3. **Type Conversion**:
   - Typed value is converted to string for storage

4. **Database Update**:
   - If a setting value exists for the scope, it's updated
   - If not, a new setting value is created

5. **Cache Invalidation**:
   - Cache for the updated setting is invalidated
   - Next request will fetch the new value from database

### 4. Admin Interface Flow

When accessing the settings admin interface:

1. **Navigation**:
   - User navigates to `/Settings/Global`, `/Settings/Module/{moduleId}`, or `/Settings/UserSettings/{userId}`

2. **Model Preparation**:
   - Controller fetches all relevant settings for the scope
   - Settings are grouped by their groups
   - Available modules and users are fetched for navigation

3. **View Rendering**:
   - Main view renders the navigation and groups
   - Each setting is rendered with appropriate input based on data type
   - Current values are displayed, with indication of custom vs. default

4. **Setting Update**:
   - User changes a setting value and clicks Save
   - AJAX request is sent to `UpdateSetting` action
   - Controller validates and updates the setting
   - UI is refreshed to show the updated value

## Implementation Examples

### Example 1: Using Page Size Setting in a Controller

```csharp
public class MaterialsController : Controller
{
    private readonly ISettingsService _settingsService;
    private readonly RequisitionContext _context;
    
    public MaterialsController(ISettingsService settingsService, RequisitionContext context)
    {
        _settingsService = settingsService;
        _context = context;
    }
    
    public IActionResult Index(int page = 1)
    {
        // Get effective page size with hierarchical resolution
        int pageSize = _settingsService.GetEffectiveSetting<int>(
            "DefaultPageSize",                               // Setting key
            HttpContext.Session.GetString("EmployeePayrollNo"), // User ID
            "Materials"                                      // Module name
        );
        
        // Use pageSize in pagination
        var materials = _context.Materials
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
            
        return View(materials);
    }
}
```

### Example 2: Using File Upload Settings

```csharp
public IActionResult Upload(IFormFile file)
{
    // Get allowed extensions and max file size from settings
    string[] allowedExtensions = _settingsService.GetGlobalSetting<string[]>("AllowedFileExtensions");
    long maxFileSize = _settingsService.GetGlobalSetting<int>("MaxFileSize");
    
    // Validate file
    string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
    if (!allowedExtensions.Contains(ext))
    {
        ModelState.AddModelError("File", "File type not allowed.");
        return View();
    }
    
    if (file.Length > maxFileSize)
    {
        ModelState.AddModelError("File", $"File size exceeds the maximum allowed size ({maxFileSize / 1024 / 1024} MB).");
        return View();
    }
    
    // Process file...
}
```

### Example 3: Using Settings in a View

```csharp
@inject ISettingsService SettingsService

@{
    var currentUser = Context.Session.GetString("EmployeePayrollNo");
    bool showHelpText = SettingsService.GetUserSetting<bool>(currentUser, "ShowHelpText");
}

<div class="form-group">
    <label for="name">Name</label>
    <input type="text" class="form-control" id="name" name="Name">
    
    @if (showHelpText)
    {
        <small class="form-text text-muted">Enter the full name of the material.</small>
    }
</div>
```

## Integration Guide

### 1. Adding a New Setting

1. **Define the Setting Key**:
   - Add a new constant in `SettingConstants.cs`
   ```csharp
   public static class GlobalSettings
   {
       // Existing settings...
       public const string NewSettingKey = "NewSettingKey";
   }
   ```

2. **Add to Seeder**:
   - Add the setting to `SettingsSeeder.cs`
   ```csharp
   CreateSetting(context, generalGroup.Id, SettingConstants.GlobalSettings.NewSettingKey, 
       "New Setting", "Description of the new setting", 
       SettingDataType.String, "default value", true);
   ```

3. **Run Database Migration**:
   - The seeder will create the setting on application startup

### 2. Using Settings in a Controller

1. **Inject the Service**:
   ```csharp
   private readonly ISettingsService _settingsService;
   
   public YourController(ISettingsService settingsService)
   {
       _settingsService = settingsService;
   }
   ```

2. **Get Setting Values**:
   ```csharp
   // Global setting
   var globalValue = _settingsService.GetGlobalSetting<string>("SettingKey");
   
   // Module setting
   var moduleValue = _settingsService.GetModuleSetting<int>("ModuleName", "SettingKey");
   
   // User setting
   var userValue = _settingsService.GetUserSetting<bool>(userId, "SettingKey");
   
   // Effective setting (hierarchical resolution)
   var effectiveValue = _settingsService.GetEffectiveSetting<T>("SettingKey", userId, moduleKey);
   ```

### 3. Using Settings in a View

1. **Inject the Service in _ViewImports.cshtml**:
   ```csharp
   @using MRIV.Services
   @inject ISettingsService SettingsService
   ```

2. **Use in Views**:
   ```csharp
   @{
       var currentUser = Context.Session.GetString("EmployeePayrollNo");
       var settingValue = SettingsService.GetEffectiveSetting<string>(
           "SettingKey", 
           currentUser, 
           "ModuleName"
       );
   }
   ```

### 4. Updating Settings Programmatically

```csharp
// Update a global setting
await _settingsService.UpdateSettingAsync("SettingKey", "new value", SettingScope.Global);

// Update a module setting
await _settingsService.UpdateSettingAsync("SettingKey", 42, SettingScope.Module, "ModuleName");

// Update a user setting
await _settingsService.UpdateSettingAsync("SettingKey", true, SettingScope.User, userId);
```

---

This documentation provides a comprehensive overview of the MRIV Settings System, its components, and how they interact. For specific implementation details, refer to the source code and comments within each file.
