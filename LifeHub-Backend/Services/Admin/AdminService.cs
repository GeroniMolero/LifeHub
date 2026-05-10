using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Data.SqlClient;
using LifeHub.Data;
using LifeHub.DTOs;
using LifeHub.Models;
using LifeHub.Utilidades;

namespace LifeHub.Services.Admin
{
    public class AdminService : IAdminService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly BusinessRules _rules;
        private readonly IConfiguration _configuration;

        public AdminService(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IOptions<BusinessRules> rules,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _context = context;
            _rules = rules.Value;
            _configuration = configuration;
        }

        public async Task<List<AdminUserDto>> GetAdminUsersAsync()
        {
            var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();

            // Batch usage queries
            var docCounts = await _context.Documents
                .GroupBy(d => d.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count);

            var spaceCounts = await _context.CreativeSpaces
                .GroupBy(cs => cs.OwnerId)
                .Select(g => new { OwnerId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.OwnerId, x => x.Count);

            var pubStats = await _context.Documents
                .Where(d => d.Publication != null)
                .Select(d => new { d.UserId, d.Publication!.IsProfileVisible })
                .GroupBy(x => x.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Published = g.Count(),
                    ProfileVisible = g.Count(x => x.IsProfileVisible)
                })
                .ToDictionaryAsync(x => x.UserId, x => new { x.Published, x.ProfileVisible });

            var profileVisibleSpaceCounts = await _context.CreativeSpaces
                .Where(cs => cs.IsPublicProfileVisible)
                .GroupBy(cs => cs.OwnerId)
                .Select(g => new { OwnerId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.OwnerId, x => x.Count);

            var result = new List<AdminUserDto>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var claims = await _userManager.GetClaimsAsync(user);

                pubStats.TryGetValue(user.Id, out var pub);
                result.Add(new AdminUserDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    FullName = user.FullName,
                    ProfilePictureUrl = user.ProfilePictureUrl,
                    Bio = user.Bio,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    Roles = roles.ToList(),
                    Claims = claims.Select(c => $"{c.Type}:{c.Value}").ToList(),
                    Usage = new UserUsageDto
                    {
                        DocumentsCount = docCounts.GetValueOrDefault(user.Id),
                        SpacesCount = spaceCounts.GetValueOrDefault(user.Id),
                        PublishedDocumentsCount = pub?.Published ?? 0,
                        ProfileVisibleDocumentsCount = pub?.ProfileVisible ?? 0,
                        ProfileVisibleSpacesCount = profileVisibleSpaceCounts.GetValueOrDefault(user.Id),
                        MaxDocuments = _rules.MaxDocumentsPerUser,
                        MaxSpaces = _rules.MaxSpacesPerUser,
                        MaxPublishedDocuments = _rules.MaxPublishedDocumentsPerUser,
                        MaxProfileVisibleDocuments = _rules.MaxProfileVisibleDocumentsPerUser,
                        MaxProfileVisibleSpaces = _rules.MaxProfileVisibleSpacesPerUser
                    }
                });
            }
            return result;
        }

        public async Task<AdminUserDto> ToggleActiveAsync(string id, string callerUserId)
        {
            if (id == callerUserId)
                throw new InvalidOperationException("No puedes desactivar tu propia cuenta.");

            var user = await _userManager.FindByIdAsync(id)
                ?? throw new KeyNotFoundException("Usuario no encontrado.");

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            return await BuildAdminUserDtoAsync(user);
        }

        public async Task<AdminUserDto> AdminUpdateUserAsync(string id, AdminUpdateUserDto dto, string callerUserId)
        {
            var user = await _userManager.FindByIdAsync(id)
                ?? throw new KeyNotFoundException("Usuario no encontrado.");

            var emailOwner = await _userManager.FindByEmailAsync(dto.Email);
            if (emailOwner != null && emailOwner.Id != id)
                throw new InvalidOperationException("El email ya está en uso por otro usuario.");

            await _userManager.SetEmailAsync(user, dto.Email);
            await _userManager.SetUserNameAsync(user, dto.Email);
            user.FullName = dto.FullName;
            await _userManager.UpdateAsync(user);

            return await BuildAdminUserDtoAsync(user);
        }

        public async Task AdminSetPasswordAsync(string id, AdminSetPasswordDto dto)
        {
            var user = await _userManager.FindByIdAsync(id)
                ?? throw new KeyNotFoundException("Usuario no encontrado.");

            var removeResult = await _userManager.RemovePasswordAsync(user);
            if (!removeResult.Succeeded)
                throw new InvalidOperationException(string.Join(", ", removeResult.Errors.Select(e => e.Description)));

            var addResult = await _userManager.AddPasswordAsync(user, dto.NewPassword);
            if (!addResult.Succeeded)
                throw new InvalidOperationException(string.Join(", ", addResult.Errors.Select(e => e.Description)));
        }

        public async Task<AdminUserDto> AdminUpdateRoleAsync(string id, AdminUpdateRoleDto dto, string callerUserId)
        {
            if (id == callerUserId)
                throw new InvalidOperationException("No puedes cambiar tu propio rol.");

            var user = await _userManager.FindByIdAsync(id)
                ?? throw new KeyNotFoundException("Usuario no encontrado.");

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, dto.Role);

            return await BuildAdminUserDtoAsync(user);
        }

        public async Task<PaginatedResult<ActivityLogDto>> GetActivityLogsAsync(ActivityLogQuery query)
        {
            var q = _context.ActivityLogs
                .Include(l => l.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.UserId))
                q = q.Where(l => l.UserId == query.UserId);

            if (!string.IsNullOrWhiteSpace(query.UserEmail))
                q = q.Where(l => l.User != null && l.User.Email!.Contains(query.UserEmail));

            if (!string.IsNullOrWhiteSpace(query.Action))
                q = q.Where(l => l.Action == query.Action);

            if (!string.IsNullOrWhiteSpace(query.EntityType))
                q = q.Where(l => l.EntityType == query.EntityType);

            if (query.From.HasValue)
                q = q.Where(l => l.CreatedAt >= query.From.Value);

            if (query.To.HasValue)
                q = q.Where(l => l.CreatedAt <= query.To.Value);

            var total = await q.CountAsync();

            var items = await q
                .OrderByDescending(l => l.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(l => new ActivityLogDto
                {
                    Id = l.Id,
                    UserId = l.UserId,
                    UserEmail = l.User != null ? l.User.Email : null,
                    UserFullName = l.User != null ? l.User.FullName : null,
                    Action = l.Action,
                    EntityType = l.EntityType,
                    EntityId = l.EntityId,
                    Details = l.Details,
                    CreatedAt = l.CreatedAt
                })
                .ToListAsync();

            return new PaginatedResult<ActivityLogDto>
            {
                Items = items,
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        public async Task<BackupResultDto> TriggerBackupAsync()
        {
            var connection = _context.Database.GetDbConnection();
            var dbName = connection.Database;

            var backupDir = _configuration["Backup:Directory"] ?? "/var/opt/mssql/backup";
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var backupFile = $"{dbName}_{timestamp}.bak";
            var backupPath = $"{backupDir}/{backupFile}";

            var sql = $"BACKUP DATABASE [{dbName}] TO DISK = N'{backupPath}' WITH FORMAT, INIT, STATS = 10";

            await _context.Database.ExecuteSqlRawAsync(sql);

            return new BackupResultDto
            {
                Message = "Backup completado correctamente.",
                BackupFile = backupPath
            };
        }

        private async Task<AdminUserDto> BuildAdminUserDtoAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var claims = await _userManager.GetClaimsAsync(user);

            var docCount = await _context.Documents.CountAsync(d => d.UserId == user.Id);
            var spaceCount = await _context.CreativeSpaces.CountAsync(cs => cs.OwnerId == user.Id);
            var publishedCount = await _context.Documents.CountAsync(d => d.UserId == user.Id && d.Publication != null);
            var profileVisibleDocsCount = await _context.Documents.CountAsync(d => d.UserId == user.Id && d.Publication != null && d.Publication.IsProfileVisible);
            var profileVisibleSpacesCount = await _context.CreativeSpaces.CountAsync(cs => cs.OwnerId == user.Id && cs.IsPublicProfileVisible);

            return new AdminUserDto
            {
                Id = user.Id,
                Email = user.Email!,
                FullName = user.FullName,
                ProfilePictureUrl = user.ProfilePictureUrl,
                Bio = user.Bio,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                Roles = roles.ToList(),
                Claims = claims.Select(c => $"{c.Type}:{c.Value}").ToList(),
                Usage = new UserUsageDto
                {
                    DocumentsCount = docCount,
                    SpacesCount = spaceCount,
                    PublishedDocumentsCount = publishedCount,
                    ProfileVisibleDocumentsCount = profileVisibleDocsCount,
                    ProfileVisibleSpacesCount = profileVisibleSpacesCount,
                    MaxDocuments = _rules.MaxDocumentsPerUser,
                    MaxSpaces = _rules.MaxSpacesPerUser,
                    MaxPublishedDocuments = _rules.MaxPublishedDocumentsPerUser,
                    MaxProfileVisibleDocuments = _rules.MaxProfileVisibleDocumentsPerUser,
                    MaxProfileVisibleSpaces = _rules.MaxProfileVisibleSpacesPerUser
                }
            };
        }
    }
}
