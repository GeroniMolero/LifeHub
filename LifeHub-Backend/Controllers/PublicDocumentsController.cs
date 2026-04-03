using System.Text.Json;
using LifeHub.Data;
using LifeHub.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LifeHub.Utilidades;

namespace LifeHub.Controllers
{
    [ApiController]
    [Route("api/public/documents")]
    [AllowAnonymous]
    public class PublicDocumentsController : ApiControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PublicDocumentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("{documentId:int}")]
        public async Task<IActionResult> GetPublicDocument(int documentId)
        {
            var document = await _context.Documents
                .Include(d => d.Publication)
                .FirstOrDefaultAsync(d => d.Id == documentId && d.IsPublic);

            if (document == null || document.Publication == null)
                return NotFoundError("Documento público no encontrado.");

            var publication = document.Publication;

            var dto = new PublicDocumentViewDto
            {
                DocumentId = document.Id,
                Title = publication.PublicTitle ?? document.Title,
                Description = publication.PublicDescription ?? document.Description,
                Content = document.Content,
                PublishedAt = document.PublishedAt,
                MediaReferences = DeserializeList<MediaReferenceDto>(publication.MediaReferencesJson),
                ExternalLinks = DeserializeList<string>(publication.ExternalLinksJson)
            };

            return Ok(dto);
        }

        private static List<T> DeserializeList<T>(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<T>();

            try
            {
                return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
            }
            catch
            {
                return new List<T>();
            }
        }
    }
}
