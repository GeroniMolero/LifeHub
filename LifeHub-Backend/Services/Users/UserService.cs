using AutoMapper;
using LifeHub.Data;
using LifeHub.DTOs;
using LifeHub.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Services.Users
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly ApplicationDbContext _context;

        public UserService(UserManager<ApplicationUser> userManager, IMapper mapper, ApplicationDbContext context)
        {
            _userManager = userManager;
            _mapper = mapper;
            _context = context;
        }

        public async Task<ServiceResult<PublicUserDto>> GetUserAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return ServiceResult<PublicUserDto>.NotFound("Usuario no encontrado.");

            return ServiceResult<PublicUserDto>.Ok(_mapper.Map<PublicUserDto>(user));
        }

        public async Task<ServiceResult<UserDto>> GetCurrentUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return ServiceResult<UserDto>.Unauthorized("Sesión inválida. Inicia sesión de nuevo.");

            return ServiceResult<UserDto>.Ok(await MapWithRolesAsync(user));
        }

        public async Task<ServiceResult<List<UserDto>>> GetUsersAsync()
        {
            var users = _userManager.Users.ToList();
            var result = new List<UserDto>();

            foreach (var user in users)
                result.Add(await MapWithRolesAsync(user));

            return ServiceResult<List<UserDto>>.Ok(result);
        }

        public async Task<ServiceResult<List<PublicUserDto>>> SearchUsersAsync(string currentUserId, string? query)
        {
            var q = _userManager.Users.AsNoTracking().Where(u => u.Id != currentUserId);

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.Trim().ToLower();
                q = q.Where(u =>
                    (u.FullName != null && u.FullName.ToLower().Contains(term)) ||
                    (u.Email != null && u.Email.ToLower() == term));
            }

            var users = await q.OrderBy(u => u.FullName ?? u.Email).Take(30).ToListAsync();

            return ServiceResult<List<PublicUserDto>>.Ok(users.Select(u => _mapper.Map<PublicUserDto>(u)).ToList());
        }

        public async Task<ServiceResult<UserDto>> UpdateProfileAsync(string userId, UpdateProfileDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return ServiceResult<UserDto>.Unauthorized("Sesión inválida. Inicia sesión de nuevo.");

            user.FullName = dto.FullName;
            user.Bio = dto.Bio;
            user.ProfilePictureUrl = dto.ProfilePictureUrl;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return ServiceResult<UserDto>.BadRequest("No se pudo actualizar el perfil.");

            return ServiceResult<UserDto>.Ok(_mapper.Map<UserDto>(user));
        }

        public async Task<ServiceResult<bool>> DeleteCurrentUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return ServiceResult<bool>.Unauthorized("Sesión inválida. Inicia sesión de nuevo.");

            await CleanupUserRelationsAsync(userId);

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return ServiceResult<bool>.BadRequest("No se pudo eliminar la cuenta.");

            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> DeleteUserAsync(string id, string callerUserId)
        {
            if (id == callerUserId)
                return ServiceResult<bool>.BadRequest("No puedes eliminar tu propia cuenta desde el panel de administración.");

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return ServiceResult<bool>.NotFound("Usuario no encontrado.");

            await CleanupUserRelationsAsync(id);

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return ServiceResult<bool>.BadRequest("No se pudo eliminar el usuario.");

            return ServiceResult<bool>.Ok(true);
        }

        private async Task CleanupUserRelationsAsync(string userId)
        {
            await _context.SpacePermissions
                .Where(p => p.UserId == userId || p.GrantedByUserId == userId)
                .ExecuteDeleteAsync();

            await _context.Friendships
                .Where(f => f.RequesterId == userId || f.ReceiverId == userId)
                .ExecuteDeleteAsync();

            await _context.Messages
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .ExecuteDeleteAsync();

            await _context.RecommendationRatings
                .Where(r => r.UserId == userId)
                .ExecuteDeleteAsync();

            await _context.DocumentVersions
                .Where(v => v.CreatedByUserId == userId && v.Document.UserId != userId)
                .ExecuteDeleteAsync();

            await _context.DocumentPublications
                .Where(p => p.PublishedByUserId == userId && p.Document.UserId != userId)
                .ExecuteDeleteAsync();
        }

        public async Task<ServiceResult<bool>> ChangePasswordAsync(string userId, ChangePasswordDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return ServiceResult<bool>.Unauthorized("Sesión inválida. Inicia sesión de nuevo.");

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
                return ServiceResult<bool>.BadRequest("No se pudo cambiar la contraseña. Revisa tus credenciales.");

            return ServiceResult<bool>.Ok(true);
        }

        private async Task<UserDto> MapWithRolesAsync(ApplicationUser user)
        {
            var dto = _mapper.Map<UserDto>(user);
            var roles = await _userManager.GetRolesAsync(user);
            var claims = await _userManager.GetClaimsAsync(user);
            dto.Roles = roles.ToList();
            dto.Claims = claims.Select(c => $"{c.Type}:{c.Value}").ToList();
            return dto;
        }
    }
}
