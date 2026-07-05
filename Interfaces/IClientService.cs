using ClockItSystem.Helpers;
using ClockItSystem.Models;
using ClockItSystem.Models.ViewModels;

namespace ClockItSystem.Interfaces
{
    public interface IClientService
    {
        Task<(List<Client> Clients, int TotalRecords)> GetAllAsync(PagedRequest request);

        Task<Client?> GetByIdAsync(int id);

        Task<ServiceResult> CreateAsync(ClientViewModel model);

        Task<ServiceResult> UpdateAsync(ClientViewModel model);

        Task<ServiceResult> ToggleStatusAsync(int id);

        Task<bool> ExistsAsync(string code);
    }
}