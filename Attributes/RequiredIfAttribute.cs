using System.ComponentModel.DataAnnotations;

namespace MRIV.Attributes
{
    public class RequiredIfAttribute : ValidationAttribute
    {
        private readonly string _dependentProperty;
        private readonly object _targetValue;

        public RequiredIfAttribute(string dependentProperty, object targetValue)
        {
            _dependentProperty = dependentProperty;
            _targetValue = targetValue;
        }

        protected override ValidationResult IsValid(object value, ValidationContext context)
        {
            var instance = context.ObjectInstance;
            var type = instance.GetType();

            // Get the dependent property's value
            var dependentPropertyInfo = type.GetProperty(_dependentProperty);
            if (dependentPropertyInfo == null)
                return ValidationResult.Success;

            var propertyValue = dependentPropertyInfo.GetValue(instance, null);

            // 🚀 Get the Display Name of the dependent property
            var displayAttribute = dependentPropertyInfo
                .GetCustomAttributes(typeof(DisplayAttribute), false)
                .FirstOrDefault() as DisplayAttribute;

            // Use Display Name if available, else fall back to property name
            var dependentDisplayName = displayAttribute?.Name ?? _dependentProperty;

            // Skip validation if conditions aren't met
            if (propertyValue == null || !propertyValue.Equals(_targetValue))
                return ValidationResult.Success;

            // Validate only when the condition is met
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                var errorMessage = ErrorMessage ??
                    $"{context.DisplayName} is required when {dependentDisplayName} is {_targetValue}";
                return new ValidationResult(errorMessage);
            }

            return ValidationResult.Success;
        }
    }


}

