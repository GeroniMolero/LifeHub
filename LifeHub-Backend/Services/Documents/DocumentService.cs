using AutoMapper;
using LifeHub.Data;
using LifeHub.DTOs;
using LifeHub.Models;
using LifeHub.Utilidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LifeHub.Services.Documents
{
    public class DocumentService : IDocumentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IHtmlSanitizer _htmlSanitizer;
        private readonly BusinessRules _rules;

        public DocumentService(ApplicationDbContext context, IMapper mapper, IHtmlSanitizer htmlSanitizer, IOptions<BusinessRules> rules)
        {
            _context = context;
            _mapper = mapper;
            _htmlSanitizer = htmlSanitizer;
            _rules = rules.Value;
        }

        public async Task<ServiceResult<List<DocumentDto>>> GetDocumentsAsync(string userId, bool canViewAll, int? spaceId = null)
        {
            var query = _context.Documents.AsQueryable();

            if (!canViewAll)
            {
                query = query.Where(d =>
                    d.UserId == userId ||
                    (d.CreativeSpaceId.HasValue &&
                     _context.SpacePermissions.Any(p =>
                         p.CreativeSpaceId == d.CreativeSpaceId.Value && p.UserId == userId))
                );
            }

            if (spaceId.HasValue)
                query = query.Where(d => d.CreativeSpaceId == spaceId.Value);

            var documents = await query
                .Include(d => d.User)
                .OrderByDescending(d => d.UpdatedAt)
                .ToListAsync();

            return ServiceResult<List<DocumentDto>>.Ok(_mapper.Map<List<DocumentDto>>(documents));
        }

        public async Task<ServiceResult<DocumentDto>> CopyToSpaceAsync(int documentId, string userId, int targetSpaceId)
        {
            var source = await _context.Documents
                .Include(d => d.CreativeSpace)
                    .ThenInclude(cs => cs!.Permissions)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (source == null)
                return ServiceResult<DocumentDto>.NotFound("Documento no encontrado.");

            if (!SpaceAccessPolicy.CanAccessDocument(source, userId))
                return ServiceResult<DocumentDto>.Forbidden("No tienes acceso a este documento.");

            var targetSpace = await _context.CreativeSpaces
                .Include(cs => cs.Permissions)
                .FirstOrDefaultAsync(cs => cs.Id == targetSpaceId);

            if (targetSpace == null)
                return ServiceResult<DocumentDto>.NotFound("Espacio destino no encontrado.");

            if (!SpaceAccessPolicy.CanEdit(targetSpace, userId))
                return ServiceResult<DocumentDto>.Forbidden("No tienes permiso para añadir documentos a este espacio.");

            var docCount = await _context.Documents.CountAsync(d => d.UserId == userId);
            if (docCount >= _rules.MaxDocumentsPerUser)
                return ServiceResult<DocumentDto>.BadRequest(
                    $"Has alcanzado el límite de {_rules.MaxDocumentsPerUser} documentos.");

            var copy = new Document
            {
                UserId = userId,
                CreativeSpaceId = targetSpaceId,
                Title = source.Title,
                Description = source.Description,
                Content = _htmlSanitizer.Sanitize(source.Content),
                Type = source.Type
            };

            _context.Documents.Add(copy);
            await _context.SaveChangesAsync();

            var created = await _context.Documents
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == copy.Id);

            return ServiceResult<DocumentDto>.Ok(_mapper.Map<DocumentDto>(created ?? copy));
        }

        public async Task<ServiceResult<DocumentDto>> GetDocumentAsync(int id, string userId, bool canViewAll)
        {
            var query = _context.Documents
                .Include(d => d.User)
                .Where(d => d.Id == id);

            if (!canViewAll)
            {
                query = query.Where(d =>
                    d.UserId == userId ||
                    (d.CreativeSpaceId.HasValue &&
                     _context.SpacePermissions.Any(p =>
                         p.CreativeSpaceId == d.CreativeSpaceId.Value && p.UserId == userId))
                );
            }

            var document = await query.FirstOrDefaultAsync();

            if (document == null)
                return ServiceResult<DocumentDto>.NotFound("Documento no encontrado.");

            return ServiceResult<DocumentDto>.Ok(_mapper.Map<DocumentDto>(document));
        }

        public async Task<ServiceResult<DocumentDto>> CreateDocumentAsync(string userId, CreateDocumentDto dto)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
                return ServiceResult<DocumentDto>.Unauthorized("Sesión inválida. Inicia sesión de nuevo.");

            var docCount = await _context.Documents.CountAsync(d => d.UserId == userId);
            if (docCount >= _rules.MaxDocumentsPerUser)
                return ServiceResult<DocumentDto>.BadRequest(
                    $"Has alcanzado el límite de {_rules.MaxDocumentsPerUser} documentos.");

            dto.Content = _htmlSanitizer.Sanitize(dto.Content);

            var document = _mapper.Map<Document>(dto);
            document.UserId = userId;

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            var created = await _context.Documents
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == document.Id);

            return ServiceResult<DocumentDto>.Ok(_mapper.Map<DocumentDto>(created ?? document));
        }

        public async Task<ServiceResult<DocumentDto>> UpdateDocumentAsync(int id, string userId, UpdateDocumentDto dto)
        {
            var document = await _context.Documents.FirstOrDefaultAsync(d => d.Id == id);

            if (document == null)
                return ServiceResult<DocumentDto>.NotFound("Documento no encontrado.");

            var isOwner = document.UserId == userId;
            var isSpaceEditor = document.CreativeSpaceId.HasValue &&
                await _context.SpacePermissions.AnyAsync(p =>
                    p.CreativeSpaceId == document.CreativeSpaceId.Value &&
                    p.UserId == userId &&
                    p.PermissionLevel == SpacePermissionLevel.Editor);

            if (!isOwner && !isSpaceEditor)
                return ServiceResult<DocumentDto>.Forbidden("No tienes permiso para editar este documento.");

            var versionCount = await _context.DocumentVersions.CountAsync(v => v.DocumentId == id);
            if (versionCount >= _rules.MaxDocumentVersions)
                return ServiceResult<DocumentDto>.BadRequest(
                    $"Este documento ha alcanzado el límite de {_rules.MaxDocumentVersions} versiones. Elimina alguna versión antes de guardar.");

            dto.Content = _htmlSanitizer.Sanitize(dto.Content);

            _mapper.Map(dto, document);
            document.UpdatedAt = DateTime.UtcNow;

            var lastVersionNumber = await _context.DocumentVersions
                .Where(v => v.DocumentId == id)
                .OrderByDescending(v => v.VersionNumber)
                .Select(v => (int?)v.VersionNumber)
                .FirstOrDefaultAsync();

            _context.DocumentVersions.Add(new DocumentVersion
            {
                DocumentId = document.Id,
                VersionNumber = (lastVersionNumber ?? 0) + 1,
                Title = document.Title,
                Description = document.Description,
                Content = document.Content,
                CreatedByUserId = userId
            });

            await _context.SaveChangesAsync();

            var updated = await _context.Documents
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == document.Id);

            return ServiceResult<DocumentDto>.Ok(_mapper.Map<DocumentDto>(updated ?? document));
        }

        public async Task<ServiceResult<bool>> DeleteDocumentAsync(int id, string userId)
        {
            var document = await _context.Documents.FirstOrDefaultAsync(d => d.Id == id);

            if (document == null)
                return ServiceResult<bool>.NotFound("Documento no encontrado.");

            if (document.UserId != userId)
                return ServiceResult<bool>.Forbidden("Solo el propietario del documento puede eliminarlo.");

            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<List<DocumentDto>>> GetPublicDocumentsByUserAsync(string targetUserId)
        {
            var documents = await _context.DocumentPublications
                .Where(p => p.PublishedByUserId == targetUserId && p.IsProfileVisible)
                .Include(p => p.Document)
                    .ThenInclude(d => d.User)
                .Include(p => p.Document)
                    .ThenInclude(d => d.Publication)
                .OrderByDescending(p => p.UpdatedAt)
                .Select(p => p.Document)
                .ToListAsync();

            return ServiceResult<List<DocumentDto>>.Ok(_mapper.Map<List<DocumentDto>>(documents));
        }

    }
}
