using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

            var document = await _context.Documents.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

            if (document == null)
                return NotFoundError("Documento no encontrado.");

            _mapper.Map(dto, document);
            document.UpdatedAt = DateTime.UtcNow;

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

            var document = await _context.Documents.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

            if (document == null)
                return NotFoundError("Documento no encontrado.");

            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool HasPermission(string value)
        {
            return User.HasClaim("permission", value);
        }
    }
}
