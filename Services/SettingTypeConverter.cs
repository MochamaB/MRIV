using MRIV.Enums;
using System.Text.Json;

namespace MRIV.Services
{
    /// <summary>
    /// Helper class for converting between string values stored in the database and typed values
    /// </summary>
    internal static class SettingTypeConverter
    {
        /// <summary>
        /// Converts a string value to the specified type based on the data type
        /// </summary>
        public static T ConvertFromString<T>(string value, SettingDataType dataType)
        {
            if (string.IsNullOrEmpty(value))
            {
                return default;
            }

            try
            {
                // Special case: if T is string, return the value directly regardless of data type
                if (typeof(T) == typeof(string))
                {
                    return (T)(object)value;
                }

                return dataType switch
                {
                    SettingDataType.String => (T)(object)value,
                    SettingDataType.Int => (T)(object)int.Parse(value),
                    SettingDataType.Decimal => (T)(object)decimal.Parse(value),
                    SettingDataType.Boolean => (T)(object)bool.Parse(value),
                    SettingDataType.DateTime => (T)(object)DateTime.Parse(value),
                    SettingDataType.Json => JsonSerializer.Deserialize<T>(value),
                    SettingDataType.StringArray => (T)(object)value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                       .Select(s => s.Trim())
                                                       .ToArray(),
                    _ => throw new ArgumentException($"Unsupported data type: {dataType}")
                };
            }
            catch (Exception ex)
            {
                throw new FormatException($"Failed to convert value '{value}' to type {typeof(T).Name} for data type {dataType}", ex);
            }
        }

        /// <summary>
        /// Converts a typed value to a string for storage in the database
        /// </summary>
        public static string ConvertToString<T>(T value, SettingDataType dataType)
        {
            if (value == null)
            {
                return null;
            }

            return dataType switch
            {
                SettingDataType.String => value.ToString(),
                SettingDataType.Int => value.ToString(),
                SettingDataType.Decimal => value.ToString(),
                SettingDataType.Boolean => value.ToString().ToLowerInvariant(),
                SettingDataType.DateTime => ((DateTime)(object)value).ToString("o"),
                SettingDataType.Json => JsonSerializer.Serialize(value),
                SettingDataType.StringArray => string.Join(",", (string[])(object)value),
                _ => throw new ArgumentException($"Unsupported data type: {dataType}")
            };
        }

        /// <summary>
        /// Validates if a value is compatible with the specified data type
        /// </summary>
        public static bool IsValidForDataType<T>(T value, SettingDataType dataType)
        {
            try
            {
                // Convert to string and back to validate
                string stringValue = ConvertToString(value, dataType);
                ConvertFromString<T>(stringValue, dataType);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
