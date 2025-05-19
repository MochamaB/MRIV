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

     
        [Description("On Hold")]
        OnHold = 6,

        [Description("Pending Dispatch")]
        PendingDispatch = 7,

        [Description("Not Received")]
        PendingReceive = 8,

        [Description("Cancelled")]
        Cancelled = 9,

    }
}