using MRIV.Enums;

namespace MRIV.Helpers
{
    public class BadgeHelper
    {
        public static string GetApprovalsBadgeClass(ApprovalStatus status)
        {
            return status switch
            {
                ApprovalStatus.PendingApproval => "badge-warning",
                ApprovalStatus.NotStarted => "badge-information",
                ApprovalStatus.Rejected => "badge-error",
                ApprovalStatus.Dispatched => "badge-success",
                ApprovalStatus.Received => "badge-active",
                ApprovalStatus.Approved => "badge-success",
                ApprovalStatus.PendingDispatch => "badge-warning",
                ApprovalStatus.PendingReceive => "badge-warning",
                ApprovalStatus.OnHold => "badge-secondary",
                _ => "bg-primary" // Default class
            };
        }

        public static string GetMaterialStatusBadgeClass(MaterialStatus? status)
        {
            return status switch
            {
                MaterialStatus.GoodCondition => "badge-active",
                MaterialStatus.MinorDamage => "badge-warning",
                MaterialStatus.MajorDamage => "badge-danger",
                MaterialStatus.Faulty => "badge-danger",
                MaterialStatus.UnderMaintenance => "badge-information",
                MaterialStatus.LostOrStolen => "badge-dark",
                MaterialStatus.Disposed => "badge-secondary",
                null => "badge-light", // Handle null case (optional)
                _ => "bg-primary" // Default class
            };
        }
        public static string GetRequisitionStatusBadgeClass(RequisitionStatus status)
        {
            return status switch
            {
                RequisitionStatus.NotStarted => "badge-warning",
                RequisitionStatus.PendingDispatch => "badge-information",
                RequisitionStatus.PendingReceipt => "badge-information",
                RequisitionStatus.Cancelled => "badge-danger",
                RequisitionStatus.Completed => "badge-success",
                _ => "badge-secondary"
            };
        }

        public static string GetRequisitionItemStatusBadgeClass(RequisitionItemStatus status)
        {
            return status switch
            {
                RequisitionItemStatus.PendingApproval => "badge-danger",
                RequisitionItemStatus.PendingDispatch => "badge-warning",
                RequisitionItemStatus.Received => "badge-success",
                RequisitionItemStatus.Returned => "badge-information",
                _ => "badge-secondary"
            };
        }
        public static string GetRequisitionItemConditionBadgeClass(RequisitionItemCondition? status)
        {
            return status switch
            {
                RequisitionItemCondition.GoodCondition => "badge-active",
                RequisitionItemCondition.MinorDamage => "badge-warning",
                RequisitionItemCondition.MajorDamage => "badge-danger",
                RequisitionItemCondition.Faulty => "badge-danger",
                RequisitionItemCondition.UnderMaintenance => "badge-information",
                RequisitionItemCondition.LostOrStolen => "badge-dark",
                RequisitionItemCondition.Disposed => "badge-secondary",
                null => "badge-light", // Handle null case (optional)
                _ => "bg-primary" // Default class
            };
        }
    }
}
