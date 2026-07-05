using ClockItSystem.Data;
using ClockItSystem.Helpers;
using ClockItSystem.Interfaces;
using ClockItSystem.Models;
using ClockItSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ClockItSystem.Services
{
    public class ClientService : IClientService
    {
        private readonly ApplicationDbContext _context;

        public ClientService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(List<Client> Clients, int TotalRecords)> GetAllAsync(PagedRequest request)
        {
            var query = _context.Clients
                .Include(c => c.Sites)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                string search = request.SearchTerm.Trim();

                query = query.Where(c =>
                    c.Name.Contains(search) ||
                    c.Code.Contains(search) ||
                    (c.ContactPerson != null &&
                     c.ContactPerson.Contains(search)));
            }

            if (request.IsActive.HasValue)
            {
                query = query.Where(c =>
                    c.IsActive == request.IsActive.Value);
            }

            int totalRecords = await query.CountAsync();

            var clients = await query
                .OrderBy(c => c.Name)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return (clients, totalRecords);
        }

        public async Task<Client?> GetByIdAsync(int id)
        {
            return await _context.Clients
                .Include(c => c.Sites)
                .FirstOrDefaultAsync(c => c.ClientId == id);
        }

        public async Task<bool> ExistsAsync(string code)
        {
            return await _context.Clients
                .AnyAsync(c => c.Code == code);
        }

        public async Task<ServiceResult> CreateAsync(ClientViewModel model)
        {
            if (await ExistsAsync(model.Code))
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "A client with the same code already exists."
                };
            }

            var client = new Client
            {
                Name = model.Name.Trim(),
                Code = model.Code.Trim(),
                ContactPerson = model.ContactPerson,
                Email = model.Email,
                Phone = model.Phone,
                IsActive = model.IsActive,
                CreatedDate = DateTime.Now
            };

            _context.Clients.Add(client);

            await _context.SaveChangesAsync();

            return new ServiceResult
            {
                Success = true,
                Message = "Client created successfully.",
                Id = client.ClientId
            };
        }

        public async Task<ServiceResult> UpdateAsync(ClientViewModel model)
        {
            var client = await _context.Clients
                .FirstOrDefaultAsync(c => c.ClientId == model.ClientId);

            if (client == null)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "Client not found."
                };
            }

            bool duplicate = await _context.Clients.AnyAsync(c =>
                c.Code == model.Code &&
                c.ClientId != model.ClientId);

            if (duplicate)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "A client with the same code already exists."
                };
            }

            client.Name = model.Name.Trim();
            client.Code = model.Code.Trim();
            client.ContactPerson = model.ContactPerson;
            client.Email = model.Email;
            client.Phone = model.Phone;
            client.IsActive = model.IsActive;
            client.UpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            return new ServiceResult
            {
                Success = true,
                Message = "Client updated successfully."
            };
        }

        public async Task<ServiceResult> ToggleStatusAsync(int id)
        {
            var client = await _context.Clients
                .FirstOrDefaultAsync(c => c.ClientId == id);

            if (client == null)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "Client not found."
                };
            }

            client.IsActive = !client.IsActive;

            client.UpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            return new ServiceResult
            {
                Success = true,
                Message = client.IsActive
                    ? "Client activated successfully."
                    : "Client deactivated successfully."
            };
        }
    }
}