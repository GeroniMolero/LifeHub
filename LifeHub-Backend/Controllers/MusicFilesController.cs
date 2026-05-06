using LifeHub.DTOs;
using LifeHub.Services.MusicFiles;
using LifeHub.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MusicFilesController : ApiControllerBase
    {
        private readonly IMusicFileService _musicFileService;

        public MusicFilesController(IMusicFileService musicFileService)
        {
            _musicFileService = musicFileService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMusicFiles()
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _musicFileService.GetMusicFilesAsync(userId);
            return ToActionResult(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMusicFile(int id)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _musicFileService.GetMusicFileAsync(id, userId);
            return ToActionResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateMusicFile([FromBody] CreateMusicFileDto dto)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _musicFileService.CreateMusicFileAsync(userId, dto);
            if (!result.IsSuccess) return ToActionResult(result);

            return Created($"api/musicfiles/{result.Value!.Id}", result.Value);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMusicFile(int id, [FromBody] UpdateMusicFileDto dto)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _musicFileService.UpdateMusicFileAsync(id, userId, dto);
            return ToActionResult(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMusicFile(int id)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null) return authError;

            var result = await _musicFileService.DeleteMusicFileAsync(id, userId);
            if (!result.IsSuccess) return ToActionResult(result);

            return NoContent();
        }
    }
}
