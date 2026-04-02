using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using LifeHub.Data;
using LifeHub.DTOs;
using LifeHub.Models;
using LifeHub.Utilidades;

namespace LifeHub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MusicFilesController : ApiControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public MusicFilesController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetMusicFiles()
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var files = await _context.MusicFiles
                .Where(m => m.UserId == userId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return Ok(_mapper.Map<List<MusicFileDto>>(files));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMusicFile(int id)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var file = await _context.MusicFiles.FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

            if (file == null)
                return NotFoundError("Archivo de música no encontrado.");

            return Ok(_mapper.Map<MusicFileDto>(file));
        }

        [HttpPost]
        public async Task<IActionResult> CreateMusicFile([FromBody] CreateMusicFileDto dto)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var sessionError = await EnsureActiveSessionAsync(_context, userId);
            if (sessionError != null)
                return sessionError;

            var musicFile = _mapper.Map<MusicFile>(dto);
            musicFile.UserId = userId;

            _context.MusicFiles.Add(musicFile);
            await _context.SaveChangesAsync();

            return Created($"api/musicfiles/{musicFile.Id}", _mapper.Map<MusicFileDto>(musicFile));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMusicFile(int id, [FromBody] UpdateMusicFileDto dto)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var musicFile = await _context.MusicFiles.FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

            if (musicFile == null)
                return NotFoundError("Archivo de música no encontrado.");

            _mapper.Map(dto, musicFile);
            musicFile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(_mapper.Map<MusicFileDto>(musicFile));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMusicFile(int id)
        {
            var authError = RequireAuthenticatedUserId(out var userId);
            if (authError != null)
                return authError;

            var musicFile = await _context.MusicFiles.FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

            if (musicFile == null)
                return NotFoundError("Archivo de música no encontrado.");

            _context.MusicFiles.Remove(musicFile);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
