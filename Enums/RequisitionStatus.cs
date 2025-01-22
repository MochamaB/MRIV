using System.ComponentModel;

namespace MRIV.Enums
{
    public enum RequisitionStatus
    {
        [Description("Pending Approval")]
        PendingApproval = 1,

        [Description("Pending Dispatch")]
        PendingDispatch = 2,

        [Description("Pending Receipt")]
        PendingReceipt = 3,

        [Description("Receiving")]
        Receiving = 4,

        [Description("Completed")]
        Completed = 5
    }
}
