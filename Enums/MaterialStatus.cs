using System.ComponentModel;
namespace MRIV.Enums
{
    public enum MaterialStatus
    {
       
        [Description("Under Maintenance")]
        UnderMaintenance = 1,

        [Description("Lost or Stolen")]
        LostOrStolen = 2,

        [Description("Disposed")]
        Disposed = 3,

        [Description("Available")]
        Available = 4,

        [Description("Assigned")]
        Assigned = 5,

        [Description("In Process")]
        InProcess = 6,

    }
}
