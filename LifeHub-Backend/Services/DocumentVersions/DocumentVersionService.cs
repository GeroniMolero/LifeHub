using AutoMapper;
using LifeHub.Data;
using LifeHub.DTOs;
using LifeHub.Models;
using LifeHub.Utilidades;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Services.DocumentVersions
{
    public class DocumentVersionService : IDocumentVersionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IActivityLogService _activityLogService;

        public DocumentVersionService(ApplicationDbContext context, IMapper mapper, IActivityLogService activityLogService)
        {
            _context = context;
            _mapper = mapper;
            _activityLogService = activityLogService;
        }

        public async Task<ServiceResult<List<DocumentVersionDto>>> GetDocumentVersionsAsync(int documentId, string userId)
        {
            var document = await _context.Documents
                .Include(d => d.CreativeSpace)
                    .ThenInclude(cs => cs!.Permissions)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null)
                return ServiceResult<List<DocumentVersionDto>>.NotFound("Documento no encontrado.");

            if (!CanAccessDocument(document, userId))
                return ServiceResult<List<DocumentVersionDto>>.Forbidden("No tienes permisos para ver las versiones de este documento.");

            var versions = await _context.DocumentVersions
                .Include(v => v.CreatedByUser)
                .Where(v => v.DocumentId == documentId)
                .OrderByDescending(v => v.VersionNumber)
                .ToListAsync();

            return ServiceResult<List<DocumentVersionDto>>.Ok(_mapper.Map<List<DocumentVersionDto>>(versions));
        }

        public async Task<ServiceResult<DocumentVersionDto>> CreateSnapshotAsync(int documentId, string userId, CreateDocumentVersionDto dto, string ipAddress)
        {
            var document = await _context.Documents
                .Include(d => d.CreativeSpace)
                    .ThenInclude(cs => cs!.Permissions)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null)
                return ServiceResult<DocumentVersionDto>.NotFound("Documento no encontrado.");

            if (!CanEditDocument(document, userId))
                return ServiceResult<DocumentVersionDto>.Forbidden("No tienes permisos para crear versiones de este documento.");

            const int MaxVersions = 30;
            var versionCount = await _context.DocumentVersions.CountAsync(v => v.DocumentId == documentId);
            if (versionCount >= MaxVersions)
                return ServiceResult<DocumentVersionDto>.BadRequest($"Este documento ha alcanzado el límite de {MaxVersions} versiones. Elimina alguna versión antes de crear una nueva.");

            var lastVersion = await _context.DocumentVersions
                .Where(v => v.DocumentId == documentId)
                .OrderByDescending(v => v.VersionNumber)
                .Select(v => (int?)v.VersionNumber)
                .FirstOrDefaultAsync();

            var nextVersion = (lastVersion ?? 0) + 1;

            var version = new DocumentVersion
            {
                DocumentId = document.Id,
                VersionNumber = nextVersion,
                Title = document.Title,
                Description = string.IsNullOrWhiteSpace(dto.Note) ? document.Description : dto.Note,
                Content = document.Content,
                CreatedByUserId = userId
            };

            _context.DocumentVersions.Add(version);
            await _context.SaveChangesAsync();

            await _activityLogService.LogAsync(userId, "document.version-created", nameof(Document), document.Id.ToString(), $"Created version {nextVersion}", ipAddress);

            return ServiceResult<DocumentVersionDto>.Ok(_mapper.Map<DocumentVersionDto>(version));
        }

        public async Task<ServiceResult<RestoreVersionResultDto>> RestoreVersionAsync(int versionId, string userId, string ipAddress)
        {
            var version = await _context.DocumentVersions
                .Include(v => v.Document)
                    .ThenInclude(d => d.CreativeSpace)
                        .ThenInclude(cs => cs!.Permissions)
                .FirstOrDefaultAsync(v => v.Id == versionId);

            if (version == null)
                return ServiceResult<RestoreVersionResultDto>.NotFound("Versión de documento no encontrada.");

            if (!CanEditDocument(version.Document, userId))
                return ServiceResult<RestoreVersionResultDto>.Forbidden("No tienes permisos para restaurar esta versión.");

            version.Document.Title = version.Title;
            version.Document.Description = version.Description;
            version.Document.Content = version.Content;
            version.Document.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _activityLogService.LogAsync(userId, "document.version-restored", nameof(Document), version.DocumentId.ToString(), $"Restored version {version.VersionNumber}", ipAddress);

            return ServiceResult<RestoreVersionResultDto>.Ok(new RestoreVersionResultDto
            {
                Message = "Versión restaurada",
                DocumentId = version.DocumentId,
                RestoredVersion = version.VersionNumber
            });
        }

        public async Task<ServiceResult<bool>> DeleteDocumentVersionAsync(int id, string userId, string ipAddress)
        {
            var version = await _context.DocumentVersions
                .Include(v => v.Document)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (version == null)
                return ServiceResult<bool>.NotFound("Versión de documento no encontrada.");

            if (version.Document.UserId != userId)
                return ServiceResult<bool>.Forbidden("Solo el propietario del documento puede eliminar versiones.");

            _context.DocumentVersions.Remove(version);
            await _context.SaveChangesAsync();

            await _activityLogService.LogAsync(userId, "document.version-deleted", nameof(Document), version.DocumentId.ToString(), $"Deleted version {version.VersionNumber}", ipAddress);

            return ServiceResult<bool>.Ok(true);
        }

        private static bool CanAccessDocument(Document document, string userId)
        {
            if (document.UserId == userId) return true;
            var space = document.CreativeSpace;
            if (space == null) return false;
            return space.OwnerId == userId || space.Permissions.Any(p => p.UserId == userId);
        }

        private static bool CanEditDocument(Document document, string userId)
        {
            if (document.UserId == userId) return true;
            var space = document.CreativeSpace;
            if (space == null) return false;
            if (space.OwnerId == userId) return true;
            return space.Permissions.Any(p => p.UserId == userId && p.PermissionLevel == SpacePermissionLevel.Editor);
        }
    }
}
