using LifeHub.DTOs;
using LifeHub.Services.DocumentPublications;
using LifeHub.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    [ApiController]
    [Route("api/documents/{documentId:int}/publication")]
    [Authorize]
    public class DocumentPublicationsController : ApiControllerBase
    {
        private readonly IDocumentPublicationService _publicationService;

        public DocumentPublicationsController(IDocumentPublicationService publicationService)
        {
            _publicationService = publicationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPublication(int documentId)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _publicationService.GetPublicationAsync(documentId, userId);
            return ToActionResult(result);
        }

        [HttpPut]
        public async Task<IActionResult> UpsertPublication(int documentId, [FromBody] UpsertDocumentPublicationDto dto)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _publicationService.UpsertPublicationAsync(documentId, userId, dto);
            return ToActionResult(result);
        }
    }
}
