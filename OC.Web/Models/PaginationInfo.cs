using System;

namespace OC.Web.Models
{
    public class PaginationInfo
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public string PreviousPageUrl => GetPageUrl(CurrentPage - 1);
        public string NextPageUrl => GetPageUrl(CurrentPage + 1);
        public Func<int, string> GetPageUrl { get; set; } = _ => "#";
    }
}