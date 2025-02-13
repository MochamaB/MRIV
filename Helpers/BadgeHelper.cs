namespace MRIV.Helpers
{
    public class BadgeHelper
    {
        public static string GetApprovalsBadgeClass(string status)
        {
            return status switch
            {
                "Pending Approval" => "badge-warning",
                "Not Started" => "badge-information",
                "Approval Rejected" => "badge-error",
                "Dispatched" => "badge-light",
                "Received" => "badge-active",
                _ => "bg-primary" // Default class
            };
        }
    }
}
