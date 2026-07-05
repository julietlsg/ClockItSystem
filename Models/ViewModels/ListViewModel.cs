using System.Collections.Generic;

namespace ClockItSystem.Models.ViewModels
{
    public class ListViewModel<T>
    {
        public List<T> Items { get; set; } = new();

        public PagedRequest Filter { get; set; }
            = new();

        public PagedResult Pagination { get; set; }
            = new();
    }
}