using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
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

            return Ok(_mapper.Map<UserDto>(user));
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
