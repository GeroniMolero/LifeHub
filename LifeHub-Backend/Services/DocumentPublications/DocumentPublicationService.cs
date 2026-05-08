using System.Text.Json;
using LifeHub.Data;
using LifeHub.DTOs;
using LifeHub.Models;
using LifeHub.Utilidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LifeHub.Services.DocumentPublications
{
    public class DocumentPublicationService : IDocumentPublicationService
    {
        private readonly ApplicationDbContext _context;
        private readonly BusinessRules _rules;

        public DocumentPublicationService(ApplicationDbContext context, IOptions<BusinessRules> rules)
        {
            _context = context;
            _rules = rules.Value;
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

            if (dto.IsPublic && !document.IsPublic)
            {
                var publishedCount = await _context.Documents
                    .CountAsync(d => d.UserId == userId && d.IsPublic && d.Id != documentId);
                if (publishedCount >= _rules.MaxPublishedDocumentsPerUser)
                    return ServiceResult<DocumentPublicationDto>.BadRequest(
                        $"Has alcanzado el límite de {_rules.MaxPublishedDocumentsPerUser} documentos publicados.");
            }

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

        public async Task<ServiceResult<DocumentPublicationDto>> SetProfileVisibilityAsync(int documentId, string userId, bool isVisible)
        {
            var publication = await _context.DocumentPublications
                .Include(p => p.Document)
                .FirstOrDefaultAsync(p => p.DocumentId == documentId && p.PublishedByUserId == userId);

            if (publication == null)
                return ServiceResult<DocumentPublicationDto>.NotFound("Publicación no encontrada.");

            if (isVisible && !publication.IsProfileVisible)
            {
                var visibleCount = await _context.DocumentPublications
                    .CountAsync(p => p.PublishedByUserId == userId && p.IsProfileVisible && p.DocumentId != documentId);
                if (visibleCount >= _rules.MaxProfileVisibleDocumentsPerUser)
                    return ServiceResult<DocumentPublicationDto>.BadRequest(
                        $"Solo puedes tener {_rules.MaxProfileVisibleDocumentsPerUser} documentos visibles en tu perfil.");
            }

            publication.IsProfileVisible = isVisible;
            publication.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ServiceResult<DocumentPublicationDto>.Ok(ToPublicationDto(publication.Document, publication));
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
                IsProfileVisible = publication.IsProfileVisible,
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
