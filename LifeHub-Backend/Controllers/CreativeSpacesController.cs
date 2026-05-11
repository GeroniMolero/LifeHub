using LifeHub.DTOs;
using LifeHub.Services.CreativeSpaces;
using LifeHub.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CreativeSpacesController : ApiControllerBase
    {
        private readonly ICreativeSpaceService _spaceService;

        public CreativeSpacesController(ICreativeSpaceService spaceService)
        {
            _spaceService = spaceService;
        }

        [HttpGet("public/{userId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicSpacesByUser(string userId)
        {
            var result = await _spaceService.GetPublicSpacesByUserAsync(userId);
            return ToActionResult(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetCreativeSpaces()
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _spaceService.GetCreativeSpacesAsync(userId);
            return ToActionResult(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCreativeSpace(int id)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _spaceService.GetCreativeSpaceAsync(id, userId);
            return ToActionResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCreativeSpace([FromBody] CreateCreativeSpaceDto dto)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _spaceService.CreateCreativeSpaceAsync(userId, dto, GetIpAddress());
            if (!result.IsSuccess) return ToActionResult(result);

            return Created($"api/creativespaces/{result.Value!.Id}", result.Value);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateCreativeSpace(int id, [FromBody] UpdateCreativeSpaceDto dto)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _spaceService.UpdateCreativeSpaceAsync(id, userId, dto, GetIpAddress());
            return ToActionResult(result);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteCreativeSpace(int id)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _spaceService.DeleteCreativeSpaceAsync(id, userId, GetIpAddress());
            if (!result.IsSuccess) return ToActionResult(result);

            return NoContent();
        }

        [HttpPut("{id:int}/favorite")]
        public async Task<IActionResult> AddFavorite(int id)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _spaceService.SetFavoriteAsync(id, userId, true);
            if (!result.IsSuccess) return ToActionResult(result);

            return NoContent();
        }

        [HttpDelete("{id:int}/favorite")]
        public async Task<IActionResult> RemoveFavorite(int id)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _spaceService.SetFavoriteAsync(id, userId, false);
            if (!result.IsSuccess) return ToActionResult(result);

            return NoContent();
        }

        [HttpGet("{id:int}/permissions")]
        public async Task<IActionResult> GetPermissions(int id)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _spaceService.GetPermissionsAsync(id, userId);
            return ToActionResult(result);
        }

        [HttpPost("{id:int}/permissions")]
        public async Task<IActionResult> ShareCreativeSpace(int id, [FromBody] ShareCreativeSpaceDto dto)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _spaceService.ShareAsync(id, userId, dto, GetIpAddress());
            return ToActionResult(result);
        }

        [HttpDelete("{id:int}/permissions/{targetUserId}")]
        public async Task<IActionResult> RemovePermission(int id, string targetUserId)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _spaceService.RemovePermissionAsync(id, userId, targetUserId, GetIpAddress());
            if (!result.IsSuccess) return ToActionResult(result);

            return NoContent();
        }

        [HttpGet("{id:int}/media-references")]
        public async Task<IActionResult> GetMediaReferences(int id)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _spaceService.GetMediaReferencesAsync(id, userId);
            return ToActionResult(result);
        }

        [HttpPost("{id:int}/media-references")]
        public async Task<IActionResult> AddMediaReference(int id, [FromBody] CreateSpaceMediaReferenceDto dto)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _spaceService.AddMediaReferenceAsync(id, userId, dto);
            return ToActionResult(result);
        }

        [HttpDelete("{id:int}/media-references/{referenceId}")]
        public async Task<IActionResult> RemoveMediaReference(int id, string referenceId)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _spaceService.RemoveMediaReferenceAsync(id, userId, referenceId);
            if (!result.IsSuccess) return ToActionResult(result);

            return NoContent();
        }

        private string GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
    }
}
