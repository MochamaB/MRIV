
using System.ComponentModel;

namespace MRIV.Enums
{
    // Changed class to enum to fix CS1519 and IDE1007 errors
    public enum MaterialConditionStatus
    {
        [Description("Good Condition")]
        GoodCondition = 1,


        [Description("Minor Damage")]
        MinorDamage = 2,

        [Description("Major Damage")]
        MajorDamage = 3,

        [Description("Faulty")]
        Faulty = 4,

        [Description("Broken")]
        UnderMaintenance = 5,

        [Description("Lost or Stolen")]
        LostOrStolen = 6,

        [Description("Disposed")]
        Disposed = 7,
    }

    public enum ConditionCheckType
    {
        [Description("Initial")]
        Initial = 0,

        [Description("Requisition Transfer")]
        Transfer = 1,

        [Description("At Dispatch")]
        AtDispatch = 2,

        [Description("At Receipt")]
        AtReceipt = 3,

        [Description("Periodic")]
        Periodic = 4,

        [Description("At Disposal")]
        AtDisposal = 5
    }

    public enum FunctionalStatus
    {
        [Description("Fully Functional")]
        FullyFunctional = 0,

        [Description("Partially Functional")]
        PartiallyFunctional = 1,

        [Description("Non Functional")]
        NonFunctional = 2
    }

    public enum CosmeticStatus
    {
        [Description("Excellent")]
        Excellent = 0,

        [Description("Good")]
        Good = 1,

        [Description("Fair")]
        Fair = 2,

        [Description("Poor")]
        Poor = 3
    }
}
