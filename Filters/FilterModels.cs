using MRIV.Enums;

namespace MRIV.Filters
{
    public class FilterModels
    {
        public abstract class BaseQueryFilter
        {
            public int PageNumber { get; set; } = 1;
            public int PageSize { get; set; } = 20;
            public string? SearchTerm { get; set; }
            public string? SortColumn { get; set; }
            public bool SortDescending { get; set; }
        }

        public class MaterialFilter : BaseQueryFilter
        {
            public int StationId { get; set; }
            public int DepartmentId { get; set; }
            public MaterialStatus? Status { get; set; }
            public int? CategoryId { get; set; }
            public int? SubcategoryId { get; set; }
        }

        public class RequisitionFilter : BaseQueryFilter
        {
            public int StationId { get; set; } // Issue station
            public int DepartmentId { get; set; } // Issue department
            public int? DeliveryStationId { get; set; } // Delivery station
            public int? DeliveryDepartmentId { get; set; } // Delivery department
            public RequisitionType? Type { get; set; }
            public string? DispatchType { get; set; }
            public int? VendorId { get; set; } // For vendor dispatch
        }

    }
}
