namespace MRIV.Enums
{
    /// <summary>
    /// Defines the data types that can be stored in settings
    /// </summary>
    public enum SettingDataType
    {
        /// <summary>
        /// String value
        /// </summary>
        String = 0,
        
        /// <summary>
        /// Integer value
        /// </summary>
        Int = 1,
        
        /// <summary>
        /// Decimal value
        /// </summary>
        Decimal = 2,
        
        /// <summary>
        /// Boolean value
        /// </summary>
        Boolean = 3,
        
        /// <summary>
        /// DateTime value
        /// </summary>
        DateTime = 4,
        
        /// <summary>
        /// JSON value for complex types
        /// </summary>
        Json = 5,
        
        /// <summary>
        /// Array of strings
        /// </summary>
        StringArray = 6
    }
}
