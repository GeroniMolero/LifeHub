using AutoMapper;
using LifeHub.Data;
using LifeHub.DTOs;
using LifeHub.Models;
using LifeHub.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Controllers
{
    [ApiController]
    [Route("api/admin/allowed-websites")]
    [Authorize(Roles = "Admin")]
    public class AllowedWebsitesController : ApiControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public AllowedWebsitesController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllowedWebsites()
        {
            var websites = await _context.AllowedWebsites
                .OrderBy(w => w.Domain)
                .ToListAsync();

            return Ok(_mapper.Map<List<AllowedWebsiteDto>>(websites));
        }

        [HttpPost]
        public async Task<IActionResult> CreateAllowedWebsite([FromBody] CreateAllowedWebsiteDto dto)
        {
            var normalizedDomain = NormalizeDomain(dto.Domain);
            if (string.IsNullOrWhiteSpace(normalizedDomain))
                return BadRequestError("Debes indicar un dominio válido.");

            var exists = await _context.AllowedWebsites.AnyAsync(w => w.Domain == normalizedDomain);
            if (exists)
                return ConflictError("Ese dominio ya existe en la allowlist.");

            var website = new AllowedWebsite
            {
                Domain = normalizedDomain,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.AllowedWebsites.Add(website);
            await _context.SaveChangesAsync();

            return Created($"api/admin/allowed-websites/{website.Id}", _mapper.Map<AllowedWebsiteDto>(website));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateAllowedWebsite(int id, [FromBody] UpdateAllowedWebsiteDto dto)
        {
            var website = await _context.AllowedWebsites.FirstOrDefaultAsync(w => w.Id == id);
            if (website == null)
                return NotFoundError("Dominio no encontrado.");

            website.IsActive = dto.IsActive;
            website.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(_mapper.Map<AllowedWebsiteDto>(website));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteAllowedWebsite(int id)
        {
            var website = await _context.AllowedWebsites.FirstOrDefaultAsync(w => w.Id == id);
            if (website == null)
                return NotFoundError("Dominio no encontrado.");

            _context.AllowedWebsites.Remove(website);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private static string NormalizeDomain(string value)
        {
            var trimmed = (value ?? string.Empty).Trim().ToLowerInvariant();
            if (trimmed.StartsWith("http://")) trimmed = trimmed[7..];
            if (trimmed.StartsWith("https://")) trimmed = trimmed[8..];
            if (trimmed.StartsWith("www.")) trimmed = trimmed[4..];
            var slashIndex = trimmed.IndexOf('/');
            if (slashIndex >= 0) trimmed = trimmed[..slashIndex];
            return trimmed;
        }
    }
}
