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
                ApprovalStatus.Dispatched => "badge-light",
                ApprovalStatus.Received => "badge-active",
                ApprovalStatus.Approved => "badge-success",
                ApprovalStatus.Completed => "badge-success",
                ApprovalStatus.Forwarded => "badge-info",
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
                MaterialStatus.UnderMaintenance => "badge-info",
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
    }
}
