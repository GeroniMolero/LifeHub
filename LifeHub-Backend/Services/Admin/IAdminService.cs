using LifeHub.DTOs;

namespace LifeHub.Services.Admin
{
    public interface IAdminService
    {
        Task<PaginatedResult<AdminUserDto>> GetAdminUsersAsync(int page = 1, int pageSize = 20);
        Task<AdminUserDto> ToggleActiveAsync(string id, string callerUserId);
        Task<AdminUserDto> AdminUpdateUserAsync(string id, AdminUpdateUserDto dto, string callerUserId);
        Task AdminSetPasswordAsync(string id, AdminSetPasswordDto dto);
        Task<AdminUserDto> AdminUpdateRoleAsync(string id, AdminUpdateRoleDto dto, string callerUserId);
        Task<PaginatedResult<ActivityLogDto>> GetActivityLogsAsync(ActivityLogQuery query);
        Task<BackupResultDto> TriggerBackupAsync();
    }
}
