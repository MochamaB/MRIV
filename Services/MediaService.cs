using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MRIV.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MRIV.Services
{
    public class MediaService : IMediaService
    {
        private readonly RequisitionContext _context;
        private readonly IWebHostEnvironment _environment;

        public MediaService(RequisitionContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<MediaFile> SaveMediaFileAsync(IFormFile file, string modelType, int modelId, string collection = "default")
        {
            if (file == null || file.Length == 0)
            {
                return null;
            }

            // Create directory if it doesn't exist
            string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", modelType.ToLower());
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Generate unique filename
            string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);
            
            // Save file to disk
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // Create media file record
            var mediaFile = new MediaFile
            {
                FileName = Path.GetFileName(file.FileName),
                MimeType = file.ContentType,
                FilePath = $"uploads/{modelType.ToLower()}/{uniqueFileName}",
                Collection = collection,
                ModelType = modelType,
                ModelId = modelId,
                FileSize = file.Length,
                CreatedAt = DateTime.UtcNow
            };

            _context.MediaFiles.Add(mediaFile);
            await _context.SaveChangesAsync();

            return mediaFile;
        }

        public async Task<List<MediaFile>> GetMediaForModelAsync(string modelType, int modelId, string collection = null)
        {
            var query = _context.MediaFiles
                .Where(m => m.ModelType == modelType && m.ModelId == modelId);

            if (!string.IsNullOrEmpty(collection))
            {
                query = query.Where(m => m.Collection == collection);
            }

            return await query.ToListAsync();
        }

        public async Task<MediaFile> GetFirstMediaForModelAsync(string modelType, int modelId, string collection = null)
        {
            var query = _context.MediaFiles
                .Where(m => m.ModelType == modelType && m.ModelId == modelId);

            if (!string.IsNullOrEmpty(collection))
            {
                query = query.Where(m => m.Collection == collection);
            }

            return await query.FirstOrDefaultAsync();
        }

        public async Task<bool> DeleteMediaFileAsync(int mediaFileId)
        {
            var mediaFile = await _context.MediaFiles.FindAsync(mediaFileId);
            if (mediaFile == null)
            {
                return false;
            }

            // Delete physical file
            string fullPath = Path.Combine(_environment.WebRootPath, mediaFile.FilePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            // Remove database record
            _context.MediaFiles.Remove(mediaFile);
            await _context.SaveChangesAsync();

            return true;
        }

        public string GetDefaultImageUrl()
        {
            // Return the path to the default SVG placeholder
            return "/uploads/defaultmaterialimage.svg";
        }
    }
}
