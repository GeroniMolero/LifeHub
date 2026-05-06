using AutoMapper;
using LifeHub.Data;
using LifeHub.DTOs;
using LifeHub.Models;
using LifeHub.Utilidades;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Services.AllowedWebsites
{
    public class AllowedWebsiteService : IAllowedWebsiteService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public AllowedWebsiteService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<ServiceResult<List<AllowedWebsiteDto>>> GetAllowedWebsitesAsync()
        {
            var websites = await _context.AllowedWebsites.OrderBy(w => w.Domain).ToListAsync();
            return ServiceResult<List<AllowedWebsiteDto>>.Ok(_mapper.Map<List<AllowedWebsiteDto>>(websites));
        }

        public async Task<ServiceResult<List<string>>> GetActiveDomainsAsync()
        {
            var domains = await _context.AllowedWebsites
                .Where(w => w.IsActive)
                .OrderBy(w => w.Domain)
                .Select(w => w.Domain)
                .ToListAsync();

            return ServiceResult<List<string>>.Ok(domains);
        }

        public async Task<ServiceResult<AllowedWebsiteDto>> CreateAllowedWebsiteAsync(CreateAllowedWebsiteDto dto)
        {
            var normalizedDomain = DomainHelper.NormalizeUserInputDomain(dto.Domain);
            if (string.IsNullOrWhiteSpace(normalizedDomain))
                return ServiceResult<AllowedWebsiteDto>.BadRequest("Debes indicar un dominio válido.");

            var exists = await _context.AllowedWebsites.AnyAsync(w => w.Domain == normalizedDomain);
            if (exists)
                return ServiceResult<AllowedWebsiteDto>.Conflict("Ese dominio ya existe en la allowlist.");

            var website = new AllowedWebsite
            {
                Domain = normalizedDomain,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.AllowedWebsites.Add(website);
            await _context.SaveChangesAsync();

            return ServiceResult<AllowedWebsiteDto>.Ok(_mapper.Map<AllowedWebsiteDto>(website));
        }

        public async Task<ServiceResult<AllowedWebsiteDto>> UpdateAllowedWebsiteAsync(int id, UpdateAllowedWebsiteDto dto)
        {
            var website = await _context.AllowedWebsites.FirstOrDefaultAsync(w => w.Id == id);
            if (website == null)
                return ServiceResult<AllowedWebsiteDto>.NotFound("Dominio no encontrado.");

            website.IsActive = dto.IsActive;
            website.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ServiceResult<AllowedWebsiteDto>.Ok(_mapper.Map<AllowedWebsiteDto>(website));
        }

        public async Task<ServiceResult<bool>> DeleteAllowedWebsiteAsync(int id)
        {
            var website = await _context.AllowedWebsites.FirstOrDefaultAsync(w => w.Id == id);
            if (website == null)
                return ServiceResult<bool>.NotFound("Dominio no encontrado.");

            _context.AllowedWebsites.Remove(website);
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Ok(true);
        }
    }
}
