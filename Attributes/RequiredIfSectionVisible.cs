using System.ComponentModel.DataAnnotations;

namespace MRIV.Attributes
{
    public class RequiredIfSectionVisibleAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext context)
        {
            var instance = context.ObjectInstance;
            var type = instance.GetType();

            // Get values of dependent properties (issueStationCategory and deliveryStationCategory)
            var issueCategory = type.GetProperty("IssueStationCategory")?.GetValue(instance)?.ToString();
            var deliveryCategory = type.GetProperty("DeliveryStationCategory")?.GetValue(instance)?.ToString();

            // Determine if the section should be visible (same as your JS logic)
            bool shouldBeVisible =
                issueCategory?.ToLower() == "headoffice" &&
                (deliveryCategory?.ToLower() == "factory" ||
                 deliveryCategory?.ToLower() == "region" ||
                 deliveryCategory?.ToLower() == "vendor");

            // If the section is visible, enforce validation
            if (shouldBeVisible && string.IsNullOrEmpty(value?.ToString()))
            {
                return new ValidationResult(ErrorMessage ?? $"{context.DisplayName} is required.");
            }

            return ValidationResult.Success;
        }
    }
}
