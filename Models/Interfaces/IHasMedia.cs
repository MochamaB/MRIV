using System.Collections.Generic;

namespace MRIV.Models.Interfaces
{
    public interface IHasMedia
    {
        ICollection<MediaFile> Media { get; set; }
    }
}