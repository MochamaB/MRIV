using System.ComponentModel;

namespace MRIV.Enums
{
    public enum RequisitionType
    {
        [Description("New Purchase and Transfer")]
        NewPurchase = 1,

        [Description("Transfer")]
        Transfer = 2,

        [Description("Inter Factory Transfer")]
        InterFactory = 3,

        [Description("Send For Maintenance/Repair")]
        Maintenance = 4,

        [Description("Return To ICT")]
        Return = 5,

        [Description("Disposal")]
        Disposal = 6,

        [Description("Loan Transfer")]
        Loan = 7,

        [Description("Temporary Allocation")]
        TemporaryAllocation = 8,

        [Description("Other")]
        Other = 9
        
    }
}
