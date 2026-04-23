using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using LifeHub.DTOs;
using LifeHub.Models;
using LifeHub.Utilidades;

namespace LifeHub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ApiControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;

        public UsersController(UserManager<ApplicationUser> userManager, IMapper mapper)
        {
            _userManager = userManager;
            _mapper = mapper;
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFoundError("Usuario no encontrado.");

            return Ok(_mapper.Map<UserDto>(user));
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return UnauthorizedError("Sesión inválida. Inicia sesión de nuevo.");

            return Ok(await MapUserDtoAsync(user));
        }

        [HttpGet]
        [Authorize(Policy = "CanViewAdmin")]
        public async Task<IActionResult> GetUsers()
        {
            var users = _userManager.Users.ToList();
            var result = new List<UserDto>();

            foreach (var user in users)
            {
                result.Add(await MapUserDtoAsync(user));
            }

            return Ok(result);
        }

        [HttpGet("search")]
        [Authorize]
        public async Task<IActionResult> SearchUsers([FromQuery] string? q)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var query = _userManager.Users.AsNoTracking().Where(u => u.Id != userId);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(u =>
                    (u.FullName != null && u.FullName.ToLower().Contains(term)) ||
                    (u.Email != null && u.Email.ToLower().Contains(term))
                );
            }

            var users = await query
                .OrderBy(u => u.FullName ?? u.Email)
                .Take(30)
                .ToListAsync();

            var result = users.Select(u => _mapper.Map<UserDto>(u)).ToList();
            return Ok(result);
        }

        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return UnauthorizedError("Sesión inválida. Inicia sesión de nuevo.");

            user.FullName = dto.FullName;
            user.Bio = dto.Bio;
            user.ProfilePictureUrl = dto.ProfilePictureUrl;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return BadRequestError("No se pudo actualizar el perfil.");

            return Ok(_mapper.Map<UserDto>(user));
        }

        [HttpDelete("me")]
        [Authorize]
        public async Task<IActionResult> DeleteCurrentUser()
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return UnauthorizedError("Sesión inválida. Inicia sesión de nuevo.");

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return BadRequestError("No se pudo eliminar la cuenta.");

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "CanViewAdmin")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            if (id == userId)
                return BadRequestError("No puedes eliminar tu propia cuenta desde el panel de administración.");

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFoundError("Usuario no encontrado.");

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return BadRequestError("No se pudo eliminar el usuario.");

            return NoContent();
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return UnauthorizedError("Sesión inválida. Inicia sesión de nuevo.");

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);

            if (!result.Succeeded)
                return BadRequestError("No se pudo cambiar la contraseña. Revisa tus credenciales.");

            return Ok(new { message = "Contraseña cambiada exitosamente" });
        }

        private async Task<UserDto> MapUserDtoAsync(ApplicationUser user)
        {
            var dto = _mapper.Map<UserDto>(user);
            var roles = await _userManager.GetRolesAsync(user);
            var claims = await _userManager.GetClaimsAsync(user);

            dto.Roles = roles.ToList();
            dto.Claims = claims.Select(c => $"{c.Type}:{c.Value}").ToList();

            return dto;
        }
    }

    public class UpdateProfileDto
    {
        public string? FullName { get; set; }
        public string? Bio { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }

    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }
}
