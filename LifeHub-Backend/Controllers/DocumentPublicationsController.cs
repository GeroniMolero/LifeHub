using System.Text.Json;
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
    [Route("api/documents/{documentId:int}/publication")]
    [Authorize]
    public class DocumentPublicationsController : ApiControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DocumentPublicationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetPublication(int documentId)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var document = await _context.Documents
                .Include(d => d.Publication)
                .FirstOrDefaultAsync(d => d.Id == documentId && d.UserId == userId);

            if (document == null)
                return NotFoundError("Documento no encontrado.");

            var publication = document.Publication;
            if (publication == null)
            {
                return Ok(new DocumentPublicationDto
                {
                    DocumentId = document.Id,
                    IsPublic = document.IsPublic,
                    PublishedAt = document.PublishedAt
                });
            }

            return Ok(ToPublicationDto(document, publication));
        }

        [HttpPut]
        public async Task<IActionResult> UpsertPublication(int documentId, [FromBody] UpsertDocumentPublicationDto dto)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var document = await _context.Documents
                .Include(d => d.Publication)
                .FirstOrDefaultAsync(d => d.Id == documentId && d.UserId == userId);

            if (document == null)
                return NotFoundError("Documento no encontrado.");

            var linksValidationError = await ValidateExternalLinksAsync(dto.ExternalLinks);
            if (linksValidationError != null)
                return linksValidationError;

            var publication = document.Publication;
            if (publication == null)
            {
                publication = new DocumentPublication
                {
                    DocumentId = document.Id,
                    PublishedByUserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.DocumentPublications.Add(publication);
            }

            publication.PublicTitle = string.IsNullOrWhiteSpace(dto.PublicTitle) ? null : dto.PublicTitle.Trim();
            publication.PublicDescription = string.IsNullOrWhiteSpace(dto.PublicDescription) ? null : dto.PublicDescription.Trim();
            publication.MediaReferencesJson = JsonSerializer.Serialize(dto.MediaReferences ?? new List<MediaReferenceDto>());
            publication.ExternalLinksJson = JsonSerializer.Serialize((dto.ExternalLinks ?? new List<string>()).Select(l => l.Trim()).Where(l => !string.IsNullOrWhiteSpace(l)).Distinct().ToList());
            publication.UpdatedAt = DateTime.UtcNow;

            document.IsPublic = dto.IsPublic;
            document.PublishedAt = dto.IsPublic ? (document.PublishedAt ?? DateTime.UtcNow) : null;
            document.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(ToPublicationDto(document, publication));
        }

        private async Task<IActionResult?> ValidateExternalLinksAsync(List<string>? externalLinks)
        {
            var links = (externalLinks ?? new List<string>())
                .Where(link => !string.IsNullOrWhiteSpace(link))
                .Select(link => link.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (links.Count == 0)
                return null;

            var allowedDomains = await _context.AllowedWebsites
                .Where(w => w.IsActive)
                .Select(w => w.Domain)
                .ToListAsync();

            var rejected = new List<string>();

            foreach (var link in links)
            {
                if (!Uri.TryCreate(link, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                {
                    rejected.Add(link);
                    continue;
                }

                var host = NormalizeDomain(uri.Host);
                var isAllowed = allowedDomains.Any(domain => host == domain || host.EndsWith($".{domain}"));
                if (!isAllowed)
                    rejected.Add(link);
            }

            if (rejected.Count == 0)
                return null;

            return BadRequest(new ApiErrorDto
            {
                Code = "external_links_not_allowed",
                Message = "Hay enlaces externos no permitidos por la allowlist.",
                Details = rejected
            });
        }

        private static DocumentPublicationDto ToPublicationDto(Document document, DocumentPublication publication)
        {
            return new DocumentPublicationDto
            {
                DocumentId = document.Id,
                IsPublic = document.IsPublic,
                PublishedAt = document.PublishedAt,
                PublicTitle = publication.PublicTitle,
                PublicDescription = publication.PublicDescription,
                MediaReferences = DeserializeList<MediaReferenceDto>(publication.MediaReferencesJson),
                ExternalLinks = DeserializeList<string>(publication.ExternalLinksJson),
                CreatedAt = publication.CreatedAt,
                UpdatedAt = publication.UpdatedAt
            };
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

        private static string NormalizeDomain(string value)
        {
            var trimmed = (value ?? string.Empty).Trim().ToLowerInvariant();
            if (trimmed.StartsWith("www.")) trimmed = trimmed[4..];
            return trimmed;
        }
    }
}
