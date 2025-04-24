using Microsoft.AspNetCore.Http;
using MRIV.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MRIV.Services
{
    public interface IMediaService
    {
        /// <summary>
        /// Saves a media file uploaded through a form
        /// </summary>
        Task<MediaFile> SaveMediaFileAsync(IFormFile file, string modelType, int modelId, string collection = "default");
        
        /// <summary>
        /// Gets all media files for a specific model
        /// </summary>
        Task<List<MediaFile>> GetMediaForModelAsync(string modelType, int modelId, string collection = null);
        
        /// <summary>
        /// Gets the first media file for a model (typically used for thumbnails/featured images)
        /// </summary>
        Task<MediaFile> GetFirstMediaForModelAsync(string modelType, int modelId, string collection = null);
        
        /// <summary>
        /// Deletes a media file
        /// </summary>
        Task<bool> DeleteMediaFileAsync(int mediaFileId);
        
        /// <summary>
        /// Gets the URL for the default placeholder image
        /// </summary>
        string GetDefaultImageUrl();
    }
}
