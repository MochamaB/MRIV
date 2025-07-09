using MRIV.Models;

namespace MRIV.Data
{// Data/NotificationTemplateSeeder.cs
    public static class NotificationTemplateSeeder
    {
        public static void SeedNotificationTemplates(RequisitionContext context)
        {
            if (context.NotificationTemplates.Any())
                return; // Already seeded

            var templates = new List<NotificationTemplate>
        {
            // Requisition Creation
            new NotificationTemplate
            {
                Name = "RequisitionCreated",
                TitleTemplate = "New Material Requisition #{RequisitionId}",
                MessageTemplate = "A new requisition has been created by {Creator} for {ItemCount} items.",
                NotificationType = "Requisition"
            },
            
            // Approval Workflow
            new NotificationTemplate
            {
                Name = "ApprovalRequested",
                TitleTemplate = "Approval Required: Material Requisition #{RequisitionId}",
                MessageTemplate = "Your approval is required for a requisition created by {Creator}.",
                NotificationType = "Approval"
            },

            new NotificationTemplate
            {
                Name = "RequisitionApproved",
                TitleTemplate = "Requisition #{RequisitionId} Approved",
                MessageTemplate = "Your requisition has been approved by {Approver} and is now at step {NextStep}.",
                NotificationType = "Approval"
            },

            new NotificationTemplate
            {
                Name = "RequisitionRejected",
                TitleTemplate = "Requisition #{RequisitionId} Rejected",
                MessageTemplate = "Your requisition was rejected by {Approver}. Reason: {Reason}",
                NotificationType = "Approval"
            },
            
            new NotificationTemplate
            {
                Name = "RequisitionOnHold",
                TitleTemplate = "Requisition #{RequisitionId} On Hold",
                MessageTemplate = "Your requisition has been put on hold by {Approver}. Reason: {Reason}",
                NotificationType = "Approval"
            },
            
            new NotificationTemplate
            {
                Name = "RequisitionCancelled",
                TitleTemplate = "Requisition #{RequisitionId} Cancelled",
                MessageTemplate = "Your requisition has been cancelled. Reason: {Reason}",
                NotificationType = "Approval"
            },
            
            new NotificationTemplate
            {
                Name = "RequisitionCompleted",
                TitleTemplate = "Requisition #{RequisitionId} Completed",
                MessageTemplate = "Your requisition has been fully completed and all items have been received at {DeliveryStation}.",
                NotificationType = "Requisition"
            },

            new NotificationTemplate
            {
                Name = "MaterialAssigned",
                TitleTemplate = "Material Assigned: {MaterialName}",
                MessageTemplate = "A material ({MaterialName}, Code: {MaterialCode}) has been assigned to you through Requisition #{RequisitionId}. Location: {DeliveryStation}.",
                NotificationType = "Material"
            },
            
            new NotificationTemplate
            {
                Name = "MaterialConditionRequired",
                TitleTemplate = "Record Material Conditions: Requisition #{RequisitionId}",
                MessageTemplate = "Please record the condition of materials for requisition #{RequisitionId} before completing the {Step} step.",
                NotificationType = "Material"
            },
            
            // Dispatch Process
            new NotificationTemplate
            {
                Name = "ReadyForDispatch",
                TitleTemplate = "Requisition #{RequisitionId} Ready for Dispatch",
                MessageTemplate = "A requisition has been approved and is ready for dispatch to {DeliveryStation}.",
                NotificationType = "Dispatch"
            },
            
            // Receipt Process
            new NotificationTemplate
            {
                Name = "ItemsDispatched",
                TitleTemplate = "Items Dispatched: Requisition #{RequisitionId}",
                MessageTemplate = "Items from your requisition have been dispatched by {Dispatcher} and are on the way to {DeliveryStation}.",
                NotificationType = "Dispatch"
            },

            new NotificationTemplate
            {
                Name = "ItemsReceived",
                TitleTemplate = "Items Received: Requisition #{RequisitionId}",
                MessageTemplate = "Items from your requisition have been received by {Receiver} at {DeliveryStation}.",
                NotificationType = "Receipt"
            },
            
            new NotificationTemplate
            {
                Name = "NextApproverNotification",
                TitleTemplate = "Action Required: Requisition #{RequisitionId}",
                MessageTemplate = "The requisition #{RequisitionId} is now at your step ({StepName}) and requires your action.",
                NotificationType = "Approval"
            }
        };

            context.NotificationTemplates.AddRange(templates);
            context.SaveChanges();
        }
    }
}
