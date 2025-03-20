using System.ComponentModel;

namespace MRIV.Enums
{
    public enum ApprovalStatus
    {
        [Description("Not Started")]
        NotStarted = 0,

        [Description("Pending Approval")]
        PendingApproval = 1,

        [Description("Approved")]
        Approved = 2,

        [Description("Rejected")]
        Rejected = 3,

        [Description("Dispatched")]
        Dispatched = 4,

        [Description("Received")]
        Received = 5,

        [Description("Completed")]
        Completed = 6,

        [Description("Forwarded")]
        Forwarded = 7,

        [Description("On Hold")]
        OnHold = 8
    }
}