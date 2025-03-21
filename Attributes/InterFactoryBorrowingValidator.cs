using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using MRIV.Models;
using MRIV.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace MRIV.Attributes
{
    /// <summary>
    /// Validates that for Inter-factory Borrowing requisitions, both Issue and Delivery StationCategory are set to 'Factory'
    /// </summary>
    public class InterFactoryBorrowingValidator : ValidationAttribute, IClientModelValidator
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var model = validationContext.ObjectInstance as MaterialRequisitionWizardViewModel;
            
            if (model?.Requisition == null)
                return ValidationResult.Success;

            // Check if this is an Inter-factory Borrowing (TicketId = 0)
            if (model.Requisition.TicketId == 0)
            {
                // Validate that both Issue and Delivery StationCategory are set to 'factory'
                if (model.Requisition.IssueStationCategory != "factory" || 
                    model.Requisition.DeliveryStationCategory != "factory")
                {
                    return new ValidationResult(
                        "For Inter-factory Borrowing, both Issue and Delivery Station Categories must be set to 'Factory'.");
                }
            }

            return ValidationResult.Success;
        }

        public void AddValidation(ClientModelValidationContext context)
        {
            // Add client-side validation attributes
            context.Attributes["data-val"] = "true";
            context.Attributes["data-val-interfactoryborrowing"] = 
                "For Inter-factory Borrowing, both Issue and Delivery Station Categories must be set to 'Factory'.";
        }
    }
}
