namespace LifeHub.Utilidades
{
    public interface IActivityLogService
    {
        Task LogAsync(string? userId, string action, string entityType, string entityId, string details, string ipAddress, CancellationToken cancellationToken = default);
    }
}
