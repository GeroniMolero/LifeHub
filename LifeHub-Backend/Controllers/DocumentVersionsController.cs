using LifeHub.DTOs;
using LifeHub.Services.DocumentVersions;
using LifeHub.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DocumentVersionsController : ApiControllerBase
    {
        private readonly IDocumentVersionService _versionService;

        public DocumentVersionsController(IDocumentVersionService versionService)
        {
            _versionService = versionService;
        }

        [HttpGet("document/{documentId:int}")]
        public async Task<IActionResult> GetDocumentVersions(int documentId)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _versionService.GetDocumentVersionsAsync(documentId, userId);
            return ToActionResult(result);
        }

        [HttpPost("document/{documentId:int}/snapshot")]
        public async Task<IActionResult> CreateSnapshot(int documentId, [FromBody] CreateDocumentVersionDto dto)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _versionService.CreateSnapshotAsync(documentId, userId, dto, GetIpAddress());
            if (!result.IsSuccess) return ToActionResult(result);

            return Created($"api/documentversions/{result.Value!.Id}", result.Value);
        }

        [HttpPost("{versionId:int}/restore")]
        public async Task<IActionResult> RestoreVersion(int versionId)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _versionService.RestoreVersionAsync(versionId, userId, GetIpAddress());
            return ToActionResult(result);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteDocumentVersion(int id)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _versionService.DeleteDocumentVersionAsync(id, userId, GetIpAddress());
            if (!result.IsSuccess) return ToActionResult(result);

            return NoContent();
        }

        private string GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
    }
}
