using MRIV.Constants;
using MRIV.Enums;
using MRIV.Models;

namespace MRIV.Data
{
    /// <summary>
    /// Seeds default settings into the database
    /// </summary>
    public static class SettingsSeeder
    {
        public static void SeedSettings(RequisitionContext context)
        {
            // Only seed if no settings exist yet
            if (context.SettingGroups.Any())
            {
                return;
            }

            // Create setting groups
            var generalGroup = CreateSettingGroup(context, "General", "General application settings", 1);
            var uiGroup = CreateSettingGroup(context, "User Interface", "UI and display settings", 2);
            var fileGroup = CreateSettingGroup(context, "File Uploads", "File upload settings and restrictions", 3);
            var notificationGroup = CreateSettingGroup(context, "Notifications", "Notification and alert settings", 4);
            var materialsGroup = CreateSettingGroup(context, "Materials", "Settings for the Materials module", 5);
            var requisitionsGroup = CreateSettingGroup(context, "Requisitions", "Settings for the Requisitions module", 6);
            var approvalsGroup = CreateSettingGroup(context, "Approvals", "Settings for the Approvals module", 7);

            // Save groups to get IDs
            context.SaveChanges();

            // Create global settings
            CreateSetting(context, generalGroup.Id, SettingConstants.GlobalSettings.DefaultPageSize, 
                "Default Page Size", "Default number of items to display per page", 
                SettingDataType.Int, SettingConstants.DefaultValues.DefaultPageSizeValue, true);
            
            CreateSetting(context, generalGroup.Id, SettingConstants.GlobalSettings.MaxPageSize, 
                "Maximum Page Size", "Maximum number of items that can be displayed per page", 
                SettingDataType.Int, SettingConstants.DefaultValues.MaxPageSizeValue, false);
            
            CreateSetting(context, fileGroup.Id, SettingConstants.GlobalSettings.AllowedFileExtensions, 
                "Allowed File Extensions", "File extensions that are allowed to be uploaded", 
                SettingDataType.StringArray, SettingConstants.DefaultValues.AllowedFileExtensionsValue, false);
            
            CreateSetting(context, fileGroup.Id, SettingConstants.GlobalSettings.MaxFileSize, 
                "Maximum File Size", "Maximum size of uploaded files in bytes", 
                SettingDataType.Int, SettingConstants.DefaultValues.MaxFileSizeValue, false);
            
            CreateSetting(context, notificationGroup.Id, SettingConstants.GlobalSettings.EmailNotificationsEnabled, 
                "Email Notifications Enabled", "Whether email notifications are enabled system-wide", 
                SettingDataType.Boolean, "true", false);
            
            CreateSetting(context, notificationGroup.Id, SettingConstants.GlobalSettings.NotificationRefreshInterval, 
                "Notification Refresh Interval", "How often to check for new notifications (in milliseconds)", 
                SettingDataType.Int, SettingConstants.DefaultValues.NotificationRefreshIntervalValue, false);
            
            CreateSetting(context, uiGroup.Id, SettingConstants.GlobalSettings.DefaultTheme, 
                "Default Theme", "Default theme for the application", 
                SettingDataType.String, SettingConstants.DefaultValues.DefaultThemeValue, true);
            
            CreateSetting(context, uiGroup.Id, SettingConstants.GlobalSettings.ShowHelpText, 
                "Show Help Text", "Whether to show help text for form fields", 
                SettingDataType.Boolean, "true", true);

            // Create module settings
            CreateSetting(context, materialsGroup.Id, SettingConstants.ModuleSettings.Materials.PageSize, 
                "Materials Page Size", "Number of materials to display per page", 
                SettingDataType.Int, SettingConstants.DefaultValues.DefaultPageSizeValue, true, "Materials");
            
            CreateSetting(context, materialsGroup.Id, SettingConstants.ModuleSettings.Materials.DefaultSortField, 
                "Default Sort Field", "Default field to sort materials by", 
                SettingDataType.String, "Name", true, "Materials");
            
            CreateSetting(context, materialsGroup.Id, SettingConstants.ModuleSettings.Materials.DefaultSortDirection, 
                "Default Sort Direction", "Default direction to sort materials", 
                SettingDataType.String, "asc", true, "Materials");
            
            CreateSetting(context, materialsGroup.Id, SettingConstants.ModuleSettings.Materials.RequireImageUpload, 
                "Require Image Upload", "Whether an image is required when creating a material", 
                SettingDataType.Boolean, "false", false, "Materials");

            CreateSetting(context, requisitionsGroup.Id, SettingConstants.ModuleSettings.Requisitions.PageSize, 
                "Requisitions Page Size", "Number of requisitions to display per page", 
                SettingDataType.Int, SettingConstants.DefaultValues.DefaultPageSizeValue, true, "Requisitions");
            
            CreateSetting(context, requisitionsGroup.Id, SettingConstants.ModuleSettings.Requisitions.DefaultSortField, 
                "Default Sort Field", "Default field to sort requisitions by", 
                SettingDataType.String, "CreatedAt", true, "Requisitions");
            
            CreateSetting(context, requisitionsGroup.Id, SettingConstants.ModuleSettings.Requisitions.DefaultSortDirection, 
                "Default Sort Direction", "Default direction to sort requisitions", 
                SettingDataType.String, "desc", true, "Requisitions");
            
            CreateSetting(context, requisitionsGroup.Id, SettingConstants.ModuleSettings.Requisitions.AutoNotifyOnApproval, 
                "Auto-Notify on Approval", "Whether to automatically send notifications when a requisition is approved", 
                SettingDataType.Boolean, "true", false, "Requisitions");

            CreateSetting(context, approvalsGroup.Id, SettingConstants.ModuleSettings.Approvals.PageSize, 
                "Approvals Page Size", "Number of approvals to display per page", 
                SettingDataType.Int, SettingConstants.DefaultValues.DefaultPageSizeValue, true, "Approvals");
            
            CreateSetting(context, approvalsGroup.Id, SettingConstants.ModuleSettings.Approvals.DefaultSortField, 
                "Default Sort Field", "Default field to sort approvals by", 
                SettingDataType.String, "CreatedAt", true, "Approvals");
            
            CreateSetting(context, approvalsGroup.Id, SettingConstants.ModuleSettings.Approvals.DefaultSortDirection, 
                "Default Sort Direction", "Default direction to sort approvals", 
                SettingDataType.String, "desc", true, "Approvals");

            // Save all settings
            context.SaveChanges();
        }

        private static SettingGroup CreateSettingGroup(RequisitionContext context, string name, string description, int displayOrder, int? parentGroupId = null)
        {
            var group = new SettingGroup
            {
                Name = name,
                Description = description,
                DisplayOrder = displayOrder,
                ParentGroupId = parentGroupId
            };

            context.SettingGroups.Add(group);
            return group;
        }

        private static SettingDefinition CreateSetting(
            RequisitionContext context, 
            int groupId, 
            string key, 
            string name, 
            string description, 
            SettingDataType dataType, 
            string defaultValue, 
            bool isUserConfigurable,
            string moduleName = null)
        {
            var setting = new SettingDefinition
            {
                Key = key,
                Name = name,
                Description = description,
                DataType = dataType,
                DefaultValue = defaultValue,
                IsUserConfigurable = isUserConfigurable,
                ModuleName = moduleName,
                GroupId = groupId,
                DisplayOrder = 0
            };

            context.SettingDefinitions.Add(setting);
            return setting;
        }
    }
}
