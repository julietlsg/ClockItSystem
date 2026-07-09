using System;

namespace ClockItSystem.Models.ViewModels
{
    public class PagedResult
    {
        public int CurrentPage { get; set; }

        public int PageSize { get; set; }

        public int TotalRecords { get; set; }

        public int TotalPages =>
            (int)Math.Ceiling((double)TotalRecords / PageSize);

        public bool HasPreviousPage =>
            CurrentPage > 1;

        public bool HasNextPage =>
            CurrentPage < TotalPages;

        public int StartRecord =>
    TotalRecords == 0
        ? 0
        : ((CurrentPage - 1) * PageSize) + 1;

        public int EndRecord =>
            Math.Min(CurrentPage * PageSize, TotalRecords);
    }
}