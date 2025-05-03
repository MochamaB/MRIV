using System.ComponentModel;
namespace MRIV.Enums
{
    public enum MaterialStatus
    {
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

        [Description("Available")]
        Available = 8,

        [Description("Assigned")]
        Assigned = 9,

        [Description("In Process")]
        InProcess = 10,

    }
}
