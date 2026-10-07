using ClockItSystem.Models;

namespace ClockItSystem.Interfaces
{
    public interface IClientAccessService
    {
        Task<List<int>> GetAccessibleClientIdsAsync();

        Task<bool> CanAccessClientAsync(int clientId);
    }
}
