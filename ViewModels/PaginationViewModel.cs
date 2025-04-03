using System;
using System.Collections.Generic;

namespace MRIV.ViewModels
{
    public class PaginationViewModel
    {
        public int TotalItems { get; set; }
        public int ItemsPerPage { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages => (int)Math.Ceiling((decimal)TotalItems / ItemsPerPage);
        
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        
        public int StartPage => Math.Max(1, CurrentPage - 2);
        public int EndPage => Math.Min(TotalPages, CurrentPage + 2);
        
        public string Action { get; set; }
        public string Controller { get; set; }
        public Dictionary<string, string> RouteData { get; set; } = new Dictionary<string, string>();
    }
}
