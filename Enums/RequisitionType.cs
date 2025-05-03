using System.ComponentModel;

namespace MRIV.Enums
{
    public enum RequisitionType
    {
        [Description("New Purchase")]
        NewPurchase = 1,

        [Description("Transfer")]
        Transfer = 2,

        [Description("Inter Factory")]
        InterFactory = 3,

        [Description("Maintenance/Repair")]
        Maintenance = 4,

        [Description("Return")]
        Return = 5,

        [Description("Disposal")]
        Disposal = 6,

        [Description("Loan/Borrow")]
        Loan = 7,

        [Description("Temporary Allocation")]
        TemporaryAllocation = 8,

        [Description("Other")]
        Other = 9
        
    }
}
