using AutoMapper;
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
    [Route("api/[controller]")]
    [Authorize]
    public class DocumentVersionsController : ApiControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IActivityLogService _activityLogService;

        public DocumentVersionsController(ApplicationDbContext context, IMapper mapper, IActivityLogService activityLogService)
        {
            _context = context;
            _mapper = mapper;
            _activityLogService = activityLogService;
        }

        [HttpGet("document/{documentId:int}")]
        public async Task<IActionResult> GetDocumentVersions(int documentId)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var document = await _context.Documents
                .Include(d => d.CreativeSpace)
                    .ThenInclude(cs => cs!.Permissions)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null)
                return NotFoundError("Documento no encontrado.");

            if (!CanAccessDocument(document, userId))
                return ForbiddenError("No tienes permisos para ver las versiones de este documento.");

            var versions = await _context.DocumentVersions
                .Where(v => v.DocumentId == documentId)
                .OrderByDescending(v => v.VersionNumber)
                .ToListAsync();

            return Ok(_mapper.Map<List<DocumentVersionDto>>(versions));
        }

        [HttpPost("document/{documentId:int}/snapshot")]
        public async Task<IActionResult> CreateSnapshot(int documentId, [FromBody] CreateDocumentVersionDto dto)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var document = await _context.Documents
                .Include(d => d.CreativeSpace)
                    .ThenInclude(cs => cs!.Permissions)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null)
                return NotFoundError("Documento no encontrado.");

            if (!CanEditDocument(document, userId))
                return ForbiddenError("No tienes permisos para crear versiones de este documento.");

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

            await _activityLogService.LogAsync(
                userId,
                "document.version-created",
                nameof(Document),
                document.Id.ToString(),
                $"Created version {nextVersion}",
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty);

            return Created($"api/documentversions/{version.Id}", _mapper.Map<DocumentVersionDto>(version));
        }

        [HttpPost("{versionId:int}/restore")]
        public async Task<IActionResult> RestoreVersion(int versionId)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var version = await _context.DocumentVersions
                .Include(v => v.Document)
                    .ThenInclude(d => d.CreativeSpace)
                        .ThenInclude(cs => cs!.Permissions)
                .FirstOrDefaultAsync(v => v.Id == versionId);

            if (version == null)
                return NotFoundError("Versión de documento no encontrada.");

            if (!CanEditDocument(version.Document, userId))
                return ForbiddenError("No tienes permisos para restaurar esta versión.");

            version.Document.Title = version.Title;
            version.Document.Description = version.Description;
            version.Document.Content = version.Content;
            version.Document.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _activityLogService.LogAsync(
                userId,
                "document.version-restored",
                nameof(Document),
                version.DocumentId.ToString(),
                $"Restored version {version.VersionNumber}",
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty);

            return Ok(new { message = "Versión restaurada", documentId = version.DocumentId, restoredVersion = version.VersionNumber });
        }

        private static bool CanAccessDocument(Document document, string userId)
        {
            if (document.UserId == userId)
                return true;

            var space = document.CreativeSpace;
            if (space == null)
                return false;

            return space.OwnerId == userId || space.Permissions.Any(p => p.UserId == userId);
        }

        private static bool CanEditDocument(Document document, string userId)
        {
            if (document.UserId == userId)
                return true;

            var space = document.CreativeSpace;
            if (space == null)
                return false;

            if (space.OwnerId == userId)
                return true;

            return space.Permissions.Any(p => p.UserId == userId && p.PermissionLevel == SpacePermissionLevel.Editor);
        }

    }
}
