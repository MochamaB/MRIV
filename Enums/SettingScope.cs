namespace MRIV.Enums
{
    /// <summary>
    /// Defines the scope level at which a setting applies
    /// </summary>
    public enum SettingScope
    {
        /// <summary>
        /// Setting applies globally to the entire application
        /// </summary>
        Global = 0,
        
        /// <summary>
        /// Setting applies to a specific module/controller
        /// </summary>
        Module = 1,
        
        /// <summary>
        /// Setting applies to a specific user
        /// </summary>
        User = 2
    }
}
