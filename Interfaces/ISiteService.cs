using ClockItSystem.Models;

namespace ClockItSystem.Interfaces
{
    public interface ISiteService
    {
        Task<IEnumerable<Site>> GetAllAsync();

        Task<IEnumerable<Site>> GetByClientAsync(int clientId);

        Task<Site?> GetByIdAsync(int id);

        Task CreateAsync(Site site);

        Task UpdateAsync(Site site);

        Task<bool> DeleteAsync(int id);
    }
}
