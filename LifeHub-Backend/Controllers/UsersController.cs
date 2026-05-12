using LifeHub.DTOs;
using LifeHub.Services.Users;
using LifeHub.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ApiControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(string id)
        {
            var result = await _userService.GetUserAsync(id);
            return ToActionResult(result);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _userService.GetCurrentUserAsync(userId);
            return ToActionResult(result);
        }

        [HttpGet]
        [Authorize(Policy = "CanViewAdmin")]
        public async Task<IActionResult> GetUsers()
        {
            var result = await _userService.GetUsersAsync();
            return ToActionResult(result);
        }

        [HttpGet("search")]
        [Authorize]
        public async Task<IActionResult> SearchUsers([FromQuery] string? q)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _userService.SearchUsersAsync(userId, q);
            return ToActionResult(result);
        }

        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _userService.UpdateProfileAsync(userId, dto);
            return ToActionResult(result);
        }

        [HttpDelete("me")]
        [Authorize]
        public async Task<IActionResult> DeleteCurrentUser()
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _userService.DeleteCurrentUserAsync(userId);
            if (!result.IsSuccess) return ToActionResult(result);

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "CanViewAdmin")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _userService.DeleteUserAsync(id, userId);
            if (!result.IsSuccess) return ToActionResult(result);

            return NoContent();
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _userService.ChangePasswordAsync(userId, dto);
            if (!result.IsSuccess) return ToActionResult(result);

            return Ok(new { message = "Contraseña cambiada exitosamente" });
        }
    }
}
