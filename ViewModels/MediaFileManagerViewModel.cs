using MRIV.Models;
using System.ComponentModel.DataAnnotations;

namespace MRIV.ViewModels
{
    public class MediaFileManagerViewModel
    {
        public List<MediaFileItem> Files { get; set; } = new();
        public List<FolderItem> Folders { get; set; } = new();
        public string CurrentFolder { get; set; } = "all";
        public string ViewMode { get; set; } = "grid"; // "grid" or "list"
        public int TotalFiles { get; set; }
        public long TotalSize { get; set; }
        public string SearchQuery { get; set; } = string.Empty;
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 24;
        public int TotalPages { get; set; }
        public string SortBy { get; set; } = "name"; // "name", "date", "size", "type"
        public string SortOrder { get; set; } = "asc"; // "asc", "desc"
    }

    public class MediaFileItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime CreatedAt { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string PreviewUrl { get; set; } = string.Empty;
        public bool IsImage { get; set; }
        public string ModelType { get; set; } = string.Empty;
        public int ModelId { get; set; }
        public string RelatedEntityName { get; set; } = string.Empty;
        public string Collection { get; set; } = string.Empty;
        public string Alt { get; set; } = string.Empty;
        public string FormattedSize { get; set; } = string.Empty;
        public string FileIcon { get; set; } = string.Empty;
    }

    public class FolderItem
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int FileCount { get; set; }
        public long TotalSize { get; set; }
        public string Icon { get; set; } = string.Empty;
        public string FormattedSize { get; set; } = string.Empty;
    }

    public class MediaFileDetailsViewModel
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Collection { get; set; } = string.Empty;
        public string ModelType { get; set; } = string.Empty;
        public int ModelId { get; set; }
        public long FileSize { get; set; }
        public string FormattedSize { get; set; } = string.Empty;
        public string Alt { get; set; } = string.Empty;
        public string CustomProperties { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string PreviewUrl { get; set; } = string.Empty;
        public bool IsImage { get; set; }
        public string RelatedEntityName { get; set; } = string.Empty;
        public string RelatedEntityUrl { get; set; } = string.Empty;
        public string FileIcon { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
    }
}
