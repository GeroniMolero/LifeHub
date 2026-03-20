using AutoMapper;
using LifeHub.Data;
using LifeHub.DTOs;
using LifeHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DocumentVersionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public DocumentVersionsController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        private string GetUserId() => User.FindFirst("sub")?.Value ?? string.Empty;

        [HttpGet("document/{documentId:int}")]
        public async Task<IActionResult> GetDocumentVersions(int documentId)
        {
            var userId = GetUserId();
            var document = await _context.Documents
                .Include(d => d.CreativeSpace)
                    .ThenInclude(cs => cs!.Permissions)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null)
                return NotFound();

            if (!CanAccessDocument(document, userId))
                return Forbid();

            var versions = await _context.DocumentVersions
                .Where(v => v.DocumentId == documentId)
                .OrderByDescending(v => v.VersionNumber)
                .ToListAsync();

            return Ok(_mapper.Map<List<DocumentVersionDto>>(versions));
        }

        [HttpPost("document/{documentId:int}/snapshot")]
        public async Task<IActionResult> CreateSnapshot(int documentId, [FromBody] CreateDocumentVersionDto dto)
        {
            var userId = GetUserId();
            var document = await _context.Documents
                .Include(d => d.CreativeSpace)
                    .ThenInclude(cs => cs!.Permissions)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null)
                return NotFound();

            if (!CanEditDocument(document, userId))
                return Forbid();

            var nextVersion = await _context.DocumentVersions
                .Where(v => v.DocumentId == documentId)
                .Select(v => v.VersionNumber)
                .DefaultIfEmpty(0)
                .MaxAsync() + 1;

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

            await LogActivityAsync(userId, "document.version-created", nameof(Document), document.Id.ToString(), $"Created version {nextVersion}");

            return Created($"api/documentversions/{version.Id}", _mapper.Map<DocumentVersionDto>(version));
        }

        [HttpPost("{versionId:int}/restore")]
        public async Task<IActionResult> RestoreVersion(int versionId)
        {
            var userId = GetUserId();
            var version = await _context.DocumentVersions
                .Include(v => v.Document)
                    .ThenInclude(d => d.CreativeSpace)
                        .ThenInclude(cs => cs!.Permissions)
                .FirstOrDefaultAsync(v => v.Id == versionId);

            if (version == null)
                return NotFound();

            if (!CanEditDocument(version.Document, userId))
                return Forbid();

            version.Document.Title = version.Title;
            version.Document.Description = version.Description;
            version.Document.Content = version.Content;
            version.Document.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await LogActivityAsync(userId, "document.version-restored", nameof(Document), version.DocumentId.ToString(), $"Restored version {version.VersionNumber}");

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

        private async Task LogActivityAsync(string userId, string action, string entityType, string entityId, string details)
        {
            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty
            });

            await _context.SaveChangesAsync();
        }
    }
}
