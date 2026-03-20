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
    public class CreativeSpacesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public CreativeSpacesController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        private string GetUserId() => User.FindFirst("sub")?.Value ?? string.Empty;

        [HttpGet]
        public async Task<IActionResult> GetCreativeSpaces()
        {
            var userId = GetUserId();

            var spaces = await _context.CreativeSpaces
                .Where(cs => cs.OwnerId == userId || cs.Permissions.Any(p => p.UserId == userId))
                .OrderByDescending(cs => cs.UpdatedAt)
                .ToListAsync();

            return Ok(_mapper.Map<List<CreativeSpaceDto>>(spaces));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCreativeSpace(int id)
        {
            var userId = GetUserId();
            var space = await _context.CreativeSpaces
                .Include(cs => cs.Permissions)
                .FirstOrDefaultAsync(cs => cs.Id == id);

            if (space == null)
                return NotFound();

            var canAccess = space.OwnerId == userId || space.Permissions.Any(p => p.UserId == userId);
            if (!canAccess)
                return Forbid();

            return Ok(_mapper.Map<CreativeSpaceDto>(space));
        }

        [HttpPost]
        public async Task<IActionResult> CreateCreativeSpace([FromBody] CreateCreativeSpaceDto dto)
        {
            var userId = GetUserId();

            var space = _mapper.Map<CreativeSpace>(dto);
            space.OwnerId = userId;

            _context.CreativeSpaces.Add(space);
            await _context.SaveChangesAsync();

            await LogActivityAsync(userId, "creative-space.created", nameof(CreativeSpace), space.Id.ToString(), $"Created space '{space.Name}'");

            return Created($"api/creativespaces/{space.Id}", _mapper.Map<CreativeSpaceDto>(space));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateCreativeSpace(int id, [FromBody] UpdateCreativeSpaceDto dto)
        {
            var userId = GetUserId();
            var space = await _context.CreativeSpaces.FirstOrDefaultAsync(cs => cs.Id == id && cs.OwnerId == userId);

            if (space == null)
                return NotFound();

            _mapper.Map(dto, space);
            space.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await LogActivityAsync(userId, "creative-space.updated", nameof(CreativeSpace), space.Id.ToString(), $"Updated space '{space.Name}'");

            return Ok(_mapper.Map<CreativeSpaceDto>(space));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteCreativeSpace(int id)
        {
            var userId = GetUserId();
            var space = await _context.CreativeSpaces.FirstOrDefaultAsync(cs => cs.Id == id && cs.OwnerId == userId);

            if (space == null)
                return NotFound();

            _context.CreativeSpaces.Remove(space);
            await _context.SaveChangesAsync();

            await LogActivityAsync(userId, "creative-space.deleted", nameof(CreativeSpace), id.ToString(), "Deleted creative space");

            return NoContent();
        }

        [HttpGet("{id:int}/permissions")]
        public async Task<IActionResult> GetPermissions(int id)
        {
            var userId = GetUserId();
            var isOwner = await _context.CreativeSpaces.AnyAsync(cs => cs.Id == id && cs.OwnerId == userId);

            if (!isOwner)
                return Forbid();

            var permissions = await _context.SpacePermissions
                .Where(p => p.CreativeSpaceId == id)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return Ok(_mapper.Map<List<SpacePermissionDto>>(permissions));
        }

        [HttpPost("{id:int}/permissions")]
        public async Task<IActionResult> ShareCreativeSpace(int id, [FromBody] ShareCreativeSpaceDto dto)
        {
            var userId = GetUserId();
            var space = await _context.CreativeSpaces.FirstOrDefaultAsync(cs => cs.Id == id && cs.OwnerId == userId);

            if (space == null)
                return NotFound();

            var userExists = await _context.Users.AnyAsync(u => u.Id == dto.UserId);
            if (!userExists)
                return BadRequest(new { message = "El usuario no existe" });

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

            await LogActivityAsync(userId, "creative-space.shared", nameof(CreativeSpace), id.ToString(), $"Shared space with user '{dto.UserId}'");

            return Ok(_mapper.Map<SpacePermissionDto>(permission));
        }

        [HttpDelete("{id:int}/permissions/{targetUserId}")]
        public async Task<IActionResult> RemovePermission(int id, string targetUserId)
        {
            var userId = GetUserId();
            var isOwner = await _context.CreativeSpaces.AnyAsync(cs => cs.Id == id && cs.OwnerId == userId);

            if (!isOwner)
                return Forbid();

            var permission = await _context.SpacePermissions
                .FirstOrDefaultAsync(p => p.CreativeSpaceId == id && p.UserId == targetUserId);

            if (permission == null)
                return NotFound();

            _context.SpacePermissions.Remove(permission);
            await _context.SaveChangesAsync();

            await LogActivityAsync(userId, "creative-space.permission-removed", nameof(CreativeSpace), id.ToString(), $"Removed permission for user '{targetUserId}'");

            return NoContent();
        }

        private async Task LogActivityAsync(string userId, string action, string entityType, string entityId, string details)
        {
            var log = new ActivityLog
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty
            };

            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
