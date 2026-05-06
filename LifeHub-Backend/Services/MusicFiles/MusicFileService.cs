using AutoMapper;
using LifeHub.Data;
using LifeHub.DTOs;
using LifeHub.Models;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Services.MusicFiles
{
    public class MusicFileService : IMusicFileService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public MusicFileService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<ServiceResult<List<MusicFileDto>>> GetMusicFilesAsync(string userId)
        {
            var files = await _context.MusicFiles
                .Where(m => m.UserId == userId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return ServiceResult<List<MusicFileDto>>.Ok(_mapper.Map<List<MusicFileDto>>(files));
        }

        public async Task<ServiceResult<MusicFileDto>> GetMusicFileAsync(int id, string userId)
        {
            var file = await _context.MusicFiles.FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

            if (file == null)
                return ServiceResult<MusicFileDto>.NotFound("Archivo de música no encontrado.");

            return ServiceResult<MusicFileDto>.Ok(_mapper.Map<MusicFileDto>(file));
        }

        public async Task<ServiceResult<MusicFileDto>> CreateMusicFileAsync(string userId, CreateMusicFileDto dto)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
                return ServiceResult<MusicFileDto>.Unauthorized("Sesión inválida. Inicia sesión de nuevo.");

            var musicFile = _mapper.Map<MusicFile>(dto);
            musicFile.UserId = userId;

            _context.MusicFiles.Add(musicFile);
            await _context.SaveChangesAsync();

            return ServiceResult<MusicFileDto>.Ok(_mapper.Map<MusicFileDto>(musicFile));
        }

        public async Task<ServiceResult<MusicFileDto>> UpdateMusicFileAsync(int id, string userId, UpdateMusicFileDto dto)
        {
            var musicFile = await _context.MusicFiles.FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

            if (musicFile == null)
                return ServiceResult<MusicFileDto>.NotFound("Archivo de música no encontrado.");

            _mapper.Map(dto, musicFile);
            musicFile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ServiceResult<MusicFileDto>.Ok(_mapper.Map<MusicFileDto>(musicFile));
        }

        public async Task<ServiceResult<bool>> DeleteMusicFileAsync(int id, string userId)
        {
            var musicFile = await _context.MusicFiles.FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

            if (musicFile == null)
                return ServiceResult<bool>.NotFound("Archivo de música no encontrado.");

            _context.MusicFiles.Remove(musicFile);
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Ok(true);
        }
    }
}
