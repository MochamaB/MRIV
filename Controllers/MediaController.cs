using Microsoft.AspNetCore.Mvc;
using MRIV.Attributes;
using MRIV.Services;
using MRIV.ViewModels;
using MRIV.Models;
using Microsoft.EntityFrameworkCore;

namespace MRIV.Controllers
{
    [CustomAuthorize]
    public class MediaController : Controller
    {
        private readonly IMediaService _mediaService;
        private readonly RequisitionContext _context;

        public MediaController(IMediaService mediaService, RequisitionContext context)
        {
            _mediaService = mediaService;
            _context = context;
        }

        public async Task<IActionResult> Index(string folder = "all", string view = "grid", string search = "", 
            int page = 1, string sortBy = "name", string sortOrder = "asc")
        {
            var viewModel = new MediaFileManagerViewModel
            {
                CurrentFolder = folder,
                ViewMode = view,
                SearchQuery = search,
                CurrentPage = page,
                SortBy = sortBy,
                SortOrder = sortOrder,
                PageSize = 24
            };

            // Get folders
            viewModel.Folders = await GetFoldersAsync();

            // Get files with pagination and filtering
            var filesQuery = await GetFilesQueryAsync(folder, search, sortBy, sortOrder);
            
            viewModel.TotalFiles = await filesQuery.CountAsync();
            viewModel.TotalPages = (int)Math.Ceiling((double)viewModel.TotalFiles / viewModel.PageSize);

            var files = await filesQuery
                .Skip((page - 1) * viewModel.PageSize)
                .Take(viewModel.PageSize)
                .ToListAsync();

            viewModel.Files = await MapToMediaFileItemsAsync(files);
            viewModel.TotalSize = await GetTotalSizeAsync(folder);

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetFolderContents(string folder = "all", string view = "grid", 
            string search = "", int page = 1, string sortBy = "name", string sortOrder = "asc")
        {
            var filesQuery = await GetFilesQueryAsync(folder, search, sortBy, sortOrder);
            
            var totalFiles = await filesQuery.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalFiles / 24);

            var files = await filesQuery
                .Skip((page - 1) * 24)
                .Take(24)
                .ToListAsync();

            var fileItems = await MapToMediaFileItemsAsync(files);

            return Json(new
            {
                files = fileItems,
                totalFiles = totalFiles,
                totalPages = totalPages,
                currentPage = page
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetMediaDetails(int id)
        {
            var mediaFile = await _context.MediaFiles.FindAsync(id);
            if (mediaFile == null)
            {
                return NotFound();
            }

            var viewModel = await MapToMediaFileDetailsAsync(mediaFile);
            return PartialView("_MediaDetails", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _mediaService.DeleteMediaFileAsync(id);
            if (success)
            {
                return Json(new { success = true, message = "File deleted successfully" });
            }
            return Json(new { success = false, message = "Failed to delete file" });
        }

        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            var mediaFile = await _context.MediaFiles.FindAsync(id);
            if (mediaFile == null)
            {
                return NotFound();
            }

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", mediaFile.FilePath);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("File not found on disk");
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, mediaFile.MimeType, mediaFile.FileName);
        }

        [HttpGet]
        public async Task<IActionResult> Search(string query, string folder = "all")
        {
            var filesQuery = await GetFilesQueryAsync(folder, query, "name", "asc");
            var files = await filesQuery.Take(50).ToListAsync();
            var fileItems = await MapToMediaFileItemsAsync(files);

            return Json(new { files = fileItems });
        }

        private async Task<List<FolderItem>> GetFoldersAsync()
        {
            var folders = new List<FolderItem>();

            // All Files folder
            var totalFiles = await _context.MediaFiles.CountAsync();
            var totalSize = await _context.MediaFiles.SumAsync(m => m.FileSize);
            
            folders.Add(new FolderItem
            {
                Name = "all",
                DisplayName = "All Files",
                Path = "all",
                FileCount = totalFiles,
                TotalSize = totalSize,
                FormattedSize = FormatFileSize(totalSize),
                Icon = "ri-folder-2-line"
            });

            // Get folders by ModelType
            var modelTypes = await _context.MediaFiles
                .Where(m => !string.IsNullOrEmpty(m.ModelType))
                .GroupBy(m => m.ModelType)
                .Select(g => new { ModelType = g.Key, Count = g.Count(), Size = g.Sum(m => m.FileSize) })
                .ToListAsync();

            foreach (var modelType in modelTypes)
            {
                var displayName = GetFolderDisplayName(modelType.ModelType);
                var icon = GetFolderIcon(modelType.ModelType);

                folders.Add(new FolderItem
                {
                    Name = modelType.ModelType.ToLower(),
                    DisplayName = displayName,
                    Path = modelType.ModelType.ToLower(),
                    FileCount = modelType.Count,
                    TotalSize = modelType.Size,
                    FormattedSize = FormatFileSize(modelType.Size),
                    Icon = icon
                });
            }

            return folders;
        }

        private async Task<IQueryable<MediaFile>> GetFilesQueryAsync(string folder, string search, string sortBy, string sortOrder)
        {
            var query = _context.MediaFiles.AsQueryable();

            // Apply folder filter
            if (!string.IsNullOrEmpty(folder) && folder != "all")
            {
                query = query.Where(m => m.ModelType.ToLower() == folder.ToLower());
            }

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(m => m.FileName.Contains(search) || 
                                       m.MimeType.Contains(search) ||
                                       m.ModelType.Contains(search));
            }

            // Apply sorting
            query = sortBy.ToLower() switch
            {
                "name" => sortOrder == "desc" ? query.OrderByDescending(m => m.FileName) : query.OrderBy(m => m.FileName),
                "date" => sortOrder == "desc" ? query.OrderByDescending(m => m.CreatedAt) : query.OrderBy(m => m.CreatedAt),
                "size" => sortOrder == "desc" ? query.OrderByDescending(m => m.FileSize) : query.OrderBy(m => m.FileSize),
                "type" => sortOrder == "desc" ? query.OrderByDescending(m => m.MimeType) : query.OrderBy(m => m.MimeType),
                _ => query.OrderBy(m => m.FileName)
            };

            return query;
        }

        private async Task<List<MediaFileItem>> MapToMediaFileItemsAsync(List<MediaFile> mediaFiles)
        {
            var items = new List<MediaFileItem>();

            foreach (var file in mediaFiles)
            {
                var item = new MediaFileItem
                {
                    Id = file.Id,
                    Name = file.FileName,
                    Extension = Path.GetExtension(file.FileName),
                    MimeType = file.MimeType,
                    Size = file.FileSize,
                    CreatedAt = file.CreatedAt,
                    FilePath = file.FilePath,
                    ModelType = file.ModelType,
                    ModelId = file.ModelId,
                    Collection = file.Collection ?? "",
                    Alt = file.Alt ?? "",
                    FormattedSize = FormatFileSize(file.FileSize),
                    IsImage = IsImageFile(file.MimeType),
                    PreviewUrl = $"/{file.FilePath}",
                    ThumbnailUrl = IsImageFile(file.MimeType) ? $"/{file.FilePath}" : "",
                    FileIcon = GetFileIcon(file.MimeType),
                    RelatedEntityName = await GetRelatedEntityNameAsync(file.ModelType, file.ModelId)
                };

                items.Add(item);
            }

            return items;
        }

        private async Task<MediaFileDetailsViewModel> MapToMediaFileDetailsAsync(MediaFile mediaFile)
        {
            return new MediaFileDetailsViewModel
            {
                Id = mediaFile.Id,
                FileName = mediaFile.FileName,
                MimeType = mediaFile.MimeType,
                FilePath = mediaFile.FilePath,
                Collection = mediaFile.Collection ?? "",
                ModelType = mediaFile.ModelType,
                ModelId = mediaFile.ModelId,
                FileSize = mediaFile.FileSize,
                FormattedSize = FormatFileSize(mediaFile.FileSize),
                Alt = mediaFile.Alt ?? "",
                CustomProperties = mediaFile.CustomProperties ?? "",
                CreatedAt = mediaFile.CreatedAt,
                UpdatedAt = mediaFile.UpdatedAt,
                PreviewUrl = $"/{mediaFile.FilePath}",
                IsImage = IsImageFile(mediaFile.MimeType),
                Extension = Path.GetExtension(mediaFile.FileName),
                FileIcon = GetFileIcon(mediaFile.MimeType),
                RelatedEntityName = await GetRelatedEntityNameAsync(mediaFile.ModelType, mediaFile.ModelId),
                RelatedEntityUrl = GetRelatedEntityUrl(mediaFile.ModelType, mediaFile.ModelId)
            };
        }

        private async Task<long> GetTotalSizeAsync(string folder)
        {
            var query = _context.MediaFiles.AsQueryable();
            
            if (!string.IsNullOrEmpty(folder) && folder != "all")
            {
                query = query.Where(m => m.ModelType.ToLower() == folder.ToLower());
            }

            return await query.SumAsync(m => m.FileSize);
        }

        private async Task<string> GetRelatedEntityNameAsync(string modelType, int modelId)
        {
            try
            {
                return modelType.ToLower() switch
                {
                    "material" => await _context.Materials
                        .Where(m => m.Id == modelId)
                        .Select(m => m.Name)
                        .FirstOrDefaultAsync() ?? "Unknown Material",
                    "requisition" => (await _context.Requisitions
                        .Where(r => r.Id == modelId)
                        .Select(r => (int?)r.Id)
                        .FirstOrDefaultAsync())?.ToString() ?? "Unknown Requisition",
                    "materialcategory" => await _context.MaterialCategories
                        .Where(c => c.Id == modelId)
                        .Select(c => c.Name)
                        .FirstOrDefaultAsync() ?? "Unknown Category",
                    "materialcondition" => await _context.MaterialConditions
                        .Where(c => c.MaterialId == modelId)
                        .Include(c => c.Material)
                        .Select(c => c.Material.Name)
                        .FirstOrDefaultAsync() ?? "Unknown Material",
                    _ => $"{modelType} #{modelId}"
                };
            }
            catch
            {
                return $"{modelType} #{modelId}";
            }
        }

        private string GetRelatedEntityUrl(string modelType, int modelId)
        {
            return modelType.ToLower() switch
            {
                "material" => Url.Action("Details", "Materials", new { id = modelId }),
                "requisition" => Url.Action("Details", "Requisitions", new { id = modelId }),
                "materialcategory" => Url.Action("Details", "MaterialCategories", new { id = modelId }),
                _ => "#"
            } ?? "#";
        }

        private static string GetFolderDisplayName(string modelType)
        {
            return modelType.ToLower() switch
            {
                "material" => "Materials",
                "requisition" => "Requisitions",
                "materialcategory" => "Categories",
                "materialcondition" => "Conditions",
                _ => modelType
            };
        }

        private static string GetFolderIcon(string modelType)
        {
            return modelType.ToLower() switch
            {
                "material" => "ri-box-3-line",
                "requisition" => "ri-file-list-3-line",
                "materialcategory" => "ri-folder-line",
                "materialcondition" => "ri-tools-line",
                _ => "ri-folder-2-line"
            };
        }

        private static bool IsImageFile(string mimeType)
        {
            return mimeType.StartsWith("image/");
        }

        private static string GetFileIcon(string mimeType)
        {
            if (mimeType.StartsWith("image/")) return "ri-image-line";
            if (mimeType.Contains("pdf")) return "ri-file-pdf-line";
            if (mimeType.Contains("word")) return "ri-file-word-line";
            if (mimeType.Contains("excel")) return "ri-file-excel-line";
            if (mimeType.Contains("powerpoint")) return "ri-file-ppt-line";
            if (mimeType.StartsWith("video/")) return "ri-video-line";
            if (mimeType.StartsWith("audio/")) return "ri-music-line";
            return "ri-file-line";
        }

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
