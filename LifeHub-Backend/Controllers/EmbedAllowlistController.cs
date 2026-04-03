using LifeHub.Data;
using LifeHub.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Controllers
{
    [ApiController]
    [Route("api/embed-allowlist")]
    [Authorize]
    public class EmbedAllowlistController : ApiControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EmbedAllowlistController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetActiveDomains()
        {
            var domains = await _context.AllowedWebsites
                .Where(w => w.IsActive)
                .OrderBy(w => w.Domain)
                .Select(w => w.Domain)
                .ToListAsync();

            return Ok(domains);
        }
    }
}
