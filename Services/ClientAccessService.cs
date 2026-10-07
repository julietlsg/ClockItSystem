using ClockItSystem.Data;
using ClockItSystem.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ClockItSystem.Services
{
    public class ClientAccessService : IClientAccessService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ClientAccessService(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<int>> GetAccessibleClientIdsAsync()
        {
            var user = _httpContextAccessor.HttpContext?.User;

            if (user == null || !user.Identity?.IsAuthenticated == true)
            {
                return new List<int>();
            }

            // Admin has access to all active clients.
            if (user.IsInRole("Admin"))
            {
                return await _context.Clients
                    .Where(c => c.IsActive)
                    .Select(c => c.ClientId)
                    .ToListAsync();
            }

            var userId = user.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return new List<int>();
            }

            return await _context.UserClients
                .Where(uc =>
                    uc.UserId == userId &&
                    uc.IsActive &&
                    uc.Client.IsActive)
                .Select(uc => uc.ClientId)
                .Distinct()
                .ToListAsync();
        }

        public async Task<bool> CanAccessClientAsync(int clientId)
        {
            var user = _httpContextAccessor.HttpContext?.User;

            if (user == null || !user.Identity?.IsAuthenticated == true)
            {
                return false;
            }

            // Admin can access any active client.
            if (user.IsInRole("Admin"))
            {
                return await _context.Clients
                    .AnyAsync(c =>
                        c.ClientId == clientId &&
                        c.IsActive);
            }

            var userId = user.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return false;
            }

            return await _context.UserClients
                .AnyAsync(uc =>
                    uc.UserId == userId &&
                    uc.ClientId == clientId &&
                    uc.IsActive &&
                    uc.Client.IsActive);
        }
    }
}