using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MRIV.Attributes;
using MRIV.Enums;
using MRIV.Models;
using MRIV.Services;
using MRIV.ViewModels;
using System.Text.Json;

namespace MRIV.Controllers
{
    [CustomAuthorize]
    public class SettingsController : Controller
    {
        private readonly ISettingsService _settingsService;
        private readonly RequisitionContext _context;
        private readonly ILogger<SettingsController> _logger;
        private readonly IEmployeeService _employeeService;

        public SettingsController(
            ISettingsService settingsService,
            RequisitionContext context,
            ILogger<SettingsController> logger,
            IEmployeeService employeeService)
        {
            _settingsService = settingsService;
            _context = context;
            _logger = logger;
            _employeeService = employeeService;
        }

        #region Global Settings

        /// <summary>
        /// Display global settings
        /// </summary>
        public async Task<IActionResult> Global()
        {
            var model = await PrepareSettingsViewModel(SettingScope.Global);
            model.ScopeDisplayName = "Global Settings";
            return View("Index", model);
        }

        #endregion

        #region Module Settings

        /// <summary>
        /// Display settings for a specific module
        /// </summary>
        public async Task<IActionResult> Module(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                // If no module specified, redirect to the first available module
                var modules = await GetAvailableModules();
                if (modules.Any())
                {
                    return RedirectToAction("Module", new { id = modules.First() });
                }
                
                // If no modules available, show global settings
                return RedirectToAction("Global");
            }

            var model = await PrepareSettingsViewModel(SettingScope.Module, id);
            model.ScopeDisplayName = $"{id} Module Settings";
            return View("Index", model);
        }

        #endregion

        #region User Settings

        /// <summary>
        /// Display settings for a specific user
        /// </summary>
        public async Task<IActionResult> UserSettings(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                // If no user specified, redirect to the current user's settings
                var currentUser = HttpContext.Session.GetString("EmployeePayrollNo");
                if (!string.IsNullOrEmpty(currentUser))
                {
                    return RedirectToAction("UserSettings", new { id = currentUser });
                }
                
                // If no current user, show global settings
                return RedirectToAction("Global");
            }

            var model = await PrepareSettingsViewModel(SettingScope.User, id);
            
            // Get user display name
            var employee = await _employeeService.GetEmployeeByPayrollAsync(id);
            model.ScopeDisplayName = $"User Settings: {employee?.Fullname ?? id}";
            
