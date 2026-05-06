using LifeHub.DTOs;
using LifeHub.Services.AllowedWebsites;
using LifeHub.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    [ApiController]
    [Route("api/admin/allowed-websites")]
    [Authorize(Roles = "Admin")]
    public class AllowedWebsitesController : ApiControllerBase
    {
        private readonly IAllowedWebsiteService _allowedWebsiteService;

        public AllowedWebsitesController(IAllowedWebsiteService allowedWebsiteService)
        {
            _allowedWebsiteService = allowedWebsiteService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllowedWebsites()
        {
            var result = await _allowedWebsiteService.GetAllowedWebsitesAsync();
            return ToActionResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAllowedWebsite([FromBody] CreateAllowedWebsiteDto dto)
        {
            var result = await _allowedWebsiteService.CreateAllowedWebsiteAsync(dto);
            if (!result.IsSuccess) return ToActionResult(result);

            return Created($"api/admin/allowed-websites/{result.Value!.Id}", result.Value);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateAllowedWebsite(int id, [FromBody] UpdateAllowedWebsiteDto dto)
        {
            var result = await _allowedWebsiteService.UpdateAllowedWebsiteAsync(id, dto);
            return ToActionResult(result);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteAllowedWebsite(int id)
        {
            var result = await _allowedWebsiteService.DeleteAllowedWebsiteAsync(id);
            if (!result.IsSuccess) return ToActionResult(result);

            return NoContent();
        }
    }
}
