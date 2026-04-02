using LifeHub.Data;
using LifeHub.Models;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Utilidades
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ActivityLogService> _logger;

        public ActivityLogService(ApplicationDbContext context, ILogger<ActivityLogService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogAsync(string? userId, string action, string entityType, string entityId, string details, string ipAddress, CancellationToken cancellationToken = default)
        {
            try
            {
                string? normalizedUserId = null;
                if (!string.IsNullOrWhiteSpace(userId))
                    normalizedUserId = await _context.Users.AnyAsync(u => u.Id == userId, cancellationToken) ? userId : null;

                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserId = normalizedUserId,
                    Action = action,
                    EntityType = entityType,
                    EntityId = entityId,
                    Details = details,
                    IpAddress = ipAddress
                });

                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to persist activity log for action {Action} and entity {EntityType}:{EntityId}", action, entityType, entityId);
            }
        }
    }
}
