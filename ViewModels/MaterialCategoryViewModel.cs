using Microsoft.AspNetCore.Http;
using MRIV.Models;
using System.Collections.Generic;

namespace MRIV.ViewModels
{
    public class MaterialCategoryViewModel
    {
        public MaterialCategory Category { get; set; } = new MaterialCategory();
        
        // For file upload
        public IFormFile? ImageFile { get; set; }
        
        // For displaying existing media
        public MediaFile? ExistingImage { get; set; }
    }
}
