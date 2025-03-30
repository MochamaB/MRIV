using System.ComponentModel.DataAnnotations;
using MRIV.Enums;

namespace MRIV.Attributes
{
    /// <summary>
    /// Validation attribute that makes a property required when the approval action is Reject
    /// </summary>
    public class RequiredIfRejectedAttribute : ValidationAttribute
    {
        private readonly string _actionProperty;

        public RequiredIfRejectedAttribute(string actionProperty)
        {
            _actionProperty = actionProperty;
        }

        protected override ValidationResult IsValid(object value, ValidationContext context)
        {
            var instance = context.ObjectInstance;
            var type = instance.GetType();

            // Get the action property's value
            var actionPropertyInfo = type.GetProperty(_actionProperty);
            if (actionPropertyInfo == null)
                return ValidationResult.Success;

            var actionValue = actionPropertyInfo.GetValue(instance)?.ToString();

            // Skip validation if action is null or empty
            if (string.IsNullOrEmpty(actionValue))
                return ValidationResult.Success;

            // Check if the action represents a rejection
            bool isRejection = false;

            // Try to parse as integer (enum value)
            if (int.TryParse(actionValue, out int statusValue))
            {
                isRejection = statusValue == (int)ApprovalStatus.Rejected;
            }

            // Skip validation if not a rejection
            if (!isRejection)
                return ValidationResult.Success;

            // Validate only when the action is a rejection
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                var errorMessage = ErrorMessage ??
                    $"{context.DisplayName} is required when rejecting an approval";
                return new ValidationResult(errorMessage);
            }

            return ValidationResult.Success;
        }
    }
}