using System.ComponentModel;

namespace MRIV.Enums
{
    public enum RequisitionStatus
    {
        [Description("Not Started")]
        NotStarted = 1,

        [Description("In Dispatch")]
        PendingDispatch = 2,

        [Description("Receiving")]
        PendingReceipt = 3,

        [Description("Cancelled")]
        Cancelled = 4,

        [Description("Completed")]
        Completed = 5
    }
}
