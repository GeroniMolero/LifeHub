using System.Text.RegularExpressions;
using AutoMapper;
using LifeHub.Data;
using LifeHub.DTOs;
using LifeHub.Models;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Services.Documents
{
    public class DocumentService : IDocumentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public DocumentService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<ServiceResult<List<DocumentDto>>> GetDocumentsAsync(string userId, bool canViewAll)
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

            var documents = await query
                .Include(d => d.User)
                .OrderByDescending(d => d.UpdatedAt)
                .ToListAsync();

            return ServiceResult<List<DocumentDto>>.Ok(_mapper.Map<List<DocumentDto>>(documents));
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

            dto.Content = SanitizeHtml(dto.Content);

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

            const int MaxVersions = 30;
            var versionCount = await _context.DocumentVersions.CountAsync(v => v.DocumentId == id);
            if (versionCount >= MaxVersions)
                return ServiceResult<DocumentDto>.BadRequest(
                    $"Este documento ha alcanzado el límite de {MaxVersions} versiones. Elimina alguna versión antes de guardar.");

            dto.Content = SanitizeHtml(dto.Content);

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

        private static string SanitizeHtml(string content)
        {
            if (string.IsNullOrEmpty(content)) return content;

            content = Regex.Replace(content,
                @"<script\b[^<]*(?:(?!</script>)<[^<]*)*</script>",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            content = Regex.Replace(content,
                @"\son\w+\s*=\s*(""[^""]*""|'[^']*'|[^\s>]*)",
                string.Empty,
                RegexOptions.IgnoreCase);

            content = Regex.Replace(content,
                @"href\s*=\s*(""javascript:[^""]*""|'javascript:[^']*')",
                string.Empty,
                RegexOptions.IgnoreCase);

            return content;
        }
    }
}
