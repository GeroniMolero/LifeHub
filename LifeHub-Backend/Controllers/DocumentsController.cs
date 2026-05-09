using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LifeHub.DTOs;
using LifeHub.Services.Documents;
using LifeHub.Utilidades;

namespace LifeHub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DocumentsController : ApiControllerBase
    {
        private readonly IDocumentService _documentService;

        public DocumentsController(IDocumentService documentService)
        {
            _documentService = documentService;
        }

        [HttpGet("public/{userId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicDocumentsByUser(string userId)
        {
            var result = await _documentService.GetPublicDocumentsByUserAsync(userId);
            return ToActionResult(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetDocuments([FromQuery] int? spaceId = null)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _documentService.GetDocumentsAsync(userId, HasPermission("documents.view.all"), spaceId);
            return ToActionResult(result);
        }

        [HttpPost("{id}/copy")]
        public async Task<IActionResult> CopyToSpace(int id, [FromBody] CopyDocumentDto dto)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _documentService.CopyToSpaceAsync(id, userId, dto.TargetSpaceId);
            if (!result.IsSuccess) return ToActionResult(result);

            return Created($"api/documents/{result.Value!.Id}", result.Value);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDocument(int id)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _documentService.GetDocumentAsync(id, userId, HasPermission("documents.view.all"));
            return ToActionResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateDocument([FromBody] CreateDocumentDto dto)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _documentService.CreateDocumentAsync(userId, dto);
            if (!result.IsSuccess) return ToActionResult(result);

            return Created($"api/documents/{result.Value!.Id}", result.Value);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDocument(int id, [FromBody] UpdateDocumentDto dto)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _documentService.UpdateDocumentAsync(id, userId, dto);
            return ToActionResult(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _documentService.DeleteDocumentAsync(id, userId);
            if (!result.IsSuccess) return ToActionResult(result);

            return NoContent();
        }

        private bool HasPermission(string value) => User.HasClaim("permission", value);
    }
}
