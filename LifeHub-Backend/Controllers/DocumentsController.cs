using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using AutoMapper;
using LifeHub.Data;
using LifeHub.DTOs;
using LifeHub.Models;
using LifeHub.Utilidades;

namespace LifeHub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DocumentsController : ApiControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public DocumentsController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetDocuments()
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var canViewAllDocuments = HasPermission("documents.view.all");

            var query = _context.Documents.AsQueryable();

            if (!canViewAllDocuments)
            {
                query = query.Where(d =>
                    d.UserId == userId ||
                    (d.CreativeSpaceId.HasValue &&
                     _context.SpacePermissions.Any(p => p.CreativeSpaceId == d.CreativeSpaceId.Value && p.UserId == userId))
                );
            }

            var documents = await query
                .Include(d => d.User)
                .OrderByDescending(d => d.UpdatedAt)
                .ToListAsync();

            return Ok(_mapper.Map<List<DocumentDto>>(documents));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDocument(int id)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var canViewAllDocuments = HasPermission("documents.view.all");

            var query = _context.Documents
                .Include(d => d.User)
                .Where(d => d.Id == id);

            if (!canViewAllDocuments)
            {
                query = query.Where(d =>
                    d.UserId == userId ||
                    (d.CreativeSpaceId.HasValue &&
                     _context.SpacePermissions.Any(p => p.CreativeSpaceId == d.CreativeSpaceId.Value && p.UserId == userId))
                );
            }

            var document = await query.FirstOrDefaultAsync();

            if (document == null)
                return NotFoundError("Documento no encontrado.");

            return Ok(_mapper.Map<DocumentDto>(document));
        }

        [HttpPost]
        public async Task<IActionResult> CreateDocument([FromBody] CreateDocumentDto dto)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var sessionError = await EnsureActiveSessionAsync(_context, userId);
            if (sessionError != null)
                return sessionError;

            dto.Content = SanitizeHtml(dto.Content);

            var document = _mapper.Map<Document>(dto);
            document.UserId = userId;

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            var createdDocument = await _context.Documents
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == document.Id);

            if (createdDocument == null)
                return Created($"api/documents/{document.Id}", _mapper.Map<DocumentDto>(document));

            return Created($"api/documents/{document.Id}", _mapper.Map<DocumentDto>(createdDocument));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDocument(int id, [FromBody] UpdateDocumentDto dto)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var document = await _context.Documents.FirstOrDefaultAsync(d => d.Id == id);

            if (document == null)
                return NotFoundError("Documento no encontrado.");

            var isOwner = document.UserId == userId;
            var isSpaceEditor = document.CreativeSpaceId.HasValue &&
                await _context.SpacePermissions.AnyAsync(p =>
                    p.CreativeSpaceId == document.CreativeSpaceId.Value &&
                    p.UserId == userId &&
                    p.PermissionLevel == SpacePermissionLevel.Editor);

            if (!isOwner && !isSpaceEditor)
                return ForbiddenError("No tienes permiso para editar este documento.");

            const int MaxVersions = 30;
            var versionCount = await _context.DocumentVersions.CountAsync(v => v.DocumentId == id);
            if (versionCount >= MaxVersions)
                return BadRequestError($"Este documento ha alcanzado el límite de {MaxVersions} versiones. Elimina alguna versión antes de guardar.");

            dto.Content = SanitizeHtml(dto.Content);

            _mapper.Map(dto, document);
            document.UpdatedAt = DateTime.UtcNow;

            var lastVersion = await _context.DocumentVersions
                .Where(v => v.DocumentId == id)
                .OrderByDescending(v => v.VersionNumber)
                .Select(v => (int?)v.VersionNumber)
                .FirstOrDefaultAsync();

            var newVersion = new DocumentVersion
            {
                DocumentId = document.Id,
                VersionNumber = (lastVersion ?? 0) + 1,
                Title = document.Title,
                Description = document.Description,
                Content = document.Content,
                CreatedByUserId = userId
            };

            _context.DocumentVersions.Add(newVersion);
            await _context.SaveChangesAsync();

            var updatedDocument = await _context.Documents
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == document.Id);

            if (updatedDocument == null)
                return Ok(_mapper.Map<DocumentDto>(document));

            return Ok(_mapper.Map<DocumentDto>(updatedDocument));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var document = await _context.Documents.FirstOrDefaultAsync(d => d.Id == id);

            if (document == null)
                return NotFoundError("Documento no encontrado.");

            if (document.UserId != userId)
                return ForbiddenError("Solo el propietario del documento puede eliminarlo.");

            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool HasPermission(string value)
        {
            return User.HasClaim("permission", value);
        }

        private static string SanitizeHtml(string content)
        {
            if (string.IsNullOrEmpty(content)) return content;

            // Remove <script>...</script> blocks
            content = Regex.Replace(content,
                @"<script\b[^<]*(?:(?!</script>)<[^<]*)*</script>",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            // Remove on* event handlers (e.g. onclick=, onerror=)
            content = Regex.Replace(content,
                @"\son\w+\s*=\s*(""[^""]*""|'[^']*'|[^\s>]*)",
                string.Empty,
                RegexOptions.IgnoreCase);

            // Remove javascript: URIs
            content = Regex.Replace(content,
                @"href\s*=\s*(""javascript:[^""]*""|'javascript:[^']*')",
                string.Empty,
                RegexOptions.IgnoreCase);

            return content;
        }
    }
}
