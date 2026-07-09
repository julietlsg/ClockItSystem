using ClockItSystem.Helpers;
using ClockItSystem.Models;
using ClockItSystem.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClockItSystem.Interfaces
{
    public interface ISiteService
    {
        Task<ListViewModel<Site>> GetAllAsync(PagedRequest request);

        Task<Site?> GetByIdAsync(int id);

        Task<ServiceResult> CreateAsync(SiteViewModel model);

        Task<ServiceResult> UpdateAsync(SiteViewModel model);

        Task<ServiceResult> ToggleStatusAsync(int id);

        Task<bool> ExistsAsync(string code);

        Task<List<SelectListItem>> GetClientDropdownAsync();
    }
}