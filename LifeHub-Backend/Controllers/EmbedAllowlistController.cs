using LifeHub.Services.AllowedWebsites;
using LifeHub.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    [ApiController]
    [Route("api/embed-allowlist")]
    [AllowAnonymous]
    public class EmbedAllowlistController : ApiControllerBase
    {
        private readonly IAllowedWebsiteService _allowedWebsiteService;

        public EmbedAllowlistController(IAllowedWebsiteService allowedWebsiteService)
        {
            _allowedWebsiteService = allowedWebsiteService;
        }

        [HttpGet]
        public async Task<IActionResult> GetActiveDomains()
        {
            var result = await _allowedWebsiteService.GetActiveDomainsAsync();
            return ToActionResult(result);
        }
    }
}
