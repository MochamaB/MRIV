using MRIV.Enums;

namespace MRIV.ViewModels
{
    public class MaterialDisplayViewModel
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int MaterialCategoryId { get; set; }
        public string MaterialCategoryName { get; set; }
        public string VendorId { get; set; }
        public string VendorName { get; set; }
        public string? CurrentLocationId { get; set; }

        public MaterialStatus? Status { get; set; }
    }
}
