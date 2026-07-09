using ClockItSystem.Data;
using ClockItSystem.Helpers;
using ClockItSystem.Interfaces;
using ClockItSystem.Models;
using ClockItSystem.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ClockItSystem.Services
{
    public class SiteService : ISiteService
    {
        private readonly ApplicationDbContext _context;

        public SiteService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ListViewModel<Site>> GetAllAsync(PagedRequest request)
        {
            var query = _context.Sites
                .Include(x => x.Client)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var search = request.SearchTerm.Trim();

                query = query.Where(x =>
                    x.SiteName.Contains(search) ||
                    x.SiteCode.Contains(search) ||
                    x.Client.Name.Contains(search) ||
                    (x.Address != null && x.Address.Contains(search)));
            }

            if (request.IsActive.HasValue)
            {
                query = query.Where(x =>
                    x.IsActive == request.IsActive.Value);
            }

            var totalRecords = await query.CountAsync();

            var sites = await query
                .OrderBy(x => x.SiteName)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new ListViewModel<Site>
            {
                Items = sites,

                Filter = request,

                Pagination = new PagedResult
                {
                    CurrentPage = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalRecords = totalRecords
                }
            };
        }

        public async Task<Site?> GetByIdAsync(int id)
        {
            return await _context.Sites
                .Include(x => x.Client)
                .Include(x => x.Students)
                .FirstOrDefaultAsync(x => x.SiteId == id);
        }

        public async Task<bool> ExistsAsync(string code)
        {
            return await _context.Sites
                .AnyAsync(x => x.SiteCode == code);
        }

        public async Task<ServiceResult> CreateAsync(SiteViewModel model)
        {
            if (await ExistsAsync(model.SiteCode))
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "A site with the same code already exists."
                };
            }

            var site = new Site
            {
                ClientId = model.ClientId,
                SiteName = model.SiteName.Trim(),
                SiteCode = model.SiteCode.Trim(),
                Address = model.Address,
                IsActive = model.IsActive,
                CreatedDate = DateTime.Now
            };

            _context.Sites.Add(site);

            await _context.SaveChangesAsync();

            return new ServiceResult
            {
                Success = true,
                Message = "Site created successfully.",
                Id = site.SiteId
            };
        }

        public async Task<ServiceResult> UpdateAsync(SiteViewModel model)
        {
            var site = await _context.Sites
                .FirstOrDefaultAsync(x => x.SiteId == model.SiteId);

            if (site == null)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "Site not found."
                };
            }

            bool duplicate = await _context.Sites.AnyAsync(x =>
                x.SiteCode == model.SiteCode &&
                x.SiteId != model.SiteId);

            if (duplicate)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "A site with the same code already exists."
                };
            }

            site.ClientId = model.ClientId;
            site.SiteName = model.SiteName.Trim();
            site.SiteCode = model.SiteCode.Trim();
            site.Address = model.Address;
            site.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            return new ServiceResult
            {
                Success = true,
                Message = "Site updated successfully."
            };
        }

        public async Task<ServiceResult> ToggleStatusAsync(int id)
        {
            var site = await _context.Sites
                .FirstOrDefaultAsync(x => x.SiteId == id);

            if (site == null)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = "Site not found."
                };
            }

            site.IsActive = !site.IsActive;

            await _context.SaveChangesAsync();

            return new ServiceResult
            {
                Success = true,
                Message = site.IsActive
                    ? "Site activated successfully."
                    : "Site deactivated successfully."
            };
        }

        public async Task<List<SelectListItem>> GetClientDropdownAsync()
        {
            return await _context.Clients
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem
                {
                    Value = x.ClientId.ToString(),
                    Text = x.Name
                })
                .ToListAsync();
        }
    }
}