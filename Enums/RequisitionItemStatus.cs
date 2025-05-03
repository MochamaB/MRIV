using System.ComponentModel;
namespace MRIV.Enums
{
    public enum RequisitionItemStatus
    {
        //Transfer,Dispatched, Received, Returned
        [Description("Pending Approval")]
        PendingApproval = 1,

        [Description("Dispatched")]
        PendingDispatch = 2,

        [Description("Received")]
        Received = 3,

        [Description("Returned")]
        Returned = 4,
    }
    public enum RequisitionItemCondition
    {
        //Pending Approval,Dispatched, Received, Returned
        [Description("Good Condition")]
        GoodCondition = 1,

        [Description("Minor Damage")]
        MinorDamage = 2,

        [Description("Major Damage")]
        MajorDamage = 3,

        [Description("Faulty")]
        Faulty = 4,

        [Description("Under Maintenance")]
        UnderMaintenance = 5,

        [Description("Lost or Stolen")]
        LostOrStolen = 6,

        [Description("Disposed")]
        Disposed = 7,

    }
}