            return View("Index", model);
        }

        /// <summary>
        /// Display settings for the current user
        /// </summary>
        public IActionResult MySettings()
        {
            var currentUser = HttpContext.Session.GetString("EmployeePayrollNo");
            if (string.IsNullOrEmpty(currentUser))
            {
                return RedirectToAction("Global");
            }
            
            return RedirectToAction("UserSettings", new { id = currentUser });
        }

        #endregion

        #region AJAX Actions

        /// <summary>
        /// Update a setting value
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateSetting(UpdateSettingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Get the setting definition to determine its data type
                var definition = await _context.SettingDefinitions
                    .FirstOrDefaultAsync(sd => sd.Key == model.Key);
                
                if (definition == null)
                {
                    return NotFound($"Setting with key '{model.Key}' not found.");
                }

                bool success;
                
                if (model.ResetToDefault)
                {
                    // Delete the setting value to revert to default
                    var settingValue = await _context.SettingValues
                        .FirstOrDefaultAsync(sv => 
                            sv.SettingDefinition.Key == model.Key && 
                            sv.Scope == model.Scope && 
                            sv.ScopeId == model.ScopeId);
                    
                    if (settingValue != null)
                    {
                        _context.SettingValues.Remove(settingValue);
                        await _context.SaveChangesAsync();
                        
                        // Invalidate cache
                        await _settingsService.InvalidateCacheAsync(model.Key, model.Scope, model.ScopeId);
                        
                        success = true;
                    }
                    else
                    {
                        // Nothing to reset
                        success = true;
                    }
                }
                else
                {
                    // Update the setting value based on its data type
                    switch (definition.DataType)
                    {
                        case SettingDataType.String:
                            success = await _settingsService.UpdateSettingAsync(model.Key, model.Value, model.Scope, model.ScopeId);
                            break;
                        case SettingDataType.Int:
                            if (int.TryParse(model.Value, out int intValue))
                            {
                                success = await _settingsService.UpdateSettingAsync(model.Key, intValue, model.Scope, model.ScopeId);
                            }
                            else
                            {
                                ModelState.AddModelError("Value", "Invalid integer value.");
                                return BadRequest(ModelState);
                            }
                            break;
                        case SettingDataType.Decimal:
                            if (decimal.TryParse(model.Value, out decimal decimalValue))
                            {
                                success = await _settingsService.UpdateSettingAsync(model.Key, decimalValue, model.Scope, model.ScopeId);
                            }
                            else
                            {
                                ModelState.AddModelError("Value", "Invalid decimal value.");
                                return BadRequest(ModelState);
                            }
                            break;
                        case SettingDataType.Boolean:
                            if (bool.TryParse(model.Value, out bool boolValue))
                            {
                                success = await _settingsService.UpdateSettingAsync(model.Key, boolValue, model.Scope, model.ScopeId);
                            }
                            else
                            {
                                ModelState.AddModelError("Value", "Invalid boolean value.");
                                return BadRequest(ModelState);
                            }
                            break;
                        case SettingDataType.DateTime:
                            if (DateTime.TryParse(model.Value, out DateTime dateValue))
                            {
                                success = await _settingsService.UpdateSettingAsync(model.Key, dateValue, model.Scope, model.ScopeId);
                            }
                            else
                            {
                                ModelState.AddModelError("Value", "Invalid date value.");
                                return BadRequest(ModelState);
                            }
                            break;
                        case SettingDataType.StringArray:
                            var stringArray = model.Value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim())
                                .ToArray();
                            success = await _settingsService.UpdateSettingAsync(model.Key, stringArray, model.Scope, model.ScopeId);
                            break;
                        case SettingDataType.Json:
                            try
                            {
                                // Validate JSON
                                JsonDocument.Parse(model.Value);
                                success = await _settingsService.UpdateSettingAsync(model.Key, model.Value, model.Scope, model.ScopeId);
                            }
                            catch (JsonException)
                            {
                                ModelState.AddModelError("Value", "Invalid JSON value.");
                                return BadRequest(ModelState);
                            }
                            break;
                        default:
                            ModelState.AddModelError("", $"Unsupported data type: {definition.DataType}");
                            return BadRequest(ModelState);
                    }
                }

                if (success)
                {
                    return Json(new { success = true });
                }
                else
                {
                    ModelState.AddModelError("", "Failed to update setting.");
                    return BadRequest(ModelState);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating setting {Key}", model.Key);
                ModelState.AddModelError("", $"Error: {ex.Message}");
                return BadRequest(ModelState);
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Prepares the settings view model for the specified scope
        /// </summary>
        private async Task<SettingsViewModel> PrepareSettingsViewModel(SettingScope scope, string scopeId = null)
        {
            var model = new SettingsViewModel
            {
                Scope = scope,
                ScopeId = scopeId,
                AvailableModules = await GetAvailableModules(),
                AvailableUsers = await GetAvailableUsers()
            };

            // Get all setting definitions
            var definitions = await _settingsService.GetAllSettingDefinitionsAsync();
            
            // Filter definitions based on scope
            if (scope == SettingScope.Module && !string.IsNullOrEmpty(scopeId))
            {
                // For module scope, include both module-specific settings and global settings that apply to this module
                definitions = definitions.Where(d => 
                    d.ModuleName == scopeId || 
                    (string.IsNullOrEmpty(d.ModuleName) && d.IsUserConfigurable)).ToList();
            }
            else if (scope == SettingScope.User)
            {
                // For user scope, only include user-configurable settings
                definitions = definitions.Where(d => d.IsUserConfigurable).ToList();
            }

            // Group settings by their groups
            var groups = definitions
                .GroupBy(d => d.Group)
                .Select(g => new SettingGroupViewModel
                {
                    Id = g.Key.Id,
                    Name = g.Key.Name,
                    Description = g.Key.Description,
                    Settings = g.Select(d => CreateSettingItemViewModel(d, scope, scopeId)).ToList()
                })
                .OrderBy(g => g.Name)
                .ToList();

            model.Groups = groups;
            
            return model;
        }

        /// <summary>
        /// Creates a view model for a setting item
        /// </summary>
        private SettingItemViewModel CreateSettingItemViewModel(SettingDefinition definition, SettingScope scope, string scopeId)
        {
            // Get the current value for this scope
            var value = _settingsService.GetSetting<string>(definition.Key, scope, scopeId);
            
            // Check if this setting has a custom value for this scope
            var hasCustomValue = _context.SettingValues
                .Any(sv => sv.SettingDefinition.Key == definition.Key && sv.Scope == scope && sv.ScopeId == scopeId);

            return new SettingItemViewModel
            {
                Id = definition.Id,
                Key = definition.Key,
                Name = definition.Name,
                Description = definition.Description,
                DataType = definition.DataType,
                Value = value ?? definition.DefaultValue, // Ensure we have a value
                DefaultValue = definition.DefaultValue,
                IsUserConfigurable = definition.IsUserConfigurable,
                ModuleName = definition.ModuleName,
                HasCustomValue = hasCustomValue,
                ValidationRules = definition.ValidationRules
            };
        }

        /// <summary>
        /// Gets the list of available modules
        /// </summary>
        private async Task<List<string>> GetAvailableModules()
        {
            // Get distinct module names from setting definitions
            return await _context.SettingDefinitions
                .Where(sd => !string.IsNullOrEmpty(sd.ModuleName))
                .Select(sd => sd.ModuleName)
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync();
        }

        /// <summary>
        /// Gets the list of available users
        /// </summary>
        private async Task<List<UserViewModel>> GetAvailableUsers()
        {
            // Get users who have custom settings
            var userIds = await _context.SettingValues
                .Where(sv => sv.Scope == SettingScope.User && !string.IsNullOrEmpty(sv.ScopeId))
                .Select(sv => sv.ScopeId)
                .Distinct()
                .ToListAsync();

            var result = new List<UserViewModel>();
            
            // Add current user if not already in the list
            var currentUser = HttpContext.Session.GetString("EmployeePayrollNo");
            if (!string.IsNullOrEmpty(currentUser) && !userIds.Contains(currentUser))
            {
                userIds.Add(currentUser);
            }

            // Get user names
            foreach (var userId in userIds)
            {
                var employee = await _employeeService.GetEmployeeByPayrollAsync(userId);
                result.Add(new UserViewModel
                {
                    Id = userId,
                    Name = employee?.Fullname ?? userId
                });
            }

            return result.OrderBy(u => u.Name).ToList();
        }

        #endregion
    }
}
