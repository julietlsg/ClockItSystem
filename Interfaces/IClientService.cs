using ClockItSystem.Helpers;
using ClockItSystem.Models;
using ClockItSystem.Models.ViewModels;

namespace ClockItSystem.Interfaces
{
    public interface IClientService
    {
        Task<List<Client>> GetAllAsync();

        Task<Client?> GetByIdAsync(int id);

        Task<ServiceResult> CreateAsync(ClientViewModel model);

        Task<ServiceResult> UpdateAsync(ClientViewModel model);

        Task<ServiceResult> DeactivateAsync(int id);

        Task<bool> ExistsAsync(string code);
    }
}
