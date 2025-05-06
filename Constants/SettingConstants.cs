namespace MRIV.Constants
{
    /// <summary>
    /// Constants for setting keys and default values
    /// </summary>
    public static class SettingConstants
    {
        // Global setting keys
        public static class GlobalSettings
        {
            // Pagination
            public const string DefaultPageSize = "DefaultPageSize";
            public const string MaxPageSize = "MaxPageSize";
            
            // File uploads
            public const string AllowedFileExtensions = "AllowedFileExtensions";
            public const string MaxFileSize = "MaxFileSize";
            
            // Notifications
            public const string EmailNotificationsEnabled = "EmailNotificationsEnabled";
            public const string NotificationRefreshInterval = "NotificationRefreshInterval";
            
            // UI
            public const string DefaultTheme = "DefaultTheme";
            public const string ShowHelpText = "ShowHelpText";
        }
        
        // Module setting keys
        public static class ModuleSettings
        {
            // Materials module
            public static class Materials
            {
                public const string PageSize = "Materials.PageSize";
                public const string DefaultSortField = "Materials.DefaultSortField";
                public const string DefaultSortDirection = "Materials.DefaultSortDirection";
                public const string RequireImageUpload = "Materials.RequireImageUpload";
            }
            
            // Requisitions module
            public static class Requisitions
            {
                public const string PageSize = "Requisitions.PageSize";
                public const string DefaultSortField = "Requisitions.DefaultSortField";
                public const string DefaultSortDirection = "Requisitions.DefaultSortDirection";
                public const string AutoNotifyOnApproval = "Requisitions.AutoNotifyOnApproval";
            }
            
            // Approvals module
            public static class Approvals
            {
                public const string PageSize = "Approvals.PageSize";
                public const string DefaultSortField = "Approvals.DefaultSortField";
                public const string DefaultSortDirection = "Approvals.DefaultSortDirection";
            }
        }
        
        // User setting keys
        public static class UserSettings
        {
            public const string PageSize = "User.PageSize";
            public const string Theme = "User.Theme";
            public const string EmailNotifications = "User.EmailNotifications";
            public const string InAppNotifications = "User.InAppNotifications";
            public const string DashboardLayout = "User.DashboardLayout";
        }
        
        // Default values
        public static class DefaultValues
        {
            public const string DefaultPageSizeValue = "10";
            public const string MaxPageSizeValue = "100";
            public const string AllowedFileExtensionsValue = ".jpg,.jpeg,.png,.pdf,.docx,.xlsx";
            public const string MaxFileSizeValue = "10485760"; // 10MB
            public const string DefaultThemeValue = "light";
            public const string NotificationRefreshIntervalValue = "60000"; // 1 minute
        }
    }
}
