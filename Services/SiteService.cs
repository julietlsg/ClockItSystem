using ClockItSystem.Interfaces;
using ClockItSystem.Models;

namespace ClockItSystem.Services
{
    public class SiteService : ISiteService
    {
        public Task CreateAsync(Site site)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Site>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Site>> GetByClientAsync(int clientId)
        {
            throw new NotImplementedException();
        }

        public Task<Site?> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(Site site)
        {
            throw new NotImplementedException();
        }
    }
}
