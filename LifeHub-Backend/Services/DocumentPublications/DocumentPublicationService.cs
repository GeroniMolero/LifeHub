using System.Text.Json;
using LifeHub.Data;
using LifeHub.DTOs;
using LifeHub.Models;
using LifeHub.Utilidades;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Services.DocumentPublications
{
    public class DocumentPublicationService : IDocumentPublicationService
    {
        private readonly ApplicationDbContext _context;

        public DocumentPublicationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceResult<DocumentPublicationDto>> GetPublicationAsync(int documentId, string userId)
        {
            var document = await _context.Documents
                .Include(d => d.Publication)
                .FirstOrDefaultAsync(d => d.Id == documentId && d.UserId == userId);

            if (document == null)
                return ServiceResult<DocumentPublicationDto>.NotFound("Documento no encontrado.");

            if (document.Publication == null)
            {
                return ServiceResult<DocumentPublicationDto>.Ok(new DocumentPublicationDto
                {
                    DocumentId = document.Id,
                    IsPublic = document.IsPublic,
                    PublishedAt = document.PublishedAt
                });
            }

            return ServiceResult<DocumentPublicationDto>.Ok(ToPublicationDto(document, document.Publication));
        }

        public async Task<ServiceResult<DocumentPublicationDto>> UpsertPublicationAsync(int documentId, string userId, UpsertDocumentPublicationDto dto)
        {
            var document = await _context.Documents
                .Include(d => d.Publication)
                .FirstOrDefaultAsync(d => d.Id == documentId && d.UserId == userId);

            if (document == null)
                return ServiceResult<DocumentPublicationDto>.NotFound("Documento no encontrado.");

            var linksError = await ValidateExternalLinksAsync(dto.ExternalLinks);
            if (linksError != null)
                return linksError;

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
            publication.Author = string.IsNullOrWhiteSpace(dto.Author) ? null : dto.Author.Trim();
            publication.MediaReferencesJson = JsonSerializer.Serialize(dto.MediaReferences ?? new List<MediaReferenceDto>());
            publication.ExternalLinksJson = JsonSerializer.Serialize(
                (dto.ExternalLinks ?? new List<string>())
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Distinct()
                .ToList());
            publication.UpdatedAt = DateTime.UtcNow;

            document.IsPublic = dto.IsPublic;
            document.PublishedAt = dto.IsPublic ? (document.PublishedAt ?? DateTime.UtcNow) : null;
            document.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ServiceResult<DocumentPublicationDto>.Ok(ToPublicationDto(document, publication));
        }

        public async Task<ServiceResult<PublicDocumentViewDto>> GetPublicDocumentAsync(int documentId)
        {
            var document = await _context.Documents
                .Include(d => d.Publication)
                .FirstOrDefaultAsync(d => d.Id == documentId && d.IsPublic);

            if (document == null || document.Publication == null)
                return ServiceResult<PublicDocumentViewDto>.NotFound("Documento público no encontrado.");

            var publication = document.Publication;

            return ServiceResult<PublicDocumentViewDto>.Ok(new PublicDocumentViewDto
            {
                DocumentId = document.Id,
                Title = publication.PublicTitle ?? document.Title,
                Description = publication.PublicDescription ?? document.Description,
                Content = document.Content,
                Author = publication.Author,
                PublishedAt = document.PublishedAt,
                MediaReferences = DeserializeList<MediaReferenceDto>(publication.MediaReferencesJson),
                ExternalLinks = DeserializeList<string>(publication.ExternalLinksJson)
            });
        }

        private async Task<ServiceResult<DocumentPublicationDto>?> ValidateExternalLinksAsync(List<string>? externalLinks)
        {
            var links = (externalLinks ?? new List<string>())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(l => l.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (links.Count == 0) return null;

            var allowedDomains = await _context.AllowedWebsites
                .Where(w => w.IsActive)
                .Select(w => w.Domain)
                .ToListAsync();

            var rejected = links.Where(link =>
            {
                if (!Uri.TryCreate(link, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                    return true;
                var host = DomainHelper.NormalizeHost(uri.Host);
                return !allowedDomains.Any(domain => host == domain || host.EndsWith($".{domain}"));
            }).ToList();

            if (rejected.Count == 0) return null;

            return ServiceResult<DocumentPublicationDto>.BadRequest(
                $"Hay enlaces externos no permitidos: {string.Join(", ", rejected)}");
        }

        private static DocumentPublicationDto ToPublicationDto(Document document, DocumentPublication publication) =>
            new()
            {
                DocumentId = document.Id,
                IsPublic = document.IsPublic,
                PublishedAt = document.PublishedAt,
                PublicTitle = publication.PublicTitle,
                PublicDescription = publication.PublicDescription,
                Author = publication.Author,
                MediaReferences = DeserializeList<MediaReferenceDto>(publication.MediaReferencesJson),
                ExternalLinks = DeserializeList<string>(publication.ExternalLinksJson),
                CreatedAt = publication.CreatedAt,
                UpdatedAt = publication.UpdatedAt
            };

        private static List<T> DeserializeList<T>(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<T>();
            try { return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>(); }
            catch { return new List<T>(); }
        }
    }
}
