using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LifeHub.DTOs;
using LifeHub.Services.Admin;
using LifeHub.Utilidades;

namespace LifeHub.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize]
    public class AdminController : ApiControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("users")]
        [Authorize(Policy = "CanViewAdmin")]
        public async Task<IActionResult> GetAdminUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _adminService.GetAdminUsersAsync(page, pageSize);
            return Ok(result);
        }

        [HttpPut("users/{id}/toggle-active")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var authError = RequireAuthenticatedUserId(out var callerId);
            if (authError != null) return authError;
            try
            {
                var user = await _adminService.ToggleActiveAsync(id, callerId);
                return Ok(user);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPut("users/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminUpdateUser(string id, [FromBody] AdminUpdateUserDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var authError = RequireAuthenticatedUserId(out var callerId);
            if (authError != null) return authError;
            try
            {
                var user = await _adminService.AdminUpdateUserAsync(id, dto, callerId);
                return Ok(user);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("users/{id}/set-password")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminSetPassword(string id, [FromBody] AdminSetPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _adminService.AdminSetPasswordAsync(id, dto);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPut("users/{id}/roles")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminUpdateRole(string id, [FromBody] AdminUpdateRoleDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var authError = RequireAuthenticatedUserId(out var callerId);
            if (authError != null) return authError;
            try
            {
                var user = await _adminService.AdminUpdateRoleAsync(id, dto, callerId);
                return Ok(user);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("activity-logs")]
        [Authorize(Policy = "CanViewAdmin")]
        public async Task<IActionResult> GetActivityLogs([FromQuery] ActivityLogQuery query)
        {
            var result = await _adminService.GetActivityLogsAsync(query);
            return Ok(result);
        }

        [HttpPost("backup")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TriggerBackup()
        {
            try
            {
                var result = await _adminService.TriggerBackupAsync();
                return Ok(result);
            }
            catch (FileNotFoundException ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
