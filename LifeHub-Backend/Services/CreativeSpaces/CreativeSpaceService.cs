using System.Text.Json;
using AutoMapper;
using LifeHub.Data;
using LifeHub.DTOs;
using LifeHub.Models;
using LifeHub.Utilidades;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Services.CreativeSpaces
{
    public class CreativeSpaceService : ICreativeSpaceService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IActivityLogService _activityLogService;

        public CreativeSpaceService(ApplicationDbContext context, IMapper mapper, IActivityLogService activityLogService)
        {
            _context = context;
            _mapper = mapper;
            _activityLogService = activityLogService;
        }

        public async Task<ServiceResult<List<CreativeSpaceDto>>> GetCreativeSpacesAsync(string userId)
        {
            var spaces = await _context.CreativeSpaces
                .Where(cs => cs.OwnerId == userId || cs.Permissions.Any(p => p.UserId == userId))
                .OrderByDescending(cs => cs.UpdatedAt)
                .ToListAsync();

            return ServiceResult<List<CreativeSpaceDto>>.Ok(_mapper.Map<List<CreativeSpaceDto>>(spaces));
        }

        public async Task<ServiceResult<CreativeSpaceDto>> GetCreativeSpaceAsync(int id, string userId)
        {
            var space = await _context.CreativeSpaces
                .Include(cs => cs.Permissions)
                .FirstOrDefaultAsync(cs => cs.Id == id);

            if (space == null)
                return ServiceResult<CreativeSpaceDto>.NotFound("Espacio creativo no encontrado.");

            var canAccess = space.OwnerId == userId || space.Permissions.Any(p => p.UserId == userId);
            if (!canAccess)
                return ServiceResult<CreativeSpaceDto>.Forbidden("No tienes permisos para acceder a este espacio creativo.");

            return ServiceResult<CreativeSpaceDto>.Ok(_mapper.Map<CreativeSpaceDto>(space));
        }

        public async Task<ServiceResult<CreativeSpaceDto>> CreateCreativeSpaceAsync(string userId, CreateCreativeSpaceDto dto, string ipAddress)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
                return ServiceResult<CreativeSpaceDto>.Unauthorized("Sesión inválida. Inicia sesión de nuevo.");

            var space = _mapper.Map<CreativeSpace>(dto);
            space.OwnerId = userId;

            _context.CreativeSpaces.Add(space);
            await _context.SaveChangesAsync();

            await _activityLogService.LogAsync(userId, "creative-space.created", nameof(CreativeSpace), space.Id.ToString(), $"Created space '{space.Name}'", ipAddress);

            return ServiceResult<CreativeSpaceDto>.Ok(_mapper.Map<CreativeSpaceDto>(space));
        }

        public async Task<ServiceResult<CreativeSpaceDto>> UpdateCreativeSpaceAsync(int id, string userId, UpdateCreativeSpaceDto dto, string ipAddress)
        {
            var space = await _context.CreativeSpaces.FirstOrDefaultAsync(cs => cs.Id == id && cs.OwnerId == userId);

            if (space == null)
                return ServiceResult<CreativeSpaceDto>.NotFound("Espacio creativo no encontrado.");

            _mapper.Map(dto, space);
            space.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _activityLogService.LogAsync(userId, "creative-space.updated", nameof(CreativeSpace), space.Id.ToString(), $"Updated space '{space.Name}'", ipAddress);

            return ServiceResult<CreativeSpaceDto>.Ok(_mapper.Map<CreativeSpaceDto>(space));
        }

        public async Task<ServiceResult<bool>> DeleteCreativeSpaceAsync(int id, string userId, string ipAddress)
        {
            var space = await _context.CreativeSpaces.FirstOrDefaultAsync(cs => cs.Id == id && cs.OwnerId == userId);

            if (space == null)
                return ServiceResult<bool>.NotFound("Espacio creativo no encontrado.");

            var linkedDocuments = await _context.Documents.Where(d => d.CreativeSpaceId == id).ToListAsync();
            foreach (var doc in linkedDocuments)
            {
                doc.CreativeSpaceId = null;
                doc.UpdatedAt = DateTime.UtcNow;
            }

            _context.CreativeSpaces.Remove(space);
            await _context.SaveChangesAsync();

            await _activityLogService.LogAsync(userId, "creative-space.deleted", nameof(CreativeSpace), id.ToString(), "Deleted creative space", ipAddress);

            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> SetFavoriteAsync(int id, string userId, bool isFavorite)
        {
            var space = await _context.CreativeSpaces.FirstOrDefaultAsync(cs => cs.Id == id && cs.OwnerId == userId);

            if (space == null)
                return ServiceResult<bool>.NotFound("Espacio creativo no encontrado.");

            if (space.IsFavorite != isFavorite)
            {
                space.IsFavorite = isFavorite;
                space.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<List<SpacePermissionDto>>> GetPermissionsAsync(int id, string userId)
        {
            var isOwner = await _context.CreativeSpaces.AnyAsync(cs => cs.Id == id && cs.OwnerId == userId);

            if (!isOwner)
                return ServiceResult<List<SpacePermissionDto>>.Forbidden("Solo el propietario puede ver permisos del espacio.");

            var permissions = await _context.SpacePermissions
                .Include(p => p.User)
                .Where(p => p.CreativeSpaceId == id)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return ServiceResult<List<SpacePermissionDto>>.Ok(_mapper.Map<List<SpacePermissionDto>>(permissions));
        }

        public async Task<ServiceResult<SpacePermissionDto>> ShareAsync(int id, string userId, ShareCreativeSpaceDto dto, string ipAddress)
        {
            var space = await _context.CreativeSpaces.FirstOrDefaultAsync(cs => cs.Id == id && cs.OwnerId == userId);

            if (space == null)
                return ServiceResult<SpacePermissionDto>.NotFound("Espacio creativo no encontrado.");

            var userExists = await _context.Users.AnyAsync(u => u.Id == dto.UserId);
            if (!userExists)
                return ServiceResult<SpacePermissionDto>.BadRequest("El usuario objetivo no existe.");

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

            await _activityLogService.LogAsync(userId, "creative-space.shared", nameof(CreativeSpace), id.ToString(), $"Shared space with user '{dto.UserId}'", ipAddress);

            await _context.Entry(permission).Reference(p => p.User).LoadAsync();

            return ServiceResult<SpacePermissionDto>.Ok(_mapper.Map<SpacePermissionDto>(permission));
        }

        public async Task<ServiceResult<bool>> RemovePermissionAsync(int id, string userId, string targetUserId, string ipAddress)
        {
            var isOwner = await _context.CreativeSpaces.AnyAsync(cs => cs.Id == id && cs.OwnerId == userId);

            if (!isOwner)
                return ServiceResult<bool>.Forbidden("Solo el propietario puede eliminar permisos del espacio.");

            var permission = await _context.SpacePermissions
                .FirstOrDefaultAsync(p => p.CreativeSpaceId == id && p.UserId == targetUserId);

            if (permission == null)
                return ServiceResult<bool>.NotFound("Permiso no encontrado para el usuario indicado.");

            _context.SpacePermissions.Remove(permission);
            await _context.SaveChangesAsync();

            await _activityLogService.LogAsync(userId, "creative-space.permission-removed", nameof(CreativeSpace), id.ToString(), $"Removed permission for user '{targetUserId}'", ipAddress);

            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<List<SpaceMediaReferenceDto>>> GetMediaReferencesAsync(int id, string userId)
        {
            var space = await _context.CreativeSpaces
                .Include(cs => cs.Permissions)
                .FirstOrDefaultAsync(cs => cs.Id == id);

            if (space == null)
                return ServiceResult<List<SpaceMediaReferenceDto>>.NotFound("Espacio creativo no encontrado.");

            var canAccess = space.OwnerId == userId || space.Permissions.Any(p => p.UserId == userId);
            if (!canAccess)
                return ServiceResult<List<SpaceMediaReferenceDto>>.Forbidden("No tienes permisos para acceder a este espacio creativo.");

            return ServiceResult<List<SpaceMediaReferenceDto>>.Ok(DeserializeMediaReferences(space.MediaReferencesJson));
        }

        public async Task<ServiceResult<SpaceMediaReferenceDto>> AddMediaReferenceAsync(int id, string userId, CreateSpaceMediaReferenceDto dto)
        {
            var space = await _context.CreativeSpaces
                .Include(cs => cs.Permissions)
                .FirstOrDefaultAsync(cs => cs.Id == id);

            if (space == null)
                return ServiceResult<SpaceMediaReferenceDto>.NotFound("Espacio creativo no encontrado.");

            var canEdit = space.OwnerId == userId ||
                space.Permissions.Any(p => p.UserId == userId && p.PermissionLevel == SpacePermissionLevel.Editor);
            if (!canEdit)
                return ServiceResult<SpaceMediaReferenceDto>.Forbidden("No tienes permisos de edición para este espacio creativo.");

            if (string.IsNullOrWhiteSpace(dto.EmbedUrl) || !IsHttpUrl(dto.EmbedUrl))
                return ServiceResult<SpaceMediaReferenceDto>.BadRequest("El enlace embed debe ser una URL http(s) válida.");

            if (!Uri.TryCreate(dto.EmbedUrl.Trim(), UriKind.Absolute, out var embedUri))
                return ServiceResult<SpaceMediaReferenceDto>.BadRequest("El enlace embed debe ser una URL válida.");

            var allowedDomains = await _context.AllowedWebsites
                .Where(w => w.IsActive)
                .Select(w => w.Domain)
                .ToListAsync();

            var host = DomainHelper.NormalizeHost(embedUri.Host);
            var isAllowed = allowedDomains.Any(domain => host == domain || host.EndsWith($".{domain}"));
            if (!isAllowed)
                return ServiceResult<SpaceMediaReferenceDto>.BadRequest("El dominio del enlace embed no está permitido.");

            var references = DeserializeMediaReferences(space.MediaReferencesJson);

            var newReference = new SpaceMediaReferenceDto
            {
                Id = Guid.NewGuid().ToString("N"),
                Type = "external-embed",
                Label = string.IsNullOrWhiteSpace(dto.Label) ? "Enlace" : dto.Label.Trim(),
                Source = string.IsNullOrWhiteSpace(dto.Source) ? dto.EmbedUrl.Trim() : dto.Source.Trim(),
                Provider = string.IsNullOrWhiteSpace(dto.Provider) ? null : dto.Provider.Trim(),
                EmbedUrl = dto.EmbedUrl.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            references.Insert(0, newReference);
            space.MediaReferencesJson = JsonSerializer.Serialize(references);
            space.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ServiceResult<SpaceMediaReferenceDto>.Ok(newReference);
        }

        public async Task<ServiceResult<bool>> RemoveMediaReferenceAsync(int id, string userId, string referenceId)
        {
            var space = await _context.CreativeSpaces
                .Include(cs => cs.Permissions)
                .FirstOrDefaultAsync(cs => cs.Id == id);

            if (space == null)
                return ServiceResult<bool>.NotFound("Espacio creativo no encontrado.");

            var canEdit = space.OwnerId == userId ||
                space.Permissions.Any(p => p.UserId == userId && p.PermissionLevel == SpacePermissionLevel.Editor);
            if (!canEdit)
                return ServiceResult<bool>.Forbidden("No tienes permisos de edición para este espacio creativo.");

            var references = DeserializeMediaReferences(space.MediaReferencesJson);
            var updated = references.Where(r => !string.Equals(r.Id, referenceId, StringComparison.Ordinal)).ToList();

            if (updated.Count == references.Count)
                return ServiceResult<bool>.NotFound("Referencia multimedia no encontrada.");

            space.MediaReferencesJson = JsonSerializer.Serialize(updated);
            space.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Ok(true);
        }

        private static bool IsHttpUrl(string value) =>
            Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

        private static List<SpaceMediaReferenceDto> DeserializeMediaReferences(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<SpaceMediaReferenceDto>();
            try { return JsonSerializer.Deserialize<List<SpaceMediaReferenceDto>>(json) ?? new List<SpaceMediaReferenceDto>(); }
            catch { return new List<SpaceMediaReferenceDto>(); }
        }
    }
}
