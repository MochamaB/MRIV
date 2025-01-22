using System.ComponentModel;
namespace MRIV.Enums
{
    public enum RequisitionItemStatus
    {
        //Pending Approval,Dispatched, Received, Returned
        [Description("Pending Approval")]
        PendingApproval = 1,

        [Description("Dispatched")]
        PendingDispatch = 2,

        [Description("Received")]
        Received = 3,

        [Description("Returned")]
        Returned = 4,
    }
}
