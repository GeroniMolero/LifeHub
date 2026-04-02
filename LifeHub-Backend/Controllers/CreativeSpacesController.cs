using AutoMapper;
using LifeHub.Data;
using LifeHub.DTOs;
using LifeHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LifeHub.Utilidades;

namespace LifeHub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CreativeSpacesController : ApiControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IActivityLogService _activityLogService;

        public CreativeSpacesController(ApplicationDbContext context, IMapper mapper, IActivityLogService activityLogService)
        {
            _context = context;
            _mapper = mapper;
            _activityLogService = activityLogService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCreativeSpaces()
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var spaces = await _context.CreativeSpaces
                .Where(cs => cs.OwnerId == userId || cs.Permissions.Any(p => p.UserId == userId))
                .OrderByDescending(cs => cs.UpdatedAt)
                .ToListAsync();

            return Ok(_mapper.Map<List<CreativeSpaceDto>>(spaces));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCreativeSpace(int id)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var space = await _context.CreativeSpaces
                .Include(cs => cs.Permissions)
                .FirstOrDefaultAsync(cs => cs.Id == id);

            if (space == null)
                return NotFoundError("Espacio creativo no encontrado.");

            var canAccess = space.OwnerId == userId || space.Permissions.Any(p => p.UserId == userId);
            if (!canAccess)
                return ForbiddenError("No tienes permisos para acceder a este espacio creativo.");

            return Ok(_mapper.Map<CreativeSpaceDto>(space));
        }

        [HttpPost]
        public async Task<IActionResult> CreateCreativeSpace([FromBody] CreateCreativeSpaceDto dto)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var sessionError = await EnsureActiveSessionAsync(_context, userId);
            if (sessionError != null)
                return sessionError;

            var space = _mapper.Map<CreativeSpace>(dto);
            space.OwnerId = userId;

            _context.CreativeSpaces.Add(space);
            await _context.SaveChangesAsync();

            await _activityLogService.LogAsync(
                userId,
                "creative-space.created",
                nameof(CreativeSpace),
                space.Id.ToString(),
                $"Created space '{space.Name}'",
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty);

            return Created($"api/creativespaces/{space.Id}", _mapper.Map<CreativeSpaceDto>(space));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateCreativeSpace(int id, [FromBody] UpdateCreativeSpaceDto dto)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var space = await _context.CreativeSpaces.FirstOrDefaultAsync(cs => cs.Id == id && cs.OwnerId == userId);

            if (space == null)
                return NotFoundError("Espacio creativo no encontrado.");

            _mapper.Map(dto, space);
            space.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _activityLogService.LogAsync(
                userId,
                "creative-space.updated",
                nameof(CreativeSpace),
                space.Id.ToString(),
                $"Updated space '{space.Name}'",
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty);

            return Ok(_mapper.Map<CreativeSpaceDto>(space));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteCreativeSpace(int id)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var space = await _context.CreativeSpaces.FirstOrDefaultAsync(cs => cs.Id == id && cs.OwnerId == userId);

            if (space == null)
                return NotFoundError("Espacio creativo no encontrado.");

            var linkedDocuments = await _context.Documents
                .Where(d => d.CreativeSpaceId == id)
                .ToListAsync();

            foreach (var document in linkedDocuments)
            {
                document.CreativeSpaceId = null;
                document.UpdatedAt = DateTime.UtcNow;
            }

            _context.CreativeSpaces.Remove(space);
            await _context.SaveChangesAsync();

            await _activityLogService.LogAsync(
                userId,
                "creative-space.deleted",
                nameof(CreativeSpace),
                id.ToString(),
                "Deleted creative space",
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty);

            return NoContent();
        }

        [HttpGet("{id:int}/permissions")]
        public async Task<IActionResult> GetPermissions(int id)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var isOwner = await _context.CreativeSpaces.AnyAsync(cs => cs.Id == id && cs.OwnerId == userId);

            if (!isOwner)
                return ForbiddenError("Solo el propietario puede ver permisos del espacio.");

            var permissions = await _context.SpacePermissions
                .Where(p => p.CreativeSpaceId == id)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return Ok(_mapper.Map<List<SpacePermissionDto>>(permissions));
        }

        [HttpPost("{id:int}/permissions")]
        public async Task<IActionResult> ShareCreativeSpace(int id, [FromBody] ShareCreativeSpaceDto dto)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var space = await _context.CreativeSpaces.FirstOrDefaultAsync(cs => cs.Id == id && cs.OwnerId == userId);

            if (space == null)
                return NotFoundError("Espacio creativo no encontrado.");

            var userExists = await _context.Users.AnyAsync(u => u.Id == dto.UserId);
            if (!userExists)
                return BadRequestError("El usuario objetivo no existe.");

            var permission = await _context.SpacePermissions
                .FirstOrDefaultAsync(p => p.CreativeSpaceId == id && p.UserId == dto.UserId);

            if (permission == null)
            {
                permission = new SpacePermission
                {
                    CreativeSpaceId = id,
                    UserId = dto.UserId,
                    GrantedByUserId = userId,
                    PermissionLevel = (SpacePermissionLevel)dto.PermissionLevel
                };

                _context.SpacePermissions.Add(permission);
            }
            else
            {
                permission.PermissionLevel = (SpacePermissionLevel)dto.PermissionLevel;
            }

            if (space.Privacy == SpacePrivacy.Private)
                space.Privacy = SpacePrivacy.Shared;

            await _context.SaveChangesAsync();

            await _activityLogService.LogAsync(
                userId,
                "creative-space.shared",
                nameof(CreativeSpace),
                id.ToString(),
                $"Shared space with user '{dto.UserId}'",
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty);

            return Ok(_mapper.Map<SpacePermissionDto>(permission));
        }

        [HttpDelete("{id:int}/permissions/{targetUserId}")]
        public async Task<IActionResult> RemovePermission(int id, string targetUserId)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var isOwner = await _context.CreativeSpaces.AnyAsync(cs => cs.Id == id && cs.OwnerId == userId);

            if (!isOwner)
                return ForbiddenError("Solo el propietario puede eliminar permisos del espacio.");

            var permission = await _context.SpacePermissions
                .FirstOrDefaultAsync(p => p.CreativeSpaceId == id && p.UserId == targetUserId);

            if (permission == null)
                return NotFoundError("Permiso no encontrado para el usuario indicado.");

            _context.SpacePermissions.Remove(permission);
            await _context.SaveChangesAsync();

            await _activityLogService.LogAsync(
                userId,
                "creative-space.permission-removed",
                nameof(CreativeSpace),
                id.ToString(),
                $"Removed permission for user '{targetUserId}'",
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty);

            return NoContent();
        }
    }
}
