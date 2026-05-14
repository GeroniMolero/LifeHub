using System.ComponentModel.DataAnnotations;

namespace LifeHub.DTOs
{
    public class AdminUserDto
    {
        public string Id { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? FullName { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string? Bio { get; set; }
        public List<string> Roles { get; set; } = new();
        public List<string> Claims { get; set; } = new();
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public UserUsageDto Usage { get; set; } = new();
    }

    public class UserUsageDto
    {
        public int DocumentsCount { get; set; }
        public int SpacesCount { get; set; }
        public int PublishedDocumentsCount { get; set; }
        public int ProfileVisibleDocumentsCount { get; set; }
        public int ProfileVisibleSpacesCount { get; set; }
        public int MaxDocuments { get; set; }
        public int MaxSpaces { get; set; }
        public int MaxPublishedDocuments { get; set; }
        public int MaxProfileVisibleDocuments { get; set; }
        public int MaxProfileVisibleSpaces { get; set; }
    }

    public class AdminUpdateUserDto
    {
        [Required, EmailAddress, MaxLength(256)]
        public string Email { get; set; } = null!;

        [MaxLength(100)]
        public string? FullName { get; set; }
    }

    public class AdminSetPasswordDto
    {
        [Required, MinLength(10), MaxLength(128)]
        public string NewPassword { get; set; } = null!;
    }

    public class AdminUpdateRoleDto
    {
        [Required]
        public string Role { get; set; } = null!;
    }

    public class ActivityLogDto
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? UserEmail { get; set; }
        public string? UserFullName { get; set; }
        public string Action { get; set; } = null!;
        public string EntityType { get; set; } = null!;
        public string? EntityId { get; set; }
        public string Details { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class ActivityLogQuery
    {
        public string? UserId { get; set; }
        public string? UserEmail { get; set; }
        public string? Action { get; set; }
        public string? EntityType { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class BackupResultDto
    {
        public string Message { get; set; } = null!;
        public string? BackupFile { get; set; }
    }
}
