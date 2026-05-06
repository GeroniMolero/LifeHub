using LifeHub.Services.DocumentPublications;
using LifeHub.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    [ApiController]
    [Route("api/public/documents")]
    [AllowAnonymous]
    public class PublicDocumentsController : ApiControllerBase
    {
        private readonly IDocumentPublicationService _publicationService;

        public PublicDocumentsController(IDocumentPublicationService publicationService)
        {
            _publicationService = publicationService;
        }

        [HttpGet("{documentId:int}")]
        public async Task<IActionResult> GetPublicDocument(int documentId)
        {
            var result = await _publicationService.GetPublicDocumentAsync(documentId);
            return ToActionResult(result);
        }
    }
}
